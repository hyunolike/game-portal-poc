// 쿠폰 사용 부하 테스트.
//
//   SCENARIO=first_come  선착순 STOCK장에 PLAYERS명이 몰린다. 정확히 STOCK명만 성공해야 한다.
//   SCENARIO=throughput  수량 제한 없는 공용 쿠폰에 VUS명이 DURATION 동안 계속 요청한다.
//                        모든 요청이 캠페인 한 행의 RedeemedCount 를 갱신하므로, 이 행의 락이 처리량 상한이 된다.
//
// 토큰은 /dev/token 을 부르지 않고 k6 안에서 서명한다. 토큰 발급 요청이 측정값에 섞이지 않게 하기 위해서다.
import http from 'k6/http';
import { check, fail } from 'k6';
import crypto from 'k6/crypto';
import encoding from 'k6/encoding';
import exec from 'k6/execution';
import { Counter } from 'k6/metrics';

const SCENARIO = __ENV.SCENARIO || 'first_come';
const WEB_API = __ENV.WEB_API_URL || 'http://localhost:5100';
const ADMIN_API = __ENV.ADMIN_API_URL || 'http://localhost:5200';
const SIGNING_KEY = __ENV.WEB_SIGNING_KEY || 'local-dev-only-signing-key-change-me-0123456789';
const STOCK = Number(__ENV.STOCK || 1000);
const PLAYERS = Number(__ENV.PLAYERS || 5000);
const VUS = Number(__ENV.VUS || 100);
const DURATION = __ENV.DURATION || '30s';
const RESULTS_DIR = __ENV.RESULTS_DIR || 'tools/loadtest/results';

const redeemOk = new Counter('redeem_ok');
const redeemSoldOut = new Counter('redeem_sold_out');
const redeemUnexpected = new Counter('redeem_unexpected');
const integrityMismatch = new Counter('integrity_mismatch');
let unexpectedLogged = 0; // VU 마다 몇 건만 남긴다

const scenarios = {
  first_come: {
    executor: 'shared-iterations',
    vus: Math.min(VUS, PLAYERS),
    iterations: PLAYERS,
    maxDuration: '3m',
  },
  throughput: {
    executor: 'constant-vus',
    vus: VUS,
    duration: DURATION,
  },
};

const thresholds = {
  first_come: {
    redeem_ok: [`count==${STOCK}`],
    redeem_sold_out: [`count==${PLAYERS - STOCK}`],
    redeem_unexpected: ['count==0'],
    integrity_mismatch: ['count==0'],
  },
  throughput: {
    redeem_unexpected: ['count==0'],
    integrity_mismatch: ['count==0'],
  },
};

if (!scenarios[SCENARIO]) {
  throw new Error(`알 수 없는 SCENARIO: ${SCENARIO}`);
}

export const options = {
  scenarios: { [SCENARIO]: scenarios[SCENARIO] },
  // 쿠폰 사용 요청만 따로 집계하려고 태그 기준 하위 지표를 만든다(항상 참인 조건).
  thresholds: { ...thresholds[SCENARIO], 'http_req_duration{name:redeem}': ['max>=0'] },
  summaryTrendStats: ['avg', 'med', 'p(95)', 'p(99)', 'max'],
};

export function setup() {
  const login = http.post(
    `${ADMIN_API}/dev/token`,
    JSON.stringify({ operatorId: 1, name: 'loadtest', roles: ['Admin'] }),
    { headers: { 'Content-Type': 'application/json' } },
  );
  if (login.status !== 200) fail(`운영자 토큰 발급 실패: ${login.status}`);
  const adminToken = login.json('accessToken');

  const code = `LOAD${Date.now()}`;
  const now = new Date();
  const campaign = http.post(
    `${ADMIN_API}/api/v1/coupon-campaigns`,
    JSON.stringify({
      name: `부하 테스트 ${SCENARIO}`,
      type: 'Shared',
      startsAt: new Date(now.getTime() - 60_000).toISOString(),
      endsAt: new Date(now.getTime() + 86_400_000).toISOString(),
      maxRedemptions: SCENARIO === 'first_come' ? STOCK : null,
      rewards: [{ itemId: 1001, quantity: 1 }],
      sharedCode: code,
    }),
    { headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${adminToken}` } },
  );
  if (campaign.status !== 201 && campaign.status !== 200) fail(`캠페인 생성 실패: ${campaign.status} ${campaign.body}`);

  return { code, campaignId: campaign.json('id'), adminToken };
}

export default function (data) {
  // 반복마다 다른 계정. 계정당 1회 제약에 걸리지 않게 하고, 계정당 Rate limit 도 피한다.
  const accountId = 1_000_000 + exec.scenario.iterationInTest;
  const res = http.post(
    `${WEB_API}/api/v1/coupons/redeem`,
    JSON.stringify({ code: data.code }),
    {
      headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${playerToken(accountId)}` },
      responseCallback: http.expectedStatuses(200, 409),
      tags: { name: 'redeem' },
    },
  );

  if (res.status === 200) {
    redeemOk.add(1);
  } else if (res.status === 409 && res.json('code') === 'COUPON_SOLD_OUT') {
    redeemSoldOut.add(1);
  } else {
    redeemUnexpected.add(1);
    if (unexpectedLogged++ < 3) {
      console.warn(`예상하지 못한 응답: ${res.status} ${res.body}`);
    }
  }
}

