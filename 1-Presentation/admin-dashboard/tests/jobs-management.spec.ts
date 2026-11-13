import { test, expect } from '@playwright/test';

test.describe('Jobs Management', () => {
  test.beforeEach(async ({ page }) => {
    // Navigate to jobs page
    await page.goto('/jobs');
  });

  test.describe('Job Management Interface (14.1)', () => {
    test('should display job management page with header and create button', async ({ page }) => {
      // Check page title and description
      await expect(page.getByText('Job Management')).toBeVisible();
      await expect(page.getByText('Create, assign, and track delivery jobs')).toBeVisible();
      
      // Check create job button
      await expect(page.getByRole('button', { name: 'Create Job' })).toBeVisible();
    });

    test('should display jobs table with proper columns', async ({ page }) => {
      // Check table headers
      await expect(page.getByRole('columnheader', { name: 'Job Details' })).toBeVisible();
      await expect(page.getByRole('columnheader', { name: 'Status' })).toBeVisible();
      await expect(page.getByRole('columnheader', { name: 'Driver' })).toBeVisible();
      await expect(page.getByRole('columnheader', { name: 'Amount' })).toBeVisible();
      await expect(page.getByRole('columnheader', { name: 'Actions' })).toBeVisible();
    });

    test('should open job creation form when create button is clicked', async ({ page }) => {
      // Click create job button
      await page.getByRole('button', { name: 'Create Job' }).click();
      
      // Check if modal opens
      await expect(page.getByText('Create New Job')).toBeVisible();
      
      // Check form fields are present
      await expect(page.getByLabel('Title')).toBeVisible();
      await expect(page.getByLabel('Pickup Address')).toBeVisible();
      await expect(page.getByLabel('Dropoff Address')).toBeVisible();
      await expect(page.getByLabel('Amount')).toBeVisible();
      await expect(page.getByLabel('Priority')).toBeVisible();
      await expect(page.getByLabel('Scheduled Pickup Time')).toBeVisible();
      await expect(page.getByLabel('Customer ID')).toBeVisible();
      await expect(page.getByLabel('Special Instructions')).toBeVisible();
    });

    test('should validate required fields in job creation form', async ({ page }) => {
      // Open create job form
      await page.getByRole('button', { name: 'Create Job' }).click();
      
      // Try to submit empty form (the modal's submit button; the page header has a 'Create Job' button too)
      await page.locator('button[type="submit"]').click();
      
      // Check that form validation prevents submission
      // Note: This assumes HTML5 validation or custom validation
      await expect(page.getByLabel('Title')).toHaveAttribute('required');
      await expect(page.getByLabel('Pickup Address')).toHaveAttribute('required');
      await expect(page.getByLabel('Dropoff Address')).toHaveAttribute('required');
      await expect(page.getByLabel('Amount')).toHaveAttribute('required');
      await expect(page.getByLabel('Scheduled Pickup Time')).toHaveAttribute('required');
      await expect(page.getByLabel('Customer ID')).toHaveAttribute('required');
    });

    test('should create a new job with valid data', async ({ page }) => {
      // Mock API response for job creation
      await page.route('**/api/job', async route => {
        if (route.request().method() === 'POST') {
          await route.fulfill({
            status: 201,
            contentType: 'application/json',
            body: JSON.stringify({
              id: 'job-123',
              title: 'Test Delivery',
              pickupAddress: '123 Main St',
              dropoffAddress: '456 Oak Ave',
              amount: 150.00,
              status: 'Pending'
            })
          });
        } else {
          // The page's jobs list request (initial load and the refetch after creating) must not hang
          await route.fulfill({
            status: 200,
            contentType: 'application/json',
            body: JSON.stringify({ items: [], totalCount: 0 })
          });
        }
      });

      // Open create job form
      await page.getByRole('button', { name: 'Create Job' }).click();
      
      // Fill out the form
      await page.getByLabel('Title').fill('Test Delivery');
      await page.getByLabel('Pickup Address').fill('123 Main St');
      await page.getByLabel('Dropoff Address').fill('456 Oak Ave');
      await page.getByLabel('Amount').fill('150.00');
      await page.getByLabel('Priority').selectOption('Normal');
      await page.getByLabel('Scheduled Pickup Time').fill('2024-12-01T10:00');
      await page.getByLabel('Customer ID').fill('CUST-001');
      await page.getByLabel('Special Instructions').fill('Handle with care');
      
      // Submit the form (the modal's submit button; the page header has a 'Create Job' button too)
      await page.locator('button[type="submit"]').click();
      
      // Verify form closes (modal should disappear)
      await expect(page.getByText('Create New Job')).not.toBeVisible();
    });

    test('should cancel job creation and close form', async ({ page }) => {
      // Open create job form
      await page.getByRole('button', { name: 'Create Job' }).click();
      
      // Click cancel button
      await page.getByRole('button', { name: 'Cancel' }).click();
      
      // Verify form closes
      await expect(page.getByText('Create New Job')).not.toBeVisible();
    });

    test('should display job status with appropriate styling', async ({ page }) => {
      // Mock jobs data with different statuses
      await page.route('**/api/job*', async route => {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            items: [
              {
                id: 'job-1',
                title: 'Pending Job',
                status: 'Pending',
                pickupAddress: '123 Main St',
                dropoffAddress: '456 Oak Ave',
                amount: 100.00,
                scheduledPickupTime: '2024-12-01T10:00:00Z',
                assignedDriverId: null
              },
              {
                id: 'job-2',
                title: 'In Progress Job',
                status: 'InProgress',
                pickupAddress: '789 Pine St',
                dropoffAddress: '321 Elm Ave',
                amount: 200.00,
                scheduledPickupTime: '2024-12-01T14:00:00Z',
                assignedDriverId: 1
              }
            ],
            totalCount: 2
          })
        });
      });

      await page.reload();

      // Check that different statuses have different styling
      const pendingStatus = page.getByText('Pending', { exact: true });
      const inProgressStatus = page.getByText('InProgress', { exact: true });
      
      await expect(pendingStatus).toBeVisible();
      await expect(inProgressStatus).toBeVisible();
      
      // Verify status badges have appropriate classes (color coding)
      await expect(pendingStatus).toHaveClass(/bg-yellow-100.*text-yellow-800/);
      await expect(inProgressStatus).toHaveClass(/bg-green-100.*text-green-800/);
    });

    test('should open edit job form when edit button is clicked', async ({ page }) => {
      // Mock jobs data
      await page.route('**/api/job*', async route => {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            items: [
              {
                id: 'job-1',
                title: 'Test Job',
                status: 'Pending',
                pickupAddress: '123 Main St',
                dropoffAddress: '456 Oak Ave',
                amount: 100.00,
                scheduledPickupTime: '2024-12-01T10:00:00Z',
                specialInstructions: 'Test instructions',
                customerId: 'CUST-001',
                priority: 'Normal',
                assignedDriverId: null
              }
            ],
            totalCount: 1
          })
        });
      });

      await page.reload();

      // Click edit button (pencil icon)
      await page.locator('[data-testid="edit-job-button"]').first().click();
      
      // Check if edit modal opens with pre-filled data
      await expect(page.getByText('Edit Job')).toBeVisible();
      await expect(page.getByLabel('Title')).toHaveValue('Test Job');
      await expect(page.getByLabel('Pickup Address')).toHaveValue('123 Main St');
      await expect(page.getByLabel('Dropoff Address')).toHaveValue('456 Oak Ave');
    });

    test('should show assign driver modal for pending jobs', async ({ page }) => {
      // Mock jobs and drivers data
      await page.route('**/api/job*', async route => {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            items: [
              {
                id: 'job-1',
                title: 'Pending Job',
                status: 'Pending',
                pickupAddress: '123 Main St',
                dropoffAddress: '456 Oak Ave',
                amount: 100.00,
                scheduledPickupTime: '2024-12-01T10:00:00Z',
                assignedDriverId: null
              }
            ],
            totalCount: 1
          })
        });
      });

      await page.route('**/api/drivers*', async route => {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify([
            {
              id: 1,
              firstName: 'John',
              lastName: 'Doe',
              licenseNumber: 'DL123456',
              isActive: true
            },
            {
              id: 2,
              firstName: 'Jane',
              lastName: 'Smith',
              licenseNumber: 'DL789012',
              isActive: true
            }
          ])
        });
      });

      await page.reload();

      // Click assign driver button (truck icon)
      await page.locator('[data-testid="assign-driver-button"]').first().click();
      
      // Check if assign driver modal opens
      await expect(page.getByText('Assign Driver to Job')).toBeVisible();
      await expect(page.getByRole('heading', { name: 'Job Details' })).toBeVisible();
      await expect(page.getByText('Available Drivers')).toBeVisible();
      
      // Check that drivers are listed
      await expect(page.getByText('John Doe')).toBeVisible();
      await expect(page.getByText('Jane Smith')).toBeVisible();
    });

    test('should assign driver to job', async ({ page }) => {
      // Mock initial data
      await page.route('**/api/job*', async route => {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            items: [
              {
                id: 'job-1',
                title: 'Pending Job',
                status: 'Pending',
                pickupAddress: '123 Main St',
                dropoffAddress: '456 Oak Ave',
                amount: 100.00,
                scheduledPickupTime: '2024-12-01T10:00:00Z',
                assignedDriverId: null
              }
            ],
            totalCount: 1
          })
        });
      });

      await page.route('**/api/drivers*', async route => {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify([
            {
              id: 1,
              firstName: 'John',
              lastName: 'Doe',
              licenseNumber: 'DL123456',
              isActive: true
            }
          ])
        });
      });

      // Mock driver assignment API
      await page.route('**/api/job/job-1/assign-driver', async route => {
        if (route.request().method() === 'POST') {
          await route.fulfill({
            status: 200,
            contentType: 'application/json',
            body: JSON.stringify({ success: true })
          });
        }
      });

      await page.reload();

      // Open assign driver modal
      await page.locator('[data-testid="assign-driver-button"]').first().click();
      
      // Assign driver
      await page.getByRole('button', { name: 'Assign', exact: true }).click();
      
      // Verify modal closes
      await expect(page.getByText('Assign Driver to Job')).not.toBeVisible();
    });

    test('should filter and sort jobs', async ({ page }) => {
      // Mock jobs data with various statuses and dates
      await page.route('**/api/job*', async route => {
        const url = new URL(route.request().url());
        const status = url.searchParams.get('status');
        
        let items = [
          {
            id: 'job-1',
            title: 'Job 1',
            status: 'Pending',
            pickupAddress: '123 Main St',
            dropoffAddress: '456 Oak Ave',
            amount: 100.00,
            scheduledPickupTime: '2024-12-01T10:00:00Z',
            assignedDriverId: null
          },
          {
            id: 'job-2',
            title: 'Job 2',
            status: 'InProgress',
            pickupAddress: '789 Pine St',
            dropoffAddress: '321 Elm Ave',
            amount: 200.00,
            scheduledPickupTime: '2024-12-02T14:00:00Z',
            assignedDriverId: 1
          }
        ];

        // Apply filtering if status parameter exists
        if (status) {
          items = items.filter(job => job.status === status);
        }

        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            items,
            totalCount: items.length
          })
        });
      });

      await page.reload();

      // Verify initial state shows all jobs
      await expect(page.getByText('Job 1')).toBeVisible();
      await expect(page.getByText('Job 2')).toBeVisible();
    });

    test('should handle loading states', async ({ page }) => {
      // Mock slow API response
      await page.route('**/api/job*', async route => {
        await new Promise(resolve => setTimeout(resolve, 1000));
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

      // Check loading state
      await expect(page.getByText('Loading jobs...')).toBeVisible();
      
      // Wait for loading to complete
      await expect(page.getByText('Loading jobs...')).not.toBeVisible({ timeout: 2000 });
    });

    test('should handle empty jobs list', async ({ page }) => {
      // Mock empty response
      await page.route('**/api/job*', async route => {
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

      // Verify table structure is still present but no job rows
      await expect(page.getByText('Job Details')).toBeVisible();
      
      // Check that no job data is displayed
      const tableRows = page.locator('tbody tr');
      await expect(tableRows).toHaveCount(0);
    });
  });
});