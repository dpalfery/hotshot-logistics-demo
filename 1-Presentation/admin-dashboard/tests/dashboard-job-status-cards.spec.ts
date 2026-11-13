import { test, expect } from '@playwright/test';

// Window extensions used by the auth test-mode mocks
declare global {
  interface Window {
    __BYPASS_AUTH__?: boolean;
    msalInstance?: unknown;
    useMsal?: unknown;
    useIsAuthenticated?: unknown;
    useMsalAuthentication?: unknown;
  }
}

test.describe('Dashboard Job Status Cards', () => {
  test.beforeEach(async ({ page }) => {
    await page.addInitScript(() => {
      window.__BYPASS_AUTH__ = true;
    });

    await page.addInitScript(() => {
      const mockMsalInstance = {
        getActiveAccount: () => ({ username: 'test@example.com' }),
        getAllAccounts: () => [{ username: 'test@example.com' }],
        setActiveAccount: () => {},
        addEventCallback: () => {},
        acquireTokenSilent: async () => ({ accessToken: 'test-token' }),
        acquireTokenPopup: async () => ({ accessToken: 'test-token' }),
      };

      window.msalInstance = mockMsalInstance;
      window.useMsal = () => ({
        instance: mockMsalInstance,
        inProgress: 'none',
        accounts: [{ username: 'test@example.com' }],
      });

      window.useIsAuthenticated = () => true;
      window.useMsalAuthentication = () => ({});
    });

    await page.route('https://localhost:5001/api/job/status-summary', async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          pending: 5,
          assigned: 3,
          enRoute: 8,
          received: 3,
        }),
      });
    });

    await page.route('https://localhost:5001/api/job', async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          items: [
            {
              id: 'job-1',
              title: 'Job 1',
              pickupAddress: '1 Pickup St',
              dropoffAddress: '1 Dropoff Ave',
              amount: 100,
              status: 'InProgress',
              assignedDriverId: 1,
              scheduledPickupTime: '2024-12-01T10:00:00Z',
            },
            {
              id: 'job-2',
              title: 'Job 2',
              pickupAddress: '2 Pickup St',
              dropoffAddress: '2 Dropoff Ave',
              amount: 200,
              status: 'Pending',
              assignedDriverId: null,
              scheduledPickupTime: '2024-12-01T14:00:00Z',
            },
            {
              id: 'job-3',
              title: 'Job 3',
              pickupAddress: '3 Pickup St',
              dropoffAddress: '3 Dropoff Ave',
              amount: 300,
              status: 'InProgress',
              assignedDriverId: 2,
              scheduledPickupTime: '2024-12-01T16:00:00Z',
            },
          ],
          totalCount: 3,
        }),
      });
    });

    await page.route('https://localhost:5001/api/drivers', async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          {
            id: 1,
            name: 'John Driver',
            isActive: true,
            licenseNumber: 'DL123456',
          },
          {
            id: 2,
            name: 'Jane Driver',
            isActive: true,
            licenseNumber: 'DL789012',
          },
          {
            id: 3,
            name: 'Bob Driver',
            isActive: false,
            licenseNumber: 'DL345678',
          },
        ]),
      });
    });

    await page.route('https://localhost:5001/api/billing/invoices/overdue', async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          items: [
            {
              id: 'inv-1',
              dueDate: '2024-11-15T00:00:00Z',
              balanceDue: 1500.0,
              status: 'Overdue',
            },
            {
              id: 'inv-3',
              dueDate: '2024-11-01T00:00:00Z',
              balanceDue: 800.0,
              status: 'Overdue',
            },
          ],
          totalCount: 2,
        }),
      });
    });

    await page.goto('/');
  });

  test('should display the four job status cards with the expected values', async ({ page }) => {
    const cards = page.locator('[data-testid="job-status-card"]');
    await expect(cards).toHaveCount(4);

    const pendingCard = cards.filter({ hasText: 'Pending' });
    await expect(pendingCard).toContainText('Pending');
    await expect(pendingCard).toContainText('5');

    const assignedCard = cards.filter({ hasText: 'Assigned' });
    await expect(assignedCard).toContainText('Assigned');
    await expect(assignedCard).toContainText('3');

    const enRouteCard = cards.filter({ hasText: /En Route/i });
    await expect(enRouteCard).toContainText('En Route');
    await expect(enRouteCard).toContainText('8');

    const receivedCard = cards.filter({ hasText: 'Received' });
    await expect(receivedCard).toContainText('Received');
    await expect(receivedCard).toContainText('3');
  });

  test('should display a dash for each job status when the status summary API fails', async ({ page }) => {
    await page.route('https://localhost:5001/api/job/status-summary', async route => {
      await route.fulfill({
        status: 500,
        contentType: 'application/json',
        body: JSON.stringify({ error: 'Internal server error' }),
      });
    });

    await page.reload();

    const cards = page.locator('[data-testid="job-status-card"]');
    await expect(cards).toHaveCount(4);
    await expect(cards).toContainText('—');
  });
});
