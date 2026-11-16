import { test, expect } from '@playwright/test';

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
    window.useMsal = () => ({ instance, inProgress: 'none', accounts: [] });
    window.useIsAuthenticated = () => true;
    window.useMsalAuthentication = () => ({});
  });
  await page.route('**/api/job', route => route.fulfill({ json: { items: [], totalCount: 0 } }));
  await page.route('**/api/drivers', route => route.fulfill({ json: [] }));
  await page.route('**/api/billing/invoices/overdue', route => route.fulfill({ json: [] }));
});

test('shows ordered live counts, colors and responsive layout', async ({ page }) => {
  await page.route('**/api/job/status-counts', route => route.fulfill({
    json: { pending: 7, assigned: 5, enRoute: 3, received: 11 },
  }));
  await page.setViewportSize({ width: 1280, height: 900 });
  await page.goto('/');
  const grid = page.getByTestId('job-status-cards');
  const cards = grid.getByTestId('job-status-card');
  await expect(cards).toHaveCount(4);
  await expect(grid).toHaveClass(/grid-cols-1.*sm:grid-cols-2.*lg:grid-cols-5.*gap-4/);
  const labels = ['Pending', 'Assigned', 'EnRoute', 'Received'];
  const counts = ['7', '5', '3', '11'];
  const colors = ['rgb(59, 130, 246)', 'rgb(14, 165, 233)', 'rgb(2, 132, 199)', 'rgb(3, 105, 161)'];
  for (let i = 0; i < labels.length; i++) {
    await expect(cards.nth(i)).toHaveAttribute('data-status', labels[i]);
    await expect(cards.nth(i)).toContainText(labels[i]);
    await expect(cards.nth(i)).toHaveClass(/bg-white.*p-6.*rounded-lg.*shadow/);
    await expect(cards.nth(i).getByTestId('job-status-count')).toHaveText(counts[i]);
    await expect(cards.nth(i).getByTestId('job-status-badge')).toHaveCSS('background-color', colors[i]);
  }
  const statsBox = await page.getByTestId('stats-container').boundingBox();
  const gridBox = await grid.boundingBox();
  const recentBox = await page.getByRole('heading', { name: 'Recent Jobs' }).boundingBox();
  expect(statsBox!.y + statsBox!.height).toBeLessThan(gridBox!.y);
  expect(gridBox!.y + gridBox!.height).toBeLessThan(recentBox!.y);
  const desktop = await Promise.all(await cards.all().then(items => items.map(item => item.boundingBox())));
  expect(new Set(desktop.map(box => box!.y)).size).toBe(1);
  await page.setViewportSize({ width: 375, height: 900 });
  const mobile = await Promise.all(await cards.all().then(items => items.map(item => item.boundingBox())));
  expect(new Set(mobile.map(box => box!.y)).size).toBe(4);
});

for (const [name, response] of [
  ['server error', { status: 500, body: 'Error' }],
  ['invalid payload', { json: { pending: -1, assigned: 5, enRoute: 3, received: 11 } }],
] as const) {
  test(`shows unavailable counts on ${name}`, async ({ page }) => {
    await page.route('**/api/job/status-counts', route => route.fulfill(response));
    await page.goto('/');
    await expect(page.getByRole('alert').filter({ hasText: 'Job status counts unavailable' })).toBeVisible();
    await expect(page.getByTestId('job-status-count')).toHaveText(['—', '—', '—', '—']);
  });
}
