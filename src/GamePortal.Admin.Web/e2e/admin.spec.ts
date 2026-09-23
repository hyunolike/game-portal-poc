import { expect, test, type Page } from "@playwright/test";

const WEB_API_URL = process.env.WEB_API_URL ?? "http://localhost:5100";

async function login(page: Page, role: "Admin" | "Operator" | "CS", name = "김운영", next?: string) {
  await page.goto(next ? `/login?next=${encodeURIComponent(next)}` : "/login");
  await page.getByLabel("운영자 번호", { exact: true }).fill("7");
  await page.getByLabel("이름", { exact: true }).fill(name);
  await page.locator(`input[name=role][value=${role}]`).check();
  await page.getByRole("button", { name: "로그인" }).click();
  // 쿠키가 설정되고 리다이렉트가 끝난 뒤에 다음 이동을 해야 한다
  await expect(page).not.toHaveURL(/\/login/);
}

function uniq(prefix: string) {
  return `${prefix}${Date.now().toString(36).toUpperCase()}`;
}

test("로그인하지 않으면 로그인 화면으로 가고, 로그인 후 원래 페이지로 돌아간다", async ({ page }) => {
  await page.goto("/coupons");
  await expect(page).toHaveURL(/\/login\?next=%2Fcoupons/);

  await page.getByRole("button", { name: "로그인" }).click();

  await expect(page).toHaveURL(/\/coupons$/);
  await expect(page.getByRole("heading", { name: "쿠폰 캠페인" })).toBeVisible();
});

test("운영자: 공지를 예약 등록하면 목록에 예약 상태로 보이고 수정할 수 있다", async ({ page }) => {
  await login(page, "Operator");
  const title = uniq("E2E 점검 공지 ");

  await page.getByRole("link", { name: "공지 관리" }).click();
  await page.getByRole("link", { name: "새 공지" }).click();
  await expect(page).toHaveURL(/\/notices\/new$/);
  await page.getByRole("combobox", { name: "분류" }).selectOption("Maintenance");
  await page.getByLabel("제목", { exact: true }).fill(title);
  await page.getByLabel("본문", { exact: true }).fill("06:00 ~ 10:00 정기 점검\n\n점검 보상: 에테르 주화 300개");
  await page.getByLabel("게시 일시 (KST)", { exact: true }).fill("2030-01-01T06:00");
  await page.getByRole("button", { name: "등록" }).click();

  await expect(page.getByText("등록했습니다")).toBeVisible();
  await page.getByLabel("제목", { exact: true }).fill(`${title} (수정)`);
  await page.getByRole("button", { name: "저장" }).click();
  await expect(page.getByText("저장했습니다")).toBeVisible();

  await page.getByRole("link", { name: "공지 관리" }).click();
  const row = page.getByRole("row", { name: new RegExp(`${title} \\(수정\\)`) });
  await expect(row.getByText("예약")).toBeVisible();
  await expect(row.getByText("2030.01.01 06:00")).toBeVisible();
});

