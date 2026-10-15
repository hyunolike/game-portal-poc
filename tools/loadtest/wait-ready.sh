#!/usr/bin/env bash
# Admin.Api 가 기동하며 마이그레이션을 끝내고, 두 API 가 준비될 때까지 기다린다.
set -euo pipefail
ADMIN_API_URL=${ADMIN_API_URL:-http://localhost:5200}
WEB_API_URL=${WEB_API_URL:-http://localhost:5100}

for _ in $(seq 1 90); do
  token=$(curl -sf -X POST "$ADMIN_API_URL/dev/token" -H 'content-type: application/json' \
    -d '{"operatorId":1,"name":"loadtest","roles":["Admin"]}' | sed -E 's/.*"accessToken":"([^"]+)".*/\1/' || true)
  if [[ -n "$token" ]] \
    && curl -sf -o /dev/null "$ADMIN_API_URL/api/v1/coupon-campaigns" -H "authorization: Bearer $token" \
    && curl -sf -o /dev/null "$WEB_API_URL/health/ready"; then
    echo "ready"
    exit 0
  fi
  sleep 2
done

echo "API 가 준비되지 않았습니다" >&2
exit 1
