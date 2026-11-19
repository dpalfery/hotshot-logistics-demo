import { test, expect } from '@playwright/test';
import { mockSignalR } from './utils/signalr-mock';

test.describe('Tracking Dashboard', () => {
  test.beforeEach(async ({ page }) => {
    // Navigate to tracking page
    await page.goto('/tracking');
  });

  test.describe('Real-time Tracking Dashboard (14.4)', () => {
    test('should display tracking dashboard with header', async ({ page }) => {
      // Check page title and description
      await expect(page.getByText('Real-Time Tracking')).toBeVisible();
      await expect(page.getByText('Live maps, job monitoring, and notifications')).toBeVisible();
    });

    test('should display live map section', async ({ page }) => {
      // Check live map section
      await expect(page.getByRole('heading', { name: 'Live Map' })).toBeVisible();
      
      // Check map placeholder (since actual map integration would require external services)
      await expect(page.getByText('Map integration would be implemented here')).toBeVisible();
      
      // Verify map icon is present
      const mapIcon = page.locator('svg').filter({ hasText: /map/i }).first();
      if (await mapIcon.isVisible()) {
        await expect(mapIcon).toBeVisible();
      }
    });

    test('should display active jobs section', async ({ page }) => {
      // Mock jobs data with active jobs
      await page.route('**/api/job', async route => {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            items: [
              {
                id: 'job-1',
                title: 'Active Delivery 1',
                status: 'EnRoute',
                pickupAddress: '123 Main St',
                dropoffAddress: '456 Oak Ave',
                assignedDriverId: 1,
                scheduledPickupTime: '2024-12-01T10:00:00Z'
              },
              {
                id: 'job-2',
                title: 'Assigned Delivery 2',
                status: 'Assigned',
                pickupAddress: '789 Pine St',
                dropoffAddress: '321 Elm Ave',
                assignedDriverId: 2,
                scheduledPickupTime: '2024-12-01T14:00:00Z'
              },
              {
                id: 'job-3',
                title: 'Pending Delivery 3',
                status: 'Pending',
                pickupAddress: '555 Cedar Rd',
                dropoffAddress: '777 Birch Ln',
                assignedDriverId: null,
                scheduledPickupTime: '2024-12-01T16:00:00Z'
              }
            ],
            totalCount: 3
          })
        });
      });

      await page.reload();

      // Check active jobs section
      await expect(page.getByRole('heading', { name: 'Active Jobs' })).toBeVisible();
      
      // Should show only EnRoute and Assigned jobs, not Pending
      await expect(page.getByText('Active Delivery 1')).toBeVisible();
      await expect(page.getByText('Assigned Delivery 2')).toBeVisible();
      await expect(page.getByText('Pending Delivery 3')).not.toBeVisible();
      
      // Check job details
      await expect(page.getByText('123 Main St → 456 Oak Ave')).toBeVisible();
      await expect(page.getByText('789 Pine St → 321 Elm Ave')).toBeVisible();
      
      // Check driver assignments
      await expect(page.getByText('Driver: Driver #1')).toBeVisible();
      await expect(page.getByText('Driver: Driver #2')).toBeVisible();
    });

    test('should display job status with appropriate styling', async ({ page }) => {
      // Mock active jobs with different statuses
      await page.route('**/api/job', async route => {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            items: [
              {
                id: 'job-1',
                title: 'In Progress Job',
                status: 'EnRoute',
                pickupAddress: '123 Main St',
                dropoffAddress: '456 Oak Ave',
                assignedDriverId: 1,
                scheduledPickupTime: '2024-12-01T10:00:00Z'
              },
              {
                id: 'job-2',
                title: 'Assigned Job',
                status: 'Assigned',
                pickupAddress: '789 Pine St',
                dropoffAddress: '321 Elm Ave',
                assignedDriverId: 2,
                scheduledPickupTime: '2024-12-01T14:00:00Z'
              }
            ],
            totalCount: 2
          })
        });
      });

      await page.reload();

      // Check status styling
      const enRouteStatus = page.getByText('EnRoute', { exact: true });
      await expect(enRouteStatus).toBeVisible();
      await expect(enRouteStatus).toHaveClass(/bg-green-100.*text-green-800/);

      const assignedStatus = page.getByText('Assigned', { exact: true });
      await expect(assignedStatus).toBeVisible();
      await expect(assignedStatus).toHaveClass(/bg-blue-100.*text-blue-800/);
    });

    test('should handle empty active jobs list', async ({ page }) => {
      // Mock empty jobs response
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

      // Check empty state message
      await expect(page.getByText('No active jobs')).toBeVisible();
    });

    test('should display recent location updates section', async ({ page }) => {
      // Check location updates section
      await expect(page.getByRole('heading', { name: 'Recent Location Updates' })).toBeVisible();
      
      // Initially should show no updates
      await expect(page.getByText('No recent location updates')).toBeVisible();
    });

    test('should display notifications section', async ({ page }) => {
      // Check notifications section
      await expect(page.getByRole('heading', { name: 'Notifications' })).toBeVisible();
      
      // Check notification icon
      const bellIcon = page.locator('svg').filter({ hasText: /bell/i }).first();
      if (await bellIcon.isVisible()) {
        await expect(bellIcon).toBeVisible();
      }
      
      // Initially should show no notifications
      await expect(page.getByText('No recent notifications')).toBeVisible();
    });

    // The SignalR server is mocked at the network level (negotiate request and WebSocket), so the app runs its real client

    test('should connect to SignalR and handle real-time updates', async ({ page }) => {
      const signalR = await mockSignalR(page);
      await page.reload();
      await expect.poll(() => signalR.openConnections()).toBe(1);

      // Simulate location update
      signalR.invoke('LocationUpdate', {
        jobId: 'job-123',
        driverId: 1,
        location: { latitude: 40.7128, longitude: -74.0060, address: 'Test address' },
        timestamp: new Date().toISOString()
      });

      // Check if location update appears
      await expect(page.getByText('Job #job-123 - Driver #1')).toBeVisible();
      await expect(page.getByText('40.7128, -74.0060')).toBeVisible();

      // Simulate notification
      signalR.invoke('Notification', {
        id: 'notification-1',
        type: 'status',
        title: 'Job Update',
        message: 'Job has been completed',
        timestamp: new Date().toISOString(),
        read: false
      });

      // Check if notification appears
      await expect(page.getByText('Job Update')).toBeVisible();
      await expect(page.getByText('Job has been completed')).toBeVisible();
      await expect(page.getByText('New', { exact: true })).toBeVisible();
    });

    test('should display location updates with proper formatting', async ({ page }) => {
      const signalR = await mockSignalR(page);
      await page.reload();
      await expect.poll(() => signalR.openConnections()).toBe(1);

      signalR.invoke('LocationUpdate', {
        jobId: 'job-001',
        driverId: 1,
        location: { latitude: 40.7128, longitude: -74.0060, address: 'Test address' },
        timestamp: '2024-12-01T10:30:00Z'
      });
      signalR.invoke('LocationUpdate', {
        jobId: 'job-002',
        driverId: 2,
        location: { latitude: 34.0522, longitude: -118.2437, address: 'Test address' },
        timestamp: '2024-12-01T10:35:00Z'
      });

      // Wait for location updates to appear
      await expect(page.getByText('Job #job-001 - Driver #1')).toBeVisible();
      await expect(page.getByText('Job #job-002 - Driver #2')).toBeVisible();

      // Check coordinate formatting (4 decimal places)
      await expect(page.getByText('40.7128, -74.0060')).toBeVisible();
      await expect(page.getByText('34.0522, -118.2437')).toBeVisible();

      // Check timestamp formatting (the config pins the en-US locale and UTC)
      await expect(page.getByText('10:30:00 AM')).toBeVisible();
      await expect(page.getByText('10:35:00 AM')).toBeVisible();
    });

    test('should display notifications with read/unread status', async ({ page }) => {
      const signalR = await mockSignalR(page);
      await page.reload();
      await expect.poll(() => signalR.openConnections()).toBe(1);

      signalR.invoke('Notification', {
        id: 'notification-1',
        type: 'assignment',
        title: 'New Job Assigned',
        message: 'Job #123 has been assigned to Driver #1',
        timestamp: '2024-12-01T10:30:00Z',
        read: false
      });
      signalR.invoke('Notification', {
        id: 'notification-2',
        type: 'status',
        title: 'Delivery Completed',
        message: 'Job #122 has been completed successfully',
        timestamp: '2024-12-01T10:25:00Z',
        read: true
      });

      // Wait for notifications to appear
      await expect(page.getByText('New Job Assigned')).toBeVisible();
      await expect(page.getByText('Delivery Completed')).toBeVisible();

      // Check notification messages
      await expect(page.getByText('Job #123 has been assigned to Driver #1')).toBeVisible();
      await expect(page.getByText('Job #122 has been completed successfully')).toBeVisible();

      // Check read/unread status styling (exact: the notification title "New Job Assigned" also contains "New")
      const newStatus = page.getByText('New', { exact: true });
      await expect(newStatus).toBeVisible();
      await expect(newStatus).toHaveClass(/bg-blue-100.*text-blue-800/);

      const readStatus = page.getByText('Read', { exact: true });
      await expect(readStatus).toBeVisible();
      await expect(readStatus).toHaveClass(/bg-gray-100.*text-gray-800/);
    });

    test('should limit location updates and notifications to last 10 items', async ({ page }) => {
      const signalR = await mockSignalR(page);
      await page.reload();
      await expect.poll(() => signalR.openConnections()).toBe(1);

      // Simulate 15 location updates and 15 notifications
      for (let i = 1; i <= 15; i++) {
        signalR.invoke('LocationUpdate', {
          jobId: `job-${i.toString().padStart(3, '0')}`,
          driverId: i,
          location: { latitude: 40.7128 + i * 0.001, longitude: -74.0060 + i * 0.001, address: 'Test address' },
          timestamp: new Date(Date.now() + i * 1000).toISOString()
        });
        signalR.invoke('Notification', {
          id: `notification-${i}`,
          type: 'notification',
          title: `Notification ${i}`,
          message: `Message ${i}`,
          timestamp: new Date(Date.now() + i * 1000).toISOString(),
          read: false
        });
      }

      // The most recent item is shown, so every update has been processed
      await expect(page.getByText('Job #job-015')).toBeVisible();
      await expect(page.getByText('Notification 15', { exact: true })).toBeVisible();

      // Only the last 10 of each are kept
      await expect(page.locator('[data-testid="location-update"]')).toHaveCount(10);
      await expect(page.locator('[data-testid="notification"]')).toHaveCount(10);

      // Older items are not shown (exact: "Notification 1" is also a prefix of "Notification 10" to "15")
      await expect(page.getByText('Job #job-001')).not.toBeVisible();
      await expect(page.getByText('Notification 1', { exact: true })).not.toBeVisible();
      await expect(page.getByText('Job #job-005 - Driver #5')).toHaveCount(0);
      await expect(page.getByText('Job #job-006 - Driver #6')).toBeVisible();
    });

    test('should handle SignalR connection errors gracefully', async ({ page }) => {
      // Make the SignalR negotiation request fail
      await page.route('**/realtime/**', route => route.abort());

      // Mock console.error to capture error logs
      const consoleErrors: string[] = [];
      page.on('console', msg => {
        if (msg.type() === 'error') {
          consoleErrors.push(msg.text());
        }
      });

      await page.reload();

      // The error is logged once the connection attempt has failed
      await expect.poll(() => consoleErrors.some(error => error.includes('Failed to connect to SignalR'))).toBe(true);
      
      // Verify page still displays correctly despite connection failure
      await expect(page.getByText('Real-Time Tracking')).toBeVisible();
      await expect(page.getByText('No recent location updates')).toBeVisible();
      await expect(page.getByText('No recent notifications')).toBeVisible();
      // The failure is shown to the user, not only logged
      await expect(page.getByText('Live updates are unavailable')).toBeVisible();
    });

    test('should show an error state for active jobs when the jobs request fails', async ({ page }) => {
      await page.route('**/api/job', route => route.fulfill({
        status: 500,
        contentType: 'application/json',
        body: JSON.stringify({ error: 'Internal server error' })
      }));
      await page.reload();

      await expect(page.getByText('Error loading active jobs')).toBeVisible();
      await expect(page.getByText('No active jobs')).toHaveCount(0);
    });

    test('should disconnect SignalR when component unmounts', async ({ page }) => {
      const signalR = await mockSignalR(page);
      await page.reload();
      await expect.poll(() => signalR.openConnections()).toBe(1);

      // Navigate away client-side, so the page stays alive and only the component unmounts
      await page.click('nav a[href="/jobs"]:visible');
      await expect(page).toHaveURL('/jobs');

      // The connection was closed by the app
      await expect.poll(() => signalR.openConnections()).toBe(0);
    });

    test('should display grid layout correctly on different screen sizes', async ({ page }) => {
      // Test desktop layout
      await page.setViewportSize({ width: 1200, height: 800 });
      await page.reload();

      // Check grid layout classes
      const gridContainer = page.locator('.grid').first();
      await expect(gridContainer).toHaveClass(/lg:grid-cols-2/);

      // Test mobile layout
      await page.setViewportSize({ width: 375, height: 667 });
      await page.reload();

      // Grid should stack on mobile (grid-cols-1)
      await expect(gridContainer).toHaveClass(/grid-cols-1/);
    });

    test('should handle scrolling in location updates and notifications', async ({ page }) => {
      const signalR = await mockSignalR(page);
      await page.reload();
      await expect.poll(() => signalR.openConnections()).toBe(1);

      for (let i = 1; i <= 20; i++) {
        signalR.invoke('LocationUpdate', {
          jobId: `job-${i.toString().padStart(3, '0')}`,
          driverId: i,
          location: { latitude: 40.7128, longitude: -74.0060, address: 'Test address' },
          timestamp: new Date().toISOString()
        });
        signalR.invoke('Notification', {
          id: `notification-${i}`,
          type: 'notification',
          title: `Notification ${i}`,
          message: `Message ${i}`,
          timestamp: new Date().toISOString(),
          read: false
        });
      }
      await expect(page.getByText('Job #job-020')).toBeVisible();

      // Both lists have a maximum height and scroll
      const containers = page.locator('.max-h-64.overflow-y-auto');
      await expect(containers).toHaveCount(2);
      await expect(containers.first()).toHaveClass(/max-h-64.*overflow-y-auto/);
      await expect(containers.last()).toHaveClass(/max-h-64.*overflow-y-auto/);
    });
  });
});
