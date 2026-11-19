import { test, expect } from '@playwright/test';
import { mockSignalR } from './utils/signalr-mock';

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

// The fields the Jobs page renders are included because the navigation test visits it
const firstPageOfJobs = [
  {
    id: 'job-1',
    title: 'Job 1',
    pickupAddress: '1 Pickup St',
    dropoffAddress: '1 Dropoff Ave',
    amount: 100,
    status: 'EnRoute',
    assignedDriverId: 1,
    scheduledPickupTime: '2024-12-01T10:00:00Z'
  },
  {
    id: 'job-2',
    title: 'Job 2',
    pickupAddress: '2 Pickup St',
    dropoffAddress: '2 Dropoff Ave',
    amount: 200,
    status: 'Pending',
    assignedDriverId: null,
    scheduledPickupTime: '2024-12-01T14:00:00Z'
  },
  {
    id: 'job-3',
    title: 'Job 3',
    pickupAddress: '3 Pickup St',
    dropoffAddress: '3 Dropoff Ave',
    amount: 300,
    status: 'EnRoute',
    assignedDriverId: 2,
    scheduledPickupTime: '2024-12-01T16:00:00Z'
  }
];

// What the mocked jobs endpoint answers; a test changes it to simulate the server's data changing
let jobsState: { items: typeof firstPageOfJobs; totalCount: number; activeCount: number };

