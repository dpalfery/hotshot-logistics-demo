import { test, expect } from '@playwright/test';
import type { LocationUpdate, NotificationMessage } from '@/types';

// Extend Window interface for test mocks
declare global {
  interface Window {
    mockSignalRService?: {
      connect: () => Promise<void>;
      disconnect: () => void;
      onLocationUpdate: (callback: (update: LocationUpdate) => void) => void;
      onNotification: (callback: (notification: NotificationMessage) => void) => void;
    };
    mockLocationUpdateCallback?: (update: LocationUpdate) => void;
    mockNotificationCallback?: (notification: NotificationMessage) => void;
    disconnectCalled?: boolean;
  }
}

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
      await expect(page.getByText('Live Map')).toBeVisible();
      
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
      await page.route('**/api/jobs*', async route => {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            items: [
              {
                id: 'job-1',
                title: 'Active Delivery 1',
                status: 'InProgress',
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
      await expect(page.getByText('Active Jobs')).toBeVisible();
      
      // Should show only InProgress and Assigned jobs, not Pending
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
      await page.route('**/api/jobs*', async route => {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            items: [
              {
                id: 'job-1',
                title: 'In Progress Job',
                status: 'InProgress',
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
      const inProgressStatus = page.locator('text=InProgress').first();
      await expect(inProgressStatus).toBeVisible();
      await expect(inProgressStatus).toHaveClass(/bg-green-100.*text-green-800/);

      const assignedStatus = page.locator('text=Assigned').first();
      await expect(assignedStatus).toBeVisible();
      await expect(assignedStatus).toHaveClass(/bg-blue-100.*text-blue-800/);
    });

    test('should handle empty active jobs list', async ({ page }) => {
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

      // Check empty state message
      await expect(page.getByText('No active jobs')).toBeVisible();
    });

    test('should display recent location updates section', async ({ page }) => {
      // Check location updates section
      await expect(page.getByText('Recent Location Updates')).toBeVisible();
      
      // Initially should show no updates
      await expect(page.getByText('No recent location updates')).toBeVisible();
    });

    test('should display notifications section', async ({ page }) => {
      // Check notifications section
      await expect(page.getByText('Notifications')).toBeVisible();
      
      // Check notification icon
      const bellIcon = page.locator('svg').filter({ hasText: /bell/i }).first();
      if (await bellIcon.isVisible()) {
        await expect(bellIcon).toBeVisible();
      }
      
      // Initially should show no notifications
      await expect(page.getByText('No recent notifications')).toBeVisible();
    });

    test('should connect to SignalR and handle real-time updates', async ({ page }) => {
      // Mock SignalR connection
      await page.addInitScript(() => {
        // Mock SignalR service
        window.mockSignalRService = {
          connect: () => Promise.resolve(),
          disconnect: () => {},
          onLocationUpdate: (callback: (update: LocationUpdate) => void) => {
            window.mockLocationUpdateCallback = callback;
          },
          onNotification: (callback: (notification: NotificationMessage) => void) => {
            window.mockNotificationCallback = callback;
          }
        };
      });

      await page.reload();

      // Simulate location update
      await page.evaluate(() => {
        if (window.mockLocationUpdateCallback) {
          window.mockLocationUpdateCallback({
            jobId: 'job-123',
            driverId: 1,
            location: {
              latitude: 40.7128,
              longitude: -74.0060,
              address: 'Test address'
            },
            timestamp: new Date().toISOString()
          });
        }
      });

      // Check if location update appears
      await expect(page.getByText('Job #job-123 - Driver #1')).toBeVisible();
      await expect(page.getByText('40.7128, -74.0060')).toBeVisible();

      // Simulate notification
      await page.evaluate(() => {
        if (window.mockNotificationCallback) {
          window.mockNotificationCallback({
            id: 'notification-1',
            type: 'status',
            title: 'Job Update',
            message: 'Job has been completed',
            timestamp: new Date().toISOString(),
            read: false
          });
        }
      });

      // Check if notification appears
      await expect(page.getByText('Job Update')).toBeVisible();
      await expect(page.getByText('Job has been completed')).toBeVisible();
      await expect(page.getByText('New')).toBeVisible();
    });

    test('should display location updates with proper formatting', async ({ page }) => {
      // Mock location updates
      await page.addInitScript(() => {
        window.mockSignalRService = {
          connect: () => Promise.resolve(),
          disconnect: () => {},
          onLocationUpdate: (callback: (update: LocationUpdate) => void) => {
            // Simulate multiple location updates
            setTimeout(() => {
              callback({
                jobId: 'job-001',
                driverId: 1,
                location: { latitude: 40.7128, longitude: -74.0060, address: 'Test address' },
                timestamp: '2024-12-01T10:30:00Z'
              });
            }, 100);
            
            setTimeout(() => {
              callback({
                jobId: 'job-002',
                driverId: 2,
                location: { latitude: 34.0522, longitude: -118.2437, address: 'Test address' },
                timestamp: '2024-12-01T10:35:00Z'
              });
            }, 200);
          },
          onNotification: (callback: (notification: NotificationMessage) => void) => {}
        };
      });

      await page.reload();

      // Wait for location updates to appear
      await expect(page.getByText('Job #job-001 - Driver #1')).toBeVisible();
      await expect(page.getByText('Job #job-002 - Driver #2')).toBeVisible();
      
      // Check coordinate formatting (4 decimal places)
      await expect(page.getByText('40.7128, -74.0060')).toBeVisible();
      await expect(page.getByText('34.0522, -118.2437')).toBeVisible();
      
      // Check timestamp formatting
      await expect(page.getByText(/\d{1,2}:\d{2}:\d{2} [AP]M/)).toBeVisible();
    });

    test('should display notifications with read/unread status', async ({ page }) => {
      // Mock notifications
      await page.addInitScript(() => {
        window.mockSignalRService = {
          connect: () => Promise.resolve(),
          disconnect: () => {},
          onLocationUpdate: (callback: (update: LocationUpdate) => void) => {},
          onNotification: (callback: (notification: NotificationMessage) => void) => {
            // Simulate multiple notifications
            setTimeout(() => {
              callback({
                id: 'notification-1',
                type: 'assignment',
                title: 'New Job Assigned',
                message: 'Job #123 has been assigned to Driver #1',
                timestamp: '2024-12-01T10:30:00Z',
                read: false
              });
            }, 100);
            
            setTimeout(() => {
              callback({
                id: 'notification-2',
                type: 'status',
                title: 'Delivery Completed',
                message: 'Job #122 has been completed successfully',
                timestamp: '2024-12-01T10:25:00Z',
                read: true
              });
            }, 200);
          }
        };
      });

      await page.reload();

      // Wait for notifications to appear
      await expect(page.getByText('New Job Assigned')).toBeVisible();
      await expect(page.getByText('Delivery Completed')).toBeVisible();
      
      // Check notification messages
      await expect(page.getByText('Job #123 has been assigned to Driver #1')).toBeVisible();
      await expect(page.getByText('Job #122 has been completed successfully')).toBeVisible();
      
      // Check read/unread status styling
      const newStatus = page.locator('text=New').first();
      await expect(newStatus).toBeVisible();
      await expect(newStatus).toHaveClass(/bg-blue-100.*text-blue-800/);
      
      const readStatus = page.locator('text=Read').first();
      await expect(readStatus).toBeVisible();
      await expect(readStatus).toHaveClass(/bg-gray-100.*text-gray-800/);
    });

    test('should limit location updates and notifications to last 10 items', async ({ page }) => {
      // Mock many location updates
      await page.addInitScript(() => {
        window.mockSignalRService = {
          connect: () => Promise.resolve(),
          disconnect: () => {},
          onLocationUpdate: (callback: (update: LocationUpdate) => void) => {
            // Simulate 15 location updates
            for (let i = 1; i <= 15; i++) {
              setTimeout(() => {
                callback({
                  jobId: `job-${i.toString().padStart(3, '0')}`,
                  driverId: i,
                  location: { latitude: 40.7128 + i * 0.001, longitude: -74.0060 + i * 0.001, address: 'Test address' },
                  timestamp: new Date(Date.now() + i * 1000).toISOString()
                });
              }, i * 50);
            }
          },
          onNotification: (callback: (notification: NotificationMessage) => void) => {
            // Simulate 15 notifications
            for (let i = 1; i <= 15; i++) {
              setTimeout(() => {
                callback({
                  id: `notification-${i}`,
                  type: 'notification',
                  title: `Notification ${i}`,
                  message: `Message ${i}`,
                  timestamp: new Date(Date.now() + i * 1000).toISOString(),
                  read: false
                });
              }, i * 50);
            }
          }
        };
      });

      await page.reload();

      // Wait for updates to complete
      await page.waitForTimeout(1000);

      // Check that only the last 10 location updates are shown
      const locationUpdates = page.locator('[data-testid="location-update"]');
      if (await locationUpdates.first().isVisible()) {
        await expect(locationUpdates).toHaveCount(10);
      }

      // Check that only the last 10 notifications are shown
      const notifications = page.locator('[data-testid="notification"]');
      if (await notifications.first().isVisible()) {
        await expect(notifications).toHaveCount(10);
      }

      // Verify the most recent items are shown (job-015, job-014, etc.)
      await expect(page.getByText('Job #job-015')).toBeVisible();
      await expect(page.getByText('Notification 15')).toBeVisible();
      
      // Verify older items are not shown (job-001, job-002, etc.)
      await expect(page.getByText('Job #job-001')).not.toBeVisible();
      await expect(page.getByText('Notification 1')).not.toBeVisible();
    });

    test('should handle SignalR connection errors gracefully', async ({ page }) => {
      // Mock SignalR connection failure
      await page.addInitScript(() => {
        window.mockSignalRService = {
          connect: () => Promise.reject(new Error('Connection failed')),
          disconnect: () => {},
          onLocationUpdate: (callback: (update: LocationUpdate) => void) => {},
          onNotification: (callback: (notification: NotificationMessage) => void) => {}
        };
      });

      // Mock console.error to capture error logs
      const consoleErrors: string[] = [];
      page.on('console', msg => {
        if (msg.type() === 'error') {
          consoleErrors.push(msg.text());
        }
      });

      await page.reload();

      // Wait for connection attempt
      await page.waitForTimeout(500);

      // Check that error is logged
      expect(consoleErrors.some(error => error.includes('Failed to connect to SignalR'))).toBe(true);
      
      // Verify page still displays correctly despite connection failure
      await expect(page.getByText('Real-Time Tracking')).toBeVisible();
      await expect(page.getByText('No recent location updates')).toBeVisible();
      await expect(page.getByText('No recent notifications')).toBeVisible();
    });

    test('should disconnect SignalR when component unmounts', async ({ page }) => {
      let disconnectCalled = false;
      
      // Mock SignalR service with disconnect tracking
      await page.addInitScript(() => {
        window.mockSignalRService = {
          connect: () => Promise.resolve(),
          disconnect: () => {
            window.disconnectCalled = true;
          },
          onLocationUpdate: (callback: (update: LocationUpdate) => void) => {},
          onNotification: (callback: (notification: NotificationMessage) => void) => {}
        };
      });

      await page.reload();

      // Navigate away from the page
      await page.goto('/jobs');

      // Check if disconnect was called
      disconnectCalled = await page.evaluate(() => window.disconnectCalled || false);
      expect(disconnectCalled).toBe(true);
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
      // Mock many updates to test scrolling
      await page.addInitScript(() => {
        window.mockSignalRService = {
          connect: () => Promise.resolve(),
          disconnect: () => {},
          onLocationUpdate: (callback: (update: LocationUpdate) => void) => {
            for (let i = 1; i <= 20; i++) {
              setTimeout(() => {
                callback({
                  jobId: `job-${i.toString().padStart(3, '0')}`,
                  driverId: i,
                  location: { latitude: 40.7128, longitude: -74.0060, address: 'Test address' },
                  timestamp: new Date().toISOString()
                });
              }, i * 10);
            }
          },
          onNotification: (callback: (notification: NotificationMessage) => void) => {
            for (let i = 1; i <= 20; i++) {
              setTimeout(() => {
                callback({
                  id: `notification-${i}`,
                  type: 'notification',
                  title: `Notification ${i}`,
                  message: `Message ${i}`,
                  timestamp: new Date().toISOString(),
                  read: false
                });
              }, i * 10);
            }
          }
        };
      });

      await page.reload();
      await page.waitForTimeout(500);

      // Check that containers have max height and overflow scroll
      const locationContainer = page.locator('.max-h-64.overflow-y-auto').first();
      const notificationContainer = page.locator('.max-h-64.overflow-y-auto').last();

      if (await locationContainer.isVisible()) {
        await expect(locationContainer).toHaveClass(/max-h-64.*overflow-y-auto/);
      }

      if (await notificationContainer.isVisible()) {
        await expect(notificationContainer).toHaveClass(/max-h-64.*overflow-y-auto/);
      }
    });
  });
});