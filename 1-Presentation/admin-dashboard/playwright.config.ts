import { defineConfig, devices } from '@playwright/test';

/**
 * The suite runs against plain http on its own port: every API call is mocked in the tests, so the
 * dev certificates `npm run dev` needs (git-ignored, absent in a fresh checkout or worktree) are
 * not required, and a developer's https dev server on port 3000 is never reused by mistake. Set
 * PLAYWRIGHT_PORT to run two suites at once (for example from two worktrees).
 */
const PORT = Number(process.env.PLAYWRIGHT_PORT ?? 3100);
const BASE_URL = `http://localhost:${PORT}`;

/**
 * @see https://playwright.dev/docs/test-configuration
 */
export default defineConfig({
  testDir: './tests',
  testMatch: /.*\.spec\.ts$/,
  /* Fail fast if a single test takes too long */
  timeout: 45_000,
  /* Global timeout for the full run */
  globalTimeout: 60 * 60 * 1000,
  /* Expect timeout */
  expect: { timeout: 10_000 },
  /* Run tests in files in parallel */
  fullyParallel: true,
  /* Fail the build on CI if you accidentally left test.only in the source code. */
  forbidOnly: !!process.env.CI,
  /* Retry on CI only */
  retries: process.env.CI ? 2 : 0,
  /* Opt out of parallel tests on CI. */
  workers: process.env.CI ? 1 : undefined,
  /* Reporter to use. See https://playwright.dev/docs/test-reporters */
  reporter: process.env.CI
    ? [['dot'], ['html', { open: 'never' }]]
    : [['list'], ['html', { open: 'never' }]],
  /* Shared settings for all the projects below. See https://playwright.dev/docs/api/class-testoptions. */
  use: {
    /* Base URL to use in actions like `await page.goto('/')`. */
    baseURL: BASE_URL,
    /* Pin locale and time zone: the app formats dates with toLocaleDateString(), so assertions on them depend on both. */
    locale: 'en-US',
    timezoneId: 'UTC',

    /* Collect trace when retrying the failed test. See https://playwright.dev/docs/trace-viewer */
    trace: 'on-first-retry',
    /* Action timeout */
    actionTimeout: 15_000,
  },

  /* Configure projects for major browsers */
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },

    {
      name: 'firefox',
      use: { ...devices['Desktop Firefox'] },
    },

    {
      name: 'webkit',
      use: { ...devices['Desktop Safari'] },
    },

    /* Test against mobile viewports. */
    // {
    //   name: 'Mobile Chrome',
    //   use: { ...devices['Pixel 5'] },
    // },
    // {
    //   name: 'Mobile Safari',
    //   use: { ...devices['iPhone 12'] },
    // },

    /* Test against branded browsers. */
    // {
    //   name: 'Microsoft Edge',
    //   use: { ...devices['Desktop Edge'], channel: 'msedge' },
    // },
    // {
    //   name: 'Google Chrome',
    //   use: { ...devices['Desktop Chrome'], channel: 'chrome' },
    // },
  ],

  /* Run your local dev server before starting the tests */
  webServer: {
    // Calls `next dev` directly, skipping `predev` (generate-env.mjs needs real Azure IDs). The
    // env below gives the app placeholder IDs, so no .env.local is needed either.
    command: `npx next dev --port ${PORT}`,
    url: BASE_URL,
    env: {
      NEXT_PUBLIC_AZURE_CLIENT_ID: '11111111-1111-4111-8111-111111111111',
      NEXT_PUBLIC_AZURE_TENANT_ID: '22222222-2222-4222-8222-222222222222',
      NEXT_PUBLIC_API_BASE_URL: 'https://localhost:5001/api',
    },
    // Never reuse a server: another worktree's server on this port would test that worktree's code
    reuseExistingServer: false,
    timeout: process.env.CI ? 180_000 : 60_000,
  },
});