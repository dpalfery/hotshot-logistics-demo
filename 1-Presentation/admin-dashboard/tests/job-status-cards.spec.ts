import { test, expect, type Page } from '@playwright/test';

const summary = { pending: 7, assigned: 3, enRoute: 5, received: 12 };
const statuses = [
  { key: 'pending', label: 'Pending', count: 7, color: 'rgb(59, 130, 246)' },
  { key: 'assigned', label: 'Assigned', count: 3, color: 'rgb(14, 165, 233)' },
  { key: 'enRoute', label: 'EnRoute', count: 5, color: 'rgb(2, 132, 199)' },
  { key: 'received', label: 'Received', count: 12, color: 'rgb(3, 105, 161)' },
];

async function mockSummary(page: Page, body: unknown = summary, status = 200) {
  await page.route('**/api/job/status-summary', route => route.fulfill({ status, json: body }));
}

test.use({ ignoreHTTPSErrors: true });

test.beforeEach(async ({ page }) => {
  await page.addInitScript(() => {
    window.__BYPASS_AUTH__ = true;
    const instance = {
      getActiveAccount: () => ({ username: 'test@example.com' }),
      getAllAccounts: () => [{ username: 'test@example.com' }],
      setActiveAccount: () => {},
      addEventCallback: () => {},
      acquireTokenSilent: async () => ({ accessToken: 'test-token' }),
      acquireTokenPopup: async () => ({ accessToken: 'test-token' }),
    };
    window.msalInstance = instance;
    window.useMsal = () => ({ instance, inProgress: 'none', accounts: [{ username: 'test@example.com' }] });
    window.useIsAuthenticated = () => true;
    window.useMsalAuthentication = () => ({});
  });
  await page.route('**/api/job', route => route.fulfill({ json: { items: [], totalCount: 0 } }));
  await page.route('**/api/drivers', route => route.fulfill({ json: [] }));
  await page.route('**/api/billing/invoices/overdue', route => route.fulfill({ json: { items: [], totalCount: 0 } }));
});

test('should display a card per status with the counts from the API', async ({ page }) => {
  await mockSummary(page);
  await page.goto('/');
  const cards = page.getByTestId('job-status-card');
  await expect(cards).toHaveCount(4);
  for (const [index, item] of statuses.entries()) {
    await expect(cards.nth(index)).toHaveAttribute('data-status', item.key);
    await expect(cards.nth(index).getByText(item.label, { exact: true })).toBeVisible();
    await expect(page.getByTestId(`job-status-count-${item.key}`)).toHaveText(String(item.count));
  }
});

test('should request the status summary once', async ({ page }) => {
  let requests = 0;
  await page.route('**/api/job/status-summary', route => {
    expect(route.request().method()).toBe('GET');
    requests++;
    return route.fulfill({ json: summary });
  });
  await page.goto('/');
  await expect(page.getByTestId('job-status-card')).toHaveCount(4);
  expect(requests).toBe(1);
});

test('should show zero counts as 0', async ({ page }) => {
  await mockSummary(page, { pending: 0, assigned: 0, enRoute: 0, received: 0 });
  await page.goto('/');
  for (const item of statuses) await expect(page.getByTestId(`job-status-count-${item.key}`)).toHaveText('0');
});

test('should show an alert when the summary request fails', async ({ page }) => {
  await mockSummary(page, { error: 'failure' }, 500);
  await page.goto('/');
  await expect(page.getByRole('alert').filter({ hasText: 'Job status counts are unavailable.' })).toBeVisible();
  await expect(page.getByTestId('job-status-card')).toHaveCount(0);
});

test('should show an alert when the summary body is invalid', async ({ page }) => {
  for (const body of [{ ...summary, pending: -1 }, { pending: 7, assigned: 3, enRoute: 5 }]) {
    await mockSummary(page, body);
    await page.goto('/');
    await expect(page.getByRole('alert').filter({ hasText: 'Job status counts are unavailable.' })).toBeVisible();
    await expect(page.getByTestId('job-status-card')).toHaveCount(0);
  }
});

test('should place the cards between the main metrics and Recent Jobs', async ({ page }) => {
  await mockSummary(page);
  await page.goto('/');
  const metrics = await page.getByTestId('stats-container').boundingBox();
  const cards = await page.getByTestId('job-status-cards').boundingBox();
  const recent = await page.getByRole('heading', { name: 'Recent Jobs' }).boundingBox();
  expect(metrics && cards && recent).toBeTruthy();
  expect(metrics!.y + metrics!.height).toBeLessThanOrEqual(cards!.y);
  expect(cards!.y + cards!.height).toBeLessThanOrEqual(recent!.y);
});

test('should colour each badge with its status colour', async ({ page }) => {
  await mockSummary(page);
  await page.goto('/');
  for (const item of statuses) {
    const card = page.locator(`[data-status="${item.key}"]`);
    const badge = card.locator('div.rounded-md');
    await expect(badge).toHaveCSS('background-color', item.color);
    const badgeBox = await badge.boundingBox();
    const labelBox = await card.getByText(item.label, { exact: true }).boundingBox();
    expect(badgeBox!.x).toBeLessThan(labelBox!.x);
    await expect(badge.locator('svg')).toHaveAttribute('aria-hidden', 'true');
  }
});

test('should match the existing card styling', async ({ page }) => {
  await mockSummary(page);
  await page.goto('/');
  for (const item of statuses) {
    const card = page.locator(`[data-status="${item.key}"]`);
    for (const name of ['bg-white', 'p-6', 'rounded-lg', 'shadow']) await expect(card).toHaveClass(new RegExp(`\\b${name}\\b`));
    await expect(card.getByText(item.label, { exact: true })).toHaveClass(/font-semibold/);
    await expect(page.getByTestId(`job-status-count-${item.key}`)).toHaveClass(/font-bold.*text-center/);
  }
});

for (const [width, columns] of [[375, 1], [768, 2], [1280, 4]] as const) {
  test(`should display ${columns} columns at ${width}px`, async ({ page }) => {
    await page.setViewportSize({ width, height: 900 });
    await mockSummary(page);
    await page.goto('/');
    const grid = page.getByTestId('job-status-cards');
    await expect(grid).toHaveClass(/lg:grid-cols-5/);
    await expect(grid).toHaveClass(/gap-4/);
    const cards = page.getByTestId('job-status-card');
    await expect(cards).toHaveCount(4);
    const boxes = await Promise.all(statuses.map((_, i) => cards.nth(i).boundingBox()));
    for (let i = 0; i < boxes.length; i++) {
      expect(boxes[i]!.x).toBeCloseTo(boxes[i % columns]!.x, 0);
      if (i >= columns) expect(boxes[i]!.y).toBeGreaterThan(boxes[i - columns]!.y);
      else expect(boxes[i]!.y).toBeCloseTo(boxes[0]!.y, 0);
    }
  });
}

test('@live should show the live API counts', async ({ page, request }) => {
  test.skip(process.env.LIVE_API !== '1', 'Set LIVE_API=1 to run against the live API');
  const response = await request.get('https://localhost:5001/api/job/status-summary', { headers: { Authorization: 'Test' } });
  expect(response.ok()).toBeTruthy();
  const body: Record<string, number> = await response.json();
  await page.goto('/');
  for (const item of statuses) await expect(page.getByTestId(`job-status-count-${item.key}`)).toHaveText(String(body[item.key]));
});