export function teardown(data) {
  // 응답 수와 DB 에 남은 사용 수가 같은지 운영 API 로 한 번 더 확인한다.
  const res = http.get(`${ADMIN_API}/api/v1/coupon-campaigns/${data.campaignId}`, {
    headers: { Authorization: `Bearer ${data.adminToken}` },
  });
  const redeemed = res.json('redeemedCount');
  const ok = check(res, {
    'DB 사용 수를 읽었다': (r) => r.status === 200,
    '선착순이면 DB 사용 수가 재고와 같다': () => SCENARIO !== 'first_come' || redeemed === STOCK,
  });
  if (!ok) integrityMismatch.add(1);
  console.log(`campaign=${data.campaignId} redeemedCount=${redeemed}`);
}

export function handleSummary(data) {
  const m = data.metrics;
  const value = (name, key) => (m[name] ? m[name].values[key] : 0);
  const ms = (key) => `${value('http_req_duration{name:redeem}', key).toFixed(1)} ms`;
  const redeemed = value('redeem_ok', 'count') + value('redeem_sold_out', 'count') + value('redeem_unexpected', 'count');
  const passed = Object.values(m).every((metric) => !metric.thresholds || Object.values(metric.thresholds).every((t) => t.ok));

  const rows = [
    ['시나리오', SCENARIO],
    ['결과', passed ? '통과' : '실패'],
    ['쿠폰 사용 요청 수', redeemed],
    ['처리량', `${(redeemed / (data.state.testRunDurationMs / 1000)).toFixed(0)} req/s`],
    ['응답 시간 중앙값', ms('med')],
    ['응답 시간 p95', ms('p(95)')],
    ['응답 시간 p99', ms('p(99)')],
    ['성공', value('redeem_ok', 'count')],
    ['소진(409)', value('redeem_sold_out', 'count')],
    ['예상 밖 응답', value('redeem_unexpected', 'count')],
  ];
  const markdown = [
    `### 쿠폰 부하 테스트: ${SCENARIO}`,
    '',
    SCENARIO === 'first_come'
      ? `선착순 ${STOCK}장, 서로 다른 계정 ${PLAYERS}명, 동시 VU ${scenarios.first_come.vus}`
      : `수량 제한 없는 공용 쿠폰, 동시 VU ${VUS}, ${DURATION}`,
    '',
    '| 항목 | 값 |',
    '|---|---|',
    ...rows.map(([k, v]) => `| ${k} | ${v} |`),
    '',
  ].join('\n');

  return {
    stdout: `${markdown}\n`,
    [`${RESULTS_DIR}/${SCENARIO}.md`]: markdown,
    [`${RESULTS_DIR}/${SCENARIO}.json`]: JSON.stringify(data, null, 2),
  };
}

function playerToken(accountId) {
  const now = Math.floor(Date.now() / 1000);
  const header = b64url(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
  const payload = b64url(JSON.stringify({
    sub: String(accountId),
    name: `player${accountId}`,
    role: 'Player',
    iss: 'game-platform',
    aud: 'game-portal-web',
    nbf: now - 5,
    exp: now + 3600,
  }));
  const signature = crypto.hmac('sha256', SIGNING_KEY, `${header}.${payload}`, 'binary');
  return `${header}.${payload}.${encoding.b64encode(signature, 'rawurl')}`;
}

function b64url(text) {
  return encoding.b64encode(text, 'rawurl');
}