test("관리자: 쿠폰 발행 → 유저 사용 → CS 조회에서 지급 완료 확인 → 사용 중지 → 감사 로그", async ({ page, request }) => {
  await login(page, "Admin");
  const code = uniq("E2E");
  const campaignName = `E2E 캠페인 ${code}`;

  // 1) 발행 (확인 대화상자에 요약이 나온다)
  await page.goto("/coupons/new");
  await page.getByLabel("캠페인 이름", { exact: true }).fill(campaignName);
  await page.getByLabel("공용 코드", { exact: true }).fill(code);
  await page.getByLabel("선착순 수량 (선택)", { exact: true }).fill("100");
  await page.getByRole("button", { name: "+ 아이템 추가" }).click();
  let confirmText = "";
  page.once("dialog", (d) => {
    confirmText = d.message();
    void d.accept();
  });
  await page.getByRole("button", { name: "쿠폰 발행" }).click();
  await expect(page.getByText("쿠폰을 발행했습니다")).toBeVisible();
  expect(confirmText).toContain(`공용 코드 ${code}`);
  expect(confirmText).toContain("선착순 100명");
  const campaignUrl = page.url().replace(/\?.*$/, "");

  // 2) 유저가 홈페이지(Web.Api)에서 쿠폰 사용
  const accountId = Math.floor(Date.now() / 1000) % 1_000_000 + 5_000_000;
  const token = (await (await request.post(`${WEB_API_URL}/dev/token`, { data: { accountId } })).json()).accessToken as string;
  const redeem = await request.post(`${WEB_API_URL}/api/v1/coupons/redeem`, {
    data: { code },
    headers: { Authorization: `Bearer ${token}` },
  });
  expect(redeem.status()).toBe(200);

  // 3) CS 조회: Worker 가 게임 서버로 전달하면 "지급 완료"
  await page.getByRole("link", { name: "CS 조회" }).click();
  await page.getByLabel("계정 번호", { exact: true }).fill(String(accountId));
  await page.getByRole("button", { name: "조회" }).click();
  await expect(page.getByRole("row", { name: new RegExp(campaignName) })).toBeVisible();
  await expect(async () => {
    await page.reload();
    await expect(page.getByRole("row", { name: new RegExp(campaignName) }).getByText("지급 완료")).toBeVisible({ timeout: 1000 });
  }).toPass({ timeout: 20_000 });

  // 4) 캠페인 상세: 사용 1건, 사용 중지
  await page.goto(campaignUrl);
  await expect(page.getByText("1 / 100")).toBeVisible();
  page.once("dialog", (d) => void d.accept());
  await page.getByRole("button", { name: "사용 중지" }).click();
  await expect(page.getByText("사용을 중지했습니다")).toBeVisible();
  await expect(page.getByRole("button", { name: "사용 재개" })).toBeVisible();

  // 중지 후 유저 사용 시도 → 차단
  const token2 = (await (await request.post(`${WEB_API_URL}/dev/token`, { data: { accountId: accountId + 1 } })).json()).accessToken as string;
  const blocked = await request.post(`${WEB_API_URL}/api/v1/coupons/redeem`, {
    data: { code },
    headers: { Authorization: `Bearer ${token2}` },
  });
  expect((await blocked.json()).code).toBe("COUPON_DISABLED");

  // 5) 감사 로그에 생성 + 중지(IsEnabled true → false) 가 남는다
  await page.getByRole("link", { name: "변경 이력" }).click();
  await expect(page.getByRole("cell", { name: "생성" })).toBeVisible();
  const modified = page.getByRole("row").filter({ has: page.getByText("수정", { exact: true }) });
  await expect(modified.getByText("IsEnabled")).toBeVisible();
});

test("CS: 조회만 가능하고 발행·작성·관리 메뉴가 보이지 않는다", async ({ page }) => {
  await login(page, "CS", "이상담");

  const nav = page.getByRole("navigation", { name: "운영툴 메뉴" });
  await expect(nav.getByRole("link", { name: "CS 조회" })).toBeVisible();
  await expect(nav.getByRole("link", { name: "지급 모니터링" })).toHaveCount(0);
  await expect(nav.getByRole("link", { name: "감사 로그" })).toHaveCount(0);

  await page.goto("/coupons");
  await expect(page.getByRole("link", { name: "쿠폰 발행" })).toHaveCount(0);
  await page.goto("/notices");
  await expect(page.getByRole("link", { name: "새 공지" })).toHaveCount(0);

  // URL 직접 접근도 되돌린다 (API 도 403 으로 막지만 화면에서 먼저 차단)
  await page.goto("/coupons/new");
  await expect(page).toHaveURL(/\/coupons$/);
  await page.goto("/audit-logs");
  await expect(page).toHaveURL(/\/$/);
});