test.describe('Dashboard Overview', () => {
  test.beforeEach(async ({ page }) => {
    // Set test mode bypass flag to skip authentication
    await page.addInitScript(() => {
      window.__BYPASS_AUTH__ = true;
    });

    // Mock MSAL authentication to avoid interference
    await page.addInitScript(() => {
      // Mock MSAL instance
      const mockMsalInstance = {
        getActiveAccount: () => ({ username: 'test@example.com' }),
        getAllAccounts: () => [{ username: 'test@example.com' }],
        setActiveAccount: () => {},
        addEventCallback: () => {},
        acquireTokenSilent: async () => ({ accessToken: 'test-token' }),
        acquireTokenPopup: async () => ({ accessToken: 'test-token' }),
      };

      // Replace global MSAL instance
      window.msalInstance = mockMsalInstance;

      // Mock Azure MSAL React hooks
      window.useMsal = () => ({
        instance: mockMsalInstance,
        inProgress: 'none',
        accounts: [{ username: 'test@example.com' }]
      });

      window.useIsAuthenticated = () => true;
      window.useMsalAuthentication = () => ({});
    });

    // Mock jobs API - matches apiService.getJobs() and apiService.getActiveJobCount(), which both call /api/job.
    // The active count is a one-item page whose totalCount covers every job, so it is answered separately from the first page.
    jobsState = { items: firstPageOfJobs, totalCount: 3, activeCount: 3 };
    await page.route(url => url.origin === 'https://localhost:5001' && url.pathname === '/api/job', async route => {
      const isActiveCount = new URL(route.request().url()).searchParams.get('isActive') === 'true';
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(isActiveCount
          ? { items: jobsState.items.slice(0, 1), totalCount: jobsState.activeCount }
          : { items: jobsState.items, totalCount: jobsState.totalCount })
      });
    });

    // Mock drivers API with active drivers - matches apiService.getDrivers() endpoint
    await page.route('https://localhost:5001/api/drivers', async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          {
            id: 1,
            name: 'John Driver',
            isActive: true,
            licenseNumber: 'DL123456'
          },
          {
            id: 2,
            name: 'Jane Driver',
            isActive: true,
            licenseNumber: 'DL789012'
          },
          {
            id: 3,
            name: 'Bob Driver',
            isActive: false,
            licenseNumber: 'DL345678'
          }
        ])
      });
    });

    // Mock invoices API with overdue invoices - matches apiService.getInvoices() endpoint (/billing/invoices/overdue returns only overdue invoices)
    await page.route('https://localhost:5001/api/billing/invoices/overdue', async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          items: [
            {
              id: 'inv-1',
              dueDate: '2024-11-15T00:00:00Z', // Past date
              balanceDue: 1500.00,
              status: 'Overdue'
            },
            {
              id: 'inv-3',
              dueDate: '2024-11-01T00:00:00Z', // Past date
              balanceDue: 800.00,
              status: 'Overdue'
            }
          ],
          totalCount: 2
        })
      });
    });

    await page.goto('/');
  });

  test.describe('Dashboard Statistics Cards', () => {
    test('should display dashboard stats cards with correct values', async ({ page }) => {
      // Check for stats cards with values based on mock data
      await expect(page.getByText('Total Jobs')).toBeVisible();
      await expect(page.locator('[data-testid="metric-total-jobs"]')).toHaveText('3'); // 3 total jobs from mock data

      await expect(page.getByText('Active Drivers')).toBeVisible();
      await expect(page.locator('[data-testid="metric-active-drivers"]')).toHaveText('2'); // 2 active drivers from mock data

      await expect(page.getByText('Overdue Invoices')).toBeVisible();
      await expect(page.locator('[data-testid="metric-overdue-invoices"]')).toHaveText('2'); // 2 overdue invoices from mock data
    });

    test('should display stats cards with proper styling', async ({ page }) => {
      // Check for card containers
      const statsCards = page.locator('[data-testid="stats-card"]');
      await expect(statsCards).toHaveCount(4);

      // Check for proper card styling
      const firstCard = statsCards.first();
      await expect(firstCard).toHaveClass(/bg-white.*rounded-lg.*shadow/);
    });

    test('should handle loading state for statistics', async ({ page }) => {
      // Hold the first-page jobs response back so the loading state is still showing when it is asserted
      let releaseJobs: () => void = () => {};
      const jobsHeld = new Promise<void>(resolve => { releaseJobs = resolve; });
      await page.route('**/api/job', async route => {
        await jobsHeld;
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({ items: [], totalCount: 7 })
        });
      });

      await page.reload();

      // Only Total Jobs waits for the first page of jobs: Active Jobs has its own request, and the drivers and invoices cards show their data
      await expect(page.locator('[data-testid="loading-spinner"]')).toHaveCount(1);
      await expect(page.locator('[data-testid="metric-active-jobs"]')).toHaveText('3');
      await expect(page.locator('[data-testid="metric-active-drivers"]')).toHaveText('2');
      await expect(page.locator('[data-testid="metric-overdue-invoices"]')).toHaveText('2');
      await expect(page.locator('[data-testid="metric-total-jobs"]')).toHaveCount(0);

      releaseJobs();
      await expect(page.locator('[data-testid="metric-total-jobs"]')).toHaveText('7');
      await expect(page.locator('[data-testid="loading-spinner"]')).toHaveCount(0);
    });

    test('should count Active Jobs from every job, not the first page', async ({ page }) => {
      // The first page holds 3 jobs; the server knows 120 jobs, 87 of them active
      jobsState = { items: firstPageOfJobs, totalCount: 120, activeCount: 87 };
      await page.reload();

      await expect(page.locator('[data-testid="metric-total-jobs"]')).toHaveText('120');
      await expect(page.locator('[data-testid="metric-active-jobs"]')).toHaveText('87');
    });

    test('should handle error state for statistics', async ({ page }) => {
      // The overview computes its statistics from the jobs, drivers and invoices endpoints; there is no /api/dashboard/stats endpoint
      await page.route('**/api/job', async route => {
        await route.fulfill({
          status: 500,
          contentType: 'application/json',
          body: JSON.stringify({ error: 'Internal server error' })
        });
      });

      await page.reload();

      // One error message, naming only what failed
      await expect(page.getByText('Error loading statistics (jobs)')).toBeVisible();
      // Total Jobs shows an error, never a 0
      await expect(page.locator('[data-testid="metric-total-jobs"]')).toHaveCount(0);
      // The cards whose own requests succeeded still show their data, Active Jobs included
      await expect(page.locator('[data-testid="metric-active-jobs"]')).toHaveText('3');
      await expect(page.locator('[data-testid="metric-active-drivers"]')).toHaveText('2');
      await expect(page.locator('[data-testid="metric-overdue-invoices"]')).toHaveText('2');
    });

    test('should show an error on Active Jobs only when its own request fails', async ({ page }) => {
      await page.route(url => url.pathname === '/api/job' && url.searchParams.get('isActive') === 'true', route =>
        route.fulfill({
          status: 500,
          contentType: 'application/json',
          body: JSON.stringify({ error: 'Internal server error' })
        })
      );

      await page.reload();

      await expect(page.getByText('Error loading statistics (active jobs)')).toBeVisible();
      const activeJobsCard = page.getByTestId('stats-card').filter({ hasText: 'Active Jobs' });
      await expect(activeJobsCard.getByText('Failed to load')).toBeVisible();
      await expect(page.locator('[data-testid="metric-active-jobs"]')).toHaveCount(0);
      // Everything else loaded from its own request
      await expect(page.locator('[data-testid="metric-total-jobs"]')).toHaveText('3');
      await expect(page.getByTestId('recent-jobs').getByText('Job 1')).toBeVisible();
    });
  });

  test.describe('Navigation', () => {
    test('should navigate to different sections from sidebar', async ({ page }) => {
      // Test navigation to jobs - use more specific selector
      await page.click('nav a[href="/jobs"]:visible');
      await expect(page).toHaveURL('/jobs');

      // Navigate back to dashboard
      await page.goto('/');

      // Test navigation to drivers - use more specific selector
      await page.click('nav a[href="/drivers"]:visible');
      await expect(page).toHaveURL('/drivers');

      // Navigate back to dashboard
      await page.goto('/');

      // Test navigation to billing - use more specific selector
      await page.click('nav a[href="/billing"]:visible');
      await expect(page).toHaveURL('/billing');

      // Navigate back to dashboard
      await page.goto('/');

      // Test navigation to tracking - use more specific selector
      await page.click('nav a[href="/tracking"]:visible');
      await expect(page).toHaveURL('/tracking');
    });

    test('should highlight active navigation item', async ({ page }) => {
      // Check dashboard is active initially
      const dashboardLink = page.locator('nav a[href="/"]:visible').first();
      await expect(dashboardLink).toHaveClass(/bg-blue-50.*text-blue-700/);

      // Navigate to jobs and check active state
      await page.click('nav a[href="/jobs"]:visible');
      await expect(page).toHaveURL('/jobs');
      const jobsLink = page.locator('nav a[href="/jobs"]:visible').first();
      await expect(jobsLink).toHaveClass(/bg-blue-50.*text-blue-700/);

      // The dashboard link is no longer active
      const inactiveDashboardLink = page.locator('nav a[href="/"]:visible').first();
      await expect(inactiveDashboardLink).toHaveClass(/text-gray-600/);

      // Navigate back to dashboard
      await page.goto('/');
    });

    test('should display user profile information', async ({ page }) => {
      // Check for user profile section
      await expect(page.getByText('Admin User')).toBeVisible();
      await expect(page.getByText('admin@hotshotlogistics.com')).toBeVisible();
    });
  });

  test.describe('Recent Activity Section', () => {
    const recentJobsResponse = {
      items: [
        {
          id: 'job-1',
          title: 'Urgent Delivery',
          pickupAddress: '123 Main St',
          dropoffAddress: '456 Oak Ave',
          amount: 100,
          status: 'EnRoute',
          assignedDriverId: 1,
          scheduledPickupTime: '2024-12-01T10:00:00Z'
        },
        {
          id: 'job-2',
          title: 'Standard Delivery',
          pickupAddress: '789 Pine St',
          dropoffAddress: '321 Elm Ave',
          amount: 200,
          status: 'Pending',
          assignedDriverId: null,
          scheduledPickupTime: '2024-12-01T14:00:00Z'
        }
      ],
      totalCount: 2
    };

    test('should display recent jobs section', async ({ page }) => {
      await page.route('**/api/job', route => route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(recentJobsResponse)
      }));
      await page.reload();

      // Check recent jobs section
      await expect(page.getByText('Recent Jobs')).toBeVisible();

      // Check job entries
      await expect(page.getByText('Urgent Delivery')).toBeVisible();
      await expect(page.getByText('Standard Delivery')).toBeVisible();

      // Check job details
      await expect(page.getByText('123 Main St → 456 Oak Ave')).toBeVisible();
      await expect(page.getByText('789 Pine St → 321 Elm Ave')).toBeVisible();
    });

    test('should display job status badges with correct styling', async ({ page }) => {
      await page.route('**/api/job', route => route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(recentJobsResponse)
      }));
      await page.reload();

      // Check status badges
      const recentJobs = page.getByTestId('recent-jobs');
      const enRouteBadge = recentJobs.getByText('EnRoute', { exact: true });
      await expect(enRouteBadge).toBeVisible();
      await expect(enRouteBadge).toHaveClass(/bg-green-100.*text-green-800/);

      const pendingBadge = recentJobs.getByText('Pending', { exact: true });
      await expect(pendingBadge).toBeVisible();
      await expect(pendingBadge).toHaveClass(/bg-yellow-100.*text-yellow-800/);
    });

    test('should show a loading state in recent jobs while the jobs request is pending', async ({ page }) => {
      let releaseJobs: () => void = () => {};
      const jobsHeld = new Promise<void>(resolve => { releaseJobs = resolve; });
      await page.route('**/api/job', async route => {
        await jobsHeld;
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify(recentJobsResponse)
        });
      });
      await page.reload();

      const recentJobs = page.getByTestId('recent-jobs');
      await expect(recentJobs.getByTestId('recent-jobs-loading')).toBeVisible();
      await expect(recentJobs.getByText('No recent jobs')).toHaveCount(0);

      releaseJobs();
      await expect(recentJobs.getByText('Urgent Delivery')).toBeVisible();
      await expect(recentJobs.getByTestId('recent-jobs-loading')).toHaveCount(0);
    });

    test('should show an error state in recent jobs when the jobs request fails', async ({ page }) => {
      await page.route('**/api/job', route => route.fulfill({
        status: 500,
        contentType: 'application/json',
        body: JSON.stringify({ error: 'Internal server error' })
      }));
      await page.reload();

      const recentJobs = page.getByTestId('recent-jobs');
      await expect(recentJobs.getByText('Error loading recent jobs')).toBeVisible();
      await expect(recentJobs.getByText('No recent jobs')).toHaveCount(0);
    });

    test('should show an error state when the jobs response has an unexpected shape', async ({ page }) => {
      await page.route('**/api/job', route => route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ unexpected: true })
      }));
      await page.reload();

      await expect(page.getByTestId('recent-jobs').getByText('Error loading recent jobs')).toBeVisible();
      await expect(page.getByText('Error loading statistics')).toBeVisible();
    });

    test('should read the numeric job statuses the API sends', async ({ page }) => {
      // The API serialises JobStatus as its number: 0 Pending, 1 Assigned, 2 EnRoute, 3 Received
      await page.route('**/api/job', route => route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          items: [
            { id: 'job-1', title: 'Numeric Pending', pickupAddress: 'a', dropoffAddress: 'b', status: 0 },
            { id: 'job-2', title: 'Numeric En Route', pickupAddress: 'c', dropoffAddress: 'd', status: 2 },
            { id: 'job-3', title: 'Numeric Received', pickupAddress: 'e', dropoffAddress: 'f', status: 3 }
          ],
          totalCount: 3
        })
      }));
      await page.reload();

      const recentJobs = page.getByTestId('recent-jobs');
      await expect(recentJobs.getByText('Pending', { exact: true })).toBeVisible();
      await expect(recentJobs.getByText('EnRoute', { exact: true })).toBeVisible();
      await expect(recentJobs.getByText('Received', { exact: true })).toBeVisible();
    });

    test('should show an error state when a job has a status the server does not define', async ({ page }) => {
      await page.route('**/api/job', route => route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          items: [{ id: 'job-1', title: 'Stale Status', pickupAddress: 'a', dropoffAddress: 'b', status: 'InProgress' }],
          totalCount: 1
        })
      }));
      await page.reload();

      await expect(page.getByTestId('recent-jobs').getByText('Error loading recent jobs')).toBeVisible();
    });

    test('should handle empty recent jobs list', async ({ page }) => {
      // Mock empty jobs response (the jobs endpoint is /api/job)
      await page.route('**/api/job', async route => {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            items: [],
            totalCount: 0
          })
        });
      });

      await page.reload();

      // Check empty state
      await expect(page.getByText('No recent jobs')).toBeVisible();
    });

    test('should link to full jobs list', async ({ page }) => {
      // Check "View All Jobs" link
      const viewAllLink = page.locator('a[href="/jobs"]').filter({ hasText: 'View All Jobs' });
      await expect(viewAllLink).toBeVisible();
      
      // Click and verify navigation
      await viewAllLink.click();
      await expect(page).toHaveURL('/jobs');
    });
  });

  test.describe('Quick Actions', () => {
    test('should display quick action buttons', async ({ page }) => {
      // Check for quick action buttons
      await expect(page.getByText('Create New Job')).toBeVisible();
      await expect(page.getByText('Add Driver')).toBeVisible();
      await expect(page.getByText('Generate Invoice')).toBeVisible();
    });

    test('should navigate to create job form', async ({ page }) => {
      // Click create job button
      await page.click('text=Create New Job');
      await expect(page).toHaveURL('/jobs?action=create');
      // The Jobs page opens its create form from the action
      await expect(page.getByRole('heading', { name: 'Create New Job' })).toBeVisible();
    });

    test('should navigate to add driver form', async ({ page }) => {
      // Click add driver button
      await page.click('text=Add Driver');
      await expect(page).toHaveURL('/drivers?action=add');
    });

    test('should navigate to invoice generation', async ({ page }) => {
      // Click generate invoice button
      await page.click('text=Generate Invoice');
      await expect(page).toHaveURL('/billing?action=generate');
    });
  });

  test.describe('Real-time Updates', () => {
    test('should refresh the statistics and recent jobs when the server pushes a job status update', async ({ page }) => {
      const signalR = await mockSignalR(page);
      await page.reload();
      await expect.poll(() => signalR.openConnections()).toBe(1);
      await expect(page.locator('[data-testid="metric-total-jobs"]')).toHaveText('3');
      await expect(page.locator('[data-testid="metric-active-jobs"]')).toHaveText('3');

      // The server's data changes: a fourth job exists, and it is not the one the update names
      jobsState = {
        items: [
          ...firstPageOfJobs,
          { ...firstPageOfJobs[0], id: 'job-4', title: 'Job 4', status: 'Pending' }
        ],
        totalCount: 4,
        activeCount: 4
      };

      // Push the event under the name the server sends (IRealtimeHubClient.JobStatusUpdated), spelled out here so the
      // test fails if the client listens for any other name. The handler registers just after the connection opens,
      // so push again until the page has reacted.
      await expect(async () => {
        signalR.invoke('JobStatusUpdated', 'job-2', 1);
        await expect(page.locator('[data-testid="metric-total-jobs"]')).toHaveText('4', { timeout: 1000 });
      }).toPass();
      await expect(page.locator('[data-testid="metric-active-jobs"]')).toHaveText('4');
      await expect(page.getByTestId('recent-jobs').getByText('Job 4')).toBeVisible();
    });

    test('should tell the user when live updates are unavailable', async ({ page }) => {
      await page.route('**/realtime/negotiate**', route => route.fulfill({ status: 500 }));
      await page.reload();

      await expect(page.getByRole('alert').filter({ hasText: 'Live updates are unavailable' })).toBeVisible();
      // The statistics still load from the API
      await expect(page.locator('[data-testid="metric-total-jobs"]')).toHaveText('3');
    });

    test('should close the live connection when leaving the overview', async ({ page }) => {
      const signalR = await mockSignalR(page);
      await page.reload();
      await expect.poll(() => signalR.openConnections()).toBe(1);

      await page.click('nav a[href="/jobs"]:visible');
      await expect(page).toHaveURL('/jobs');

      await expect.poll(() => signalR.openConnections()).toBe(0);
    });
  });

  test.describe('Responsive Design', () => {
    test('should display correctly on mobile devices', async ({ page }) => {
      // Set mobile viewport
      await page.setViewportSize({ width: 375, height: 667 });
      await page.reload();

      // Check that stats cards stack vertically on mobile
      const statsGrid = page.locator('.grid').first();
      await expect(statsGrid).toHaveClass(/grid-cols-1.*sm:grid-cols-2.*lg:grid-cols-4/);

      // The layout's sidebar is hidden below the lg breakpoint, so only the overview's own top
      // navigation shows its links
      await expect(page.locator('nav:visible a[href="/jobs"]')).toHaveCount(1);
    });

    test('should display correctly on tablet devices', async ({ page }) => {
      // Set tablet viewport
      await page.setViewportSize({ width: 768, height: 1024 });
      await page.reload();

      // Check that stats cards display in 2 columns on tablet
      const statsGrid = page.locator('.grid').first();
      await expect(statsGrid).toHaveClass(/sm:grid-cols-2/);
    });

    test('should display correctly on desktop', async ({ page }) => {
      // Set desktop viewport
      await page.setViewportSize({ width: 1200, height: 800 });
      await page.reload();

      // Check that stats cards display in 4 columns on desktop
      const statsGrid = page.locator('.grid').first();
      await expect(statsGrid).toHaveClass(/lg:grid-cols-4/);

      // Check that the sidebar is visible alongside the overview's own top navigation
      await expect(page.locator('nav:visible a[href="/jobs"]')).toHaveCount(2);
    });
  });

  test.describe('Key Metrics Non-Zero Validation', () => {
    test('should display non-zero values for key metrics', async ({ page }) => {
      // One of the three jobs has been received, so it is counted in Total Jobs but not in Active Jobs
      jobsState = {
        items: [firstPageOfJobs[0], firstPageOfJobs[1], { ...firstPageOfJobs[2], status: 'Received' }],
        totalCount: 3,
        activeCount: 2
      };
      await page.reload();

      await expect(page.locator('[data-testid="metric-total-jobs"]')).toHaveText('3');
      await expect(page.locator('[data-testid="metric-active-jobs"]')).toHaveText('2');
      await expect(page.locator('[data-testid="metric-active-drivers"]')).toHaveText('2'); // 2 of the 3 mocked drivers are active
      await expect(page.locator('[data-testid="metric-overdue-invoices"]')).toHaveText('2'); // 2 overdue invoices returned by the overdue endpoint
    });
  });

  test.describe('Performance and Loading', () => {
    test('should load dashboard within acceptable time', async ({ page }) => {
      const startTime = Date.now();
      await page.goto('/');

      // Wait for main content to be visible
      await expect(page.getByText('Dashboard Overview')).toBeVisible();

      const loadTime = Date.now() - startTime;
      expect(loadTime).toBeLessThan(3000); // Should load within 3 seconds
    });

    test('should handle concurrent API requests efficiently', async ({ page }) => {
      // beforeEach returns once the page has loaded, before its own API requests finish; wait for them
      // so they are not counted below
      await expect(page.locator('[data-testid="metric-total-jobs"]')).toHaveText('3');

      let jobsRequestCount = 0;
      let activeJobsRequestCount = 0;
      let driversRequestCount = 0;
      let invoicesRequestCount = 0;

      // Count the requests the overview makes: the first page of jobs, the count of active jobs, drivers and overdue invoices
      await page.route(url => url.pathname === '/api/job', async route => {
        if (new URL(route.request().url()).searchParams.get('isActive') === 'true') {
          activeJobsRequestCount++;
        } else {
          jobsRequestCount++;
        }
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({ items: [], totalCount: 0 })
        });
      });

      await page.route('**/api/drivers*', async route => {
        driversRequestCount++;
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify([])
        });
      });

      await page.route('**/api/billing/invoices/overdue*', async route => {
        invoicesRequestCount++;
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({ items: [], totalCount: 0 })
        });
      });

      await page.reload();
      await expect(page.locator('[data-testid="metric-total-jobs"]')).toHaveText('0');
      await expect(page.locator('[data-testid="metric-active-jobs"]')).toHaveText('0');
      await expect(page.locator('[data-testid="metric-active-drivers"]')).toHaveText('0');
      await expect(page.locator('[data-testid="metric-overdue-invoices"]')).toHaveText('0');

      // Every statistic is fetched once
      expect(jobsRequestCount).toBe(1);
      expect(activeJobsRequestCount).toBe(1);
      expect(driversRequestCount).toBe(1);
      expect(invoicesRequestCount).toBe(1);
    });
  });
});
