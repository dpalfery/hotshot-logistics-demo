import { expect, test, type Page } from '@playwright/test';

const summary = { pending: 7, assigned: 3, enRoute: 5, received: 12 };
const labels = ['Pending', 'Assigned', 'EnRoute', 'Received'];
const keys = ['pending', 'assigned', 'enroute', 'received'];

async function mockSummary(page: Page, data: unknown = summary, status = 200) {
  await page.route('**/api/job/status-summary', route => route.fulfill({ status, json: data }));
}

test.beforeEach(async ({ page }) => {
  await page.addInitScript(() => { window.__BYPASS_AUTH__ = true; });
  await page.route('**/api/job', route => route.fulfill({ json: { items: [], totalCount: 0 } }));
  await page.route('**/api/drivers', route => route.fulfill({ json: [] }));
  await page.route('**/api/billing/invoices/overdue', route => route.fulfill({ json: [] }));
  await mockSummary(page);
});

test('should display four status cards with counts from the API', async ({ page }) => {
  await page.goto('/');
  const cards = page.getByTestId('job-status-card');
  await expect(cards).toHaveCount(4);
  for (let i = 0; i < labels.length; i++) {
    await expect(cards.nth(i)).toHaveAttribute('data-status', labels[i]);
    await expect(cards.nth(i).locator('dt')).toContainText(labels[i]);
    await expect(page.getByTestId(`job-status-count-${keys[i]}`)).toHaveText(['7', '3', '5', '12'][i]);
  }
});

test('should place status cards between metric cards and recent activity', async ({ page }) => {
  await page.goto('/');
  const stats = await page.getByTestId('stats-container').boundingBox();
  const cards = await page.getByTestId('job-status-cards').boundingBox();
  const recent = await page.getByText('Recent Jobs').boundingBox();
  expect(stats!.y + stats!.height).toBeLessThanOrEqual(cards!.y);
  expect(cards!.y + cards!.height).toBeLessThanOrEqual(recent!.y);
});

test('should display zero counts', async ({ page }) => {
  await mockSummary(page, { pending: 0, assigned: 0, enRoute: 0, received: 0 });
  await page.goto('/');
  await expect(page.getByTestId('job-status-card')).toHaveCount(4);
  for (const key of keys) await expect(page.getByTestId(`job-status-count-${key}`)).toHaveText('0');
});

test('should show an error and no cards when the API fails', async ({ page }) => {
  await mockSummary(page, { error: 'failure' }, 500);
  await page.goto('/');
  await expect(page.locator('p[role="alert"]')).toHaveText('Unable to load job status counts.');
  await expect(page.getByTestId('job-status-card')).toHaveCount(0);
});

test('should show an error when the response is malformed', async ({ page }) => {
  await mockSummary(page, { pending: 1 });
  await page.goto('/');
  await expect(page.locator('p[role="alert"]')).toHaveText('Unable to load job status counts.');
  await expect(page.getByTestId('job-status-card')).toHaveCount(0);
});

test('should show a loading state before counts arrive', async ({ page }) => {
  let release!: () => void;
  const wait = new Promise<void>(resolve => { release = resolve; });
  await page.route('**/api/job/status-summary', async route => {
    await wait;
    await route.fulfill({ json: summary });
  });
  await page.goto('/');
  await expect(page.getByTestId('job-status-cards-loading')).toBeVisible();
  release();
  await expect(page.getByTestId('job-status-count-pending')).toHaveText('7');
});

test('should match the metric cards padding, shadow and corners', async ({ page }) => {
  await page.goto('/');
  const styles = (element: Element) => {
    const s = getComputedStyle(element);
    return [s.padding, s.boxShadow, s.borderRadius, s.backgroundColor];
  };
  const metric = await page.getByTestId('stats-card').first().evaluate(styles);
  const card = await page.getByTestId('job-status-card').first().evaluate(styles);
  expect(card).toEqual(metric);
});

test('should render the specified badge colours and icons', async ({ page }) => {
  await page.goto('/');
  const colors = ['rgb(59, 130, 246)', 'rgb(14, 165, 233)', 'rgb(2, 132, 199)', 'rgb(3, 105, 161)'];
  const icons = ['hourglass', 'clipboard-list', 'truck', 'inbox'];
  for (let i = 0; i < 4; i++) {
    const card = page.getByTestId('job-status-card').nth(i);
    const badge = card.locator('dt > span').first();
    await expect(badge).toHaveCSS('background-color', colors[i]);
    await expect(badge.locator(`svg.lucide-${icons[i]}[aria-hidden="true"]`)).toBeVisible();
    expect((await badge.boundingBox())!.x).toBeLessThan((await card.locator('dt > span').nth(1).boundingBox())!.x);
  }
});

test('should use Inter with semibold labels and bold centred counts', async ({ page }) => {
  await page.goto('/');
  const card = page.getByTestId('job-status-card').first();
  const label = card.locator('dt > span').nth(1);
  const count = card.locator('dd');
  await expect(label).toHaveCSS('font-family', /Inter/i);
  await expect(label).toHaveCSS('font-weight', '600');
  await expect(count).toHaveCSS('font-weight', '700');
  await expect(count).toHaveCSS('text-align', 'center');
});

test('should lay out cards responsively', async ({ page }) => {
  await page.goto('/');
  const grid = page.getByTestId('job-status-cards');
  for (const cls of ['grid-cols-1', 'sm:grid-cols-2', 'lg:grid-cols-5', 'gap-4']) {
    await expect(grid).toHaveClass(new RegExp(cls.replace(':', '\\:')));
  }
  const cards = page.getByTestId('job-status-card');
  for (const width of [375, 768, 1200]) {
    await page.setViewportSize({ width, height: 900 });
    const boxes = await Promise.all([0, 1, 2, 3].map(i => cards.nth(i).boundingBox()));
    if (width === 375) expect(new Set(boxes.map(box => box!.x)).size).toBe(1);
    if (width === 768) {
      expect(boxes[0]!.y).toBe(boxes[1]!.y);
      expect(boxes[2]!.y).toBe(boxes[3]!.y);
      expect(boxes[2]!.y).toBeGreaterThan(boxes[0]!.y);
    }
    if (width === 1200) expect(new Set(boxes.map(box => box!.y)).size).toBe(1);
  }
});
