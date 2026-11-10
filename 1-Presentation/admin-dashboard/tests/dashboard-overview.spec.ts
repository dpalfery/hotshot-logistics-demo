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

    // Mock jobs API with realistic data - matches apiService.getJobs() endpoint
    await page.route('https://localhost:5001/api/job', async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          items: [
            {
              id: 'job-1',
              status: 'InProgress',
              assignedDriverId: 1,
              scheduledPickupTime: '2024-12-01T10:00:00Z'
            },
            {
              id: 'job-2',
              status: 'Pending',
              assignedDriverId: null,
              scheduledPickupTime: '2024-12-01T14:00:00Z'
            },
            {
              id: 'job-3',
              status: 'InProgress',
              assignedDriverId: 2,
              scheduledPickupTime: '2024-12-01T16:00:00Z'
            }
          ],
          totalCount: 3
        })
      });
    });

    // Mock drivers API with active drivers - matches apiService.getDrivers() endpoint
    await page.route('https://localhost:5001/api/driver', async route => {
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

    // Mock invoices API with overdue invoices - matches apiService.getInvoices() endpoint
    await page.route('https://localhost:5001/api/billing/invoices', async route => {
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
              id: 'inv-2',
              dueDate: '2024-12-15T00:00:00Z', // Future date
              balanceDue: 2500.00,
              status: 'Pending'
            },
            {
              id: 'inv-3',
              dueDate: '2024-11-01T00:00:00Z', // Past date
              balanceDue: 800.00,
              status: 'Overdue'
            }
          ],
          totalCount: 3
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
      // Mock delayed response
      await page.route('**/api/dashboard/stats', async route => {
        await new Promise(resolve => setTimeout(resolve, 1000));
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            totalJobs: 3,
            activeDrivers: 2,
            revenue: 4800.00,
            pendingJobs: 1
          })
        });
      });

      await page.reload();

      // Check for loading indicators
      const loadingIndicators = page.locator('[data-testid="loading-spinner"]');
      if (await loadingIndicators.first().isVisible()) {
        await expect(loadingIndicators).toHaveCount(4);
      }

      // Wait for data to load
      await expect(page.locator('[data-testid="metric-total-jobs"]')).toHaveText('3');
    });

    test('should handle error state for statistics', async ({ page }) => {
      // Mock error response
      await page.route('**/api/dashboard/stats', async route => {
        await route.fulfill({
          status: 500,
          contentType: 'application/json',
          body: JSON.stringify({ error: 'Internal server error' })
        });
      });

      await page.reload();

      // Check for error state
      await expect(page.getByText('Error loading statistics')).toBeVisible();
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
      const jobsLink = page.locator('nav a[href="/jobs"]:visible').first();
      await expect(jobsLink).toHaveClass(/text-gray-500/);

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
    test('should display recent jobs section', async ({ page }) => {
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
      // Check status badges
      const inProgressBadge = page.locator('text=InProgress').first();
      await expect(inProgressBadge).toBeVisible();
      await expect(inProgressBadge).toHaveClass(/bg-green-100.*text-green-800/);

      const pendingBadge = page.locator('text=Pending').first();
      await expect(pendingBadge).toBeVisible();
      await expect(pendingBadge).toHaveClass(/bg-yellow-100.*text-yellow-800/);
    });

    test('should handle empty recent jobs list', async ({ page }) => {
      // Mock empty jobs response
      await page.route('**/api/jobs*', async route => {
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
    test('should handle real-time statistics updates', async ({ page }) => {
      // Initial load
      await expect(page.locator('[data-testid="metric-total-jobs"]')).toHaveText('3');

      // Mock updated statistics
      await page.route('**/api/dashboard/stats', async route => {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            totalJobs: 4,
            activeDrivers: 3,
            revenue: 5200.00,
            pendingJobs: 1
          })
        });
      });

      // Simulate real-time update (would normally come via SignalR)
      await page.evaluate(() => {
        // Trigger a refetch of statistics
        window.dispatchEvent(new CustomEvent('dashboard-update'));
      });

      // Check updated values
      await expect(page.locator('[data-testid="metric-total-jobs"]')).toHaveText('4');
      await expect(page.locator('[data-testid="metric-active-drivers"]')).toHaveText('3');
      await expect(page.getByText('$5,200.00')).toBeVisible();
      await expect(page.getByText('1')).toBeVisible();
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

      // Check that sidebar is visible on mobile (no responsive hiding in current implementation)
      const sidebar = page.locator('nav').first();
      await expect(sidebar).toBeVisible();
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

      // Check that sidebar is visible on desktop
      const sidebar = page.locator('nav').first();
      await expect(sidebar).toBeVisible();
    });
  });

  test.describe('Key Metrics Non-Zero Validation', () => {
    test('should display non-zero values for key metrics', async ({ page }) => {
      // Set longer timeout for this test since it needs to wait for Next.js to fully load
      test.setTimeout(30000);
      
      // Log all network requests to debug the routing issue
      page.on('request', request => {
        if (request.url().includes('/api/')) {
          console.log('API Request:', request.method(), request.url());
        }
      });
      
      page.on('response', response => {
        if (response.url().includes('/api/')) {
          console.log('API Response:', response.status(), response.url());
        }
      });
      
      // Mock jobs API with realistic data - matches apiService.getJobs() endpoint
      await page.route('**/api/job**', async route => {
        console.log('Intercepted jobs API call:', route.request().url());
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            items: [
              {
                id: 'job-1',
                status: 'InProgress',
                assignedDriverId: 1,
                scheduledPickupTime: '2024-12-01T10:00:00Z'
              },
              {
                id: 'job-2',
                status: 'Pending',
                assignedDriverId: null,
                scheduledPickupTime: '2024-12-01T14:00:00Z'
              },
              {
                id: 'job-3',
                status: 'InProgress',
                assignedDriverId: 2,
                scheduledPickupTime: '2024-12-01T16:00:00Z'
              }
            ],
            totalCount: 3
          })
        });
      });

      // Mock drivers API with active drivers - matches apiService.getDrivers() endpoint
      await page.route('**/api/driver**', async route => {
        console.log('Intercepted drivers API call:', route.request().url());
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

      // Mock invoices API with overdue invoices - matches apiService.getInvoices() endpoint
      await page.route('**/api/billing/invoices**', async route => {
        console.log('Intercepted invoices API call:', route.request().url());
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
                id: 'inv-2',
                dueDate: '2024-12-15T00:00:00Z', // Future date
                balanceDue: 2500.00,
                status: 'Pending'
              },
              {
                id: 'inv-3',
                dueDate: '2024-11-01T00:00:00Z', // Past date
                balanceDue: 800.00,
                status: 'Overdue'
              }
            ],
            totalCount: 3
          })
        });
      });

      await page.goto('/');
      await page.waitForLoadState('networkidle');
  
      // Wait for Next.js to fully load and render
      await page.waitForSelector('h1:has-text("Dashboard Overview")', { timeout: 10000 });
  
      // Wait for metrics to be calculated and displayed
      await page.waitForTimeout(2000);
  
      // Debug: Check if the page content is loaded
      const pageTitle = await page.textContent('h1');
      console.log('Page title found:', pageTitle);
  
      // Debug: Check if any metric cards are present
      const metricCards = await page.locator('[data-testid^="metric-"]').count();
      console.log('Number of metric cards found:', metricCards);

      // Debug: Check if the page content is loaded first
      console.log('Page title found:', pageTitle);

      // Debug: Check if any metric cards are present
      const metricCardsCount = await page.locator('[data-testid^="metric-"]').count();
      console.log('Number of metric cards found:', metricCardsCount);

      // If no metric cards are found, the page isn't rendering properly
      if (metricCardsCount === 0) {
        const bodyContent = await page.textContent('body');
        console.log('Body content length:', bodyContent?.length || 0);
        console.log('Body content preview:', bodyContent?.substring(0, 500) || 'No content');

        // Check if we're being redirected to login
        const currentUrl = page.url();
        console.log('Current URL:', currentUrl);
      }

      // Extract and log the actual metric values for debugging
      const totalJobsValue = await page.textContent('[data-testid="metric-total-jobs"]');
      const activeJobsValue = await page.textContent('[data-testid="metric-active-jobs"]');
      const activeDriversValue = await page.textContent('[data-testid="metric-active-drivers"]');
      const overdueInvoicesValue = await page.textContent('[data-testid="metric-overdue-invoices"]');

      console.log('Dashboard Metrics Debug Info:');
      console.log('Total Jobs:', totalJobsValue);
      console.log('Active Jobs:', activeJobsValue);
      console.log('Active Drivers:', activeDriversValue);
      console.log('Overdue Invoices:', overdueInvoicesValue);

      // Assert that each metric displays a non-zero value
      expect(parseInt(totalJobsValue || '0')).toBeGreaterThan(0);
      expect(parseInt(activeJobsValue || '0')).toBeGreaterThan(0);
      expect(parseInt(activeDriversValue || '0')).toBeGreaterThan(0);
      expect(parseInt(overdueInvoicesValue || '0')).toBeGreaterThan(0);

      // Additional assertions for expected values based on mock data
      expect(parseInt(totalJobsValue || '0')).toBe(3); // 3 total jobs
      expect(parseInt(activeJobsValue || '0')).toBe(2); // 2 jobs InProgress
      expect(parseInt(activeDriversValue || '0')).toBe(2); // 2 active drivers
      expect(parseInt(overdueInvoicesValue || '0')).toBe(2); // 2 overdue invoices (from 3 total, 2 are overdue)
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
      let statsRequestCount = 0;
      let jobsRequestCount = 0;

      // Count API requests
      await page.route('**/api/dashboard/stats', async route => {
        statsRequestCount++;
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            totalJobs: 156,
            activeDrivers: 23,
            revenue: 45678.90,
            pendingJobs: 12
          })
        });
      });

      await page.route('**/api/jobs*', async route => {
        jobsRequestCount++;
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
      await page.waitForTimeout(1000);

      // Should make only one request to each endpoint
      expect(statsRequestCount).toBe(1);
      expect(jobsRequestCount).toBe(1);
    });
  });
});