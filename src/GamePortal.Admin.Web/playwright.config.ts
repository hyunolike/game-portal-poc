import { defineConfig, devices } from "@playwright/test";

/**
 * 운영툴 E2E. 실제 스택(SQL Server + Admin.Api + Web.Api + Worker + 운영툴)이 떠 있어야 한다.
 *   docker compose up -d  →  npm run e2e
 */
export default defineConfig({
  testDir: "./e2e",
  fullyParallel: false,
  retries: 0,
  reporter: [["list"]],
  use: {
    baseURL: process.env.ADMIN_WEB_URL ?? "http://localhost:3000",
    locale: "ko-KR",
    timezoneId: "Asia/Seoul",
    trace: "retain-on-failure",
    launchOptions: process.env.PW_CHROMIUM_PATH ? { executablePath: process.env.PW_CHROMIUM_PATH } : undefined,
  },
  projects: [{ name: "chromium", use: { ...devices["Desktop Chrome"] } }],
});
