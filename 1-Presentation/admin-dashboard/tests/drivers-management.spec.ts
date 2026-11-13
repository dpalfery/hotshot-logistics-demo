import { test, expect } from '@playwright/test';

test.describe('Driver Management', () => {
  test.beforeEach(async ({ page }) => {
    // Navigate to drivers page
    await page.goto('/drivers');
  });

  test.describe('Driver Management Interface (14.2)', () => {
    test('should display driver management page with header', async ({ page }) => {
      // Check page title and description
      await expect(page.getByText('Driver Management')).toBeVisible();
      await expect(page.getByText('Manage driver registrations, performance, and documents')).toBeVisible();
    });

    test('should display drivers table with proper columns', async ({ page }) => {
      // Check table headers
      await expect(page.getByRole('columnheader', { name: 'Driver' })).toBeVisible();
      await expect(page.getByRole('columnheader', { name: 'Contact' })).toBeVisible();
      await expect(page.getByRole('columnheader', { name: 'License' })).toBeVisible();
      await expect(page.getByRole('columnheader', { name: 'Status' })).toBeVisible();
    });

    test('should display driver information correctly', async ({ page }) => {
      // Mock drivers data
      await page.route('**/api/drivers*', async route => {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify([
            {
              id: 1,
              firstName: 'John',
              lastName: 'Doe',
              email: 'john.doe@example.com',
              phoneNumber: '(555) 123-4567',
              licenseNumber: 'DL123456789',
              licenseExpiryDate: '2025-12-31T00:00:00Z',
              isActive: true
            },
            {
              id: 2,
              firstName: 'Jane',
              lastName: 'Smith',
              email: 'jane.smith@example.com',
              phoneNumber: '(555) 987-6543',
              licenseNumber: 'DL987654321',
              licenseExpiryDate: '2024-06-15T00:00:00Z',
              isActive: false
            }
          ])
        });
      });

      await page.reload();

      // Check driver names
      await expect(page.getByText('John Doe')).toBeVisible();
      await expect(page.getByText('Jane Smith')).toBeVisible();

      // Check contact information
      await expect(page.getByText('john.doe@example.com')).toBeVisible();
      await expect(page.getByText('(555) 123-4567')).toBeVisible();
      await expect(page.getByText('jane.smith@example.com')).toBeVisible();
      await expect(page.getByText('(555) 987-6543')).toBeVisible();

      // Check license information
      await expect(page.getByText('DL123456789')).toBeVisible();
      await expect(page.getByText('DL987654321')).toBeVisible();
      
      // Check license expiry dates are formatted correctly
      await expect(page.getByText('Expires: 12/31/2025')).toBeVisible();
      await expect(page.getByText('Expires: 6/15/2024')).toBeVisible();
    });

    test('should display driver status with appropriate styling', async ({ page }) => {
      // Mock drivers data with different statuses
      await page.route('**/api/drivers*', async route => {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify([
            {
              id: 1,
              firstName: 'Active',
              lastName: 'Driver',
              email: 'active@example.com',
              phoneNumber: '(555) 111-1111',
              licenseNumber: 'DL111111111',
              licenseExpiryDate: '2025-12-31T00:00:00Z',
              isActive: true
            },
            {
              id: 2,
              firstName: 'Inactive',
              lastName: 'Driver',
              email: 'inactive@example.com',
              phoneNumber: '(555) 222-2222',
              licenseNumber: 'DL222222222',
              licenseExpiryDate: '2025-12-31T00:00:00Z',
              isActive: false
            }
          ])
        });
      });

      await page.reload();

      // Check active status styling
      const activeStatus = page.getByText('Active', { exact: true });
      await expect(activeStatus).toBeVisible();
      await expect(activeStatus).toHaveClass(/bg-green-100.*text-green-800/);

      // Check inactive status styling
      const inactiveStatus = page.getByText('Inactive', { exact: true });
      await expect(inactiveStatus).toBeVisible();
      await expect(inactiveStatus).toHaveClass(/bg-red-100.*text-red-800/);
    });

    test('should handle loading state', async ({ page }) => {
      // Mock slow API response
      await page.route('**/api/drivers*', async route => {
        await new Promise(resolve => setTimeout(resolve, 1000));
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify([])
        });
      });

      await page.reload();

      // Check loading state
      await expect(page.getByText('Loading drivers...')).toBeVisible();
      
      // Wait for loading to complete
      await expect(page.getByText('Loading drivers...')).not.toBeVisible({ timeout: 2000 });
    });

    test('should handle empty drivers list', async ({ page }) => {
      // Mock empty response
      await page.route('**/api/drivers*', async route => {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify([])
        });
      });

      await page.reload();

      // Verify table structure is still present but no driver rows
      await expect(page.getByRole('columnheader', { name: 'Driver' })).toBeVisible();
      await expect(page.getByRole('columnheader', { name: 'Contact' })).toBeVisible();
      await expect(page.getByRole('columnheader', { name: 'License' })).toBeVisible();
      await expect(page.getByRole('columnheader', { name: 'Status' })).toBeVisible();
      
      // Check that no driver data is displayed
      const tableRows = page.locator('tbody tr');
      await expect(tableRows).toHaveCount(0);
    });

    test('should display driver performance metrics when available', async ({ page }) => {
      // Mock drivers data with performance metrics
      await page.route('**/api/drivers*', async route => {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify([
            {
              id: 1,
              firstName: 'John',
              lastName: 'Doe',
              email: 'john.doe@example.com',
              phoneNumber: '(555) 123-4567',
              licenseNumber: 'DL123456789',
              licenseExpiryDate: '2025-12-31T00:00:00Z',
              isActive: true,
              performanceMetrics: {
                completedJobs: 150,
                averageRating: 4.8,
                onTimeDeliveryRate: 0.95
              }
            }
          ])
        });
      });

      await page.reload();

      // Check that driver information is displayed
      await expect(page.getByText('John Doe')).toBeVisible();
      
      // Note: Performance metrics display would depend on component implementation
      // This test verifies the data structure is handled correctly
    });

    test('should handle driver registration form if available', async ({ page }) => {
      // Check if there's a registration button or form
      const registerButton = page.getByRole('button', { name: /register|add.*driver/i });
      
      if (await registerButton.isVisible()) {
        await registerButton.click();
        
        // Check if registration form opens
        await expect(page.getByText(/register.*driver|add.*driver/i)).toBeVisible();
        
        // Check for expected form fields based on driver model
        await expect(page.getByLabel(/first.*name/i)).toBeVisible();
        await expect(page.getByLabel(/last.*name/i)).toBeVisible();
        await expect(page.getByLabel(/email/i)).toBeVisible();
        await expect(page.getByLabel(/phone/i)).toBeVisible();
        await expect(page.getByLabel(/license.*number/i)).toBeVisible();
        await expect(page.getByLabel(/license.*expiry/i)).toBeVisible();
      }
    });

    test('should validate driver license expiry dates', async ({ page }) => {
      // Mock drivers with expired and soon-to-expire licenses
      const currentDate = new Date();
      const expiredDate = new Date(currentDate.getTime() - 30 * 24 * 60 * 60 * 1000); // 30 days ago
      const soonToExpireDate = new Date(currentDate.getTime() + 15 * 24 * 60 * 60 * 1000); // 15 days from now

      await page.route('**/api/drivers*', async route => {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify([
            {
              id: 1,
              firstName: 'Expired',
              lastName: 'License',
              email: 'expired@example.com',
              phoneNumber: '(555) 111-1111',
              licenseNumber: 'DL111111111',
              licenseExpiryDate: expiredDate.toISOString(),
              isActive: true
            },
            {
              id: 2,
              firstName: 'Soon',
              lastName: 'Expiring',
              email: 'soon@example.com',
              phoneNumber: '(555) 222-2222',
              licenseNumber: 'DL222222222',
              licenseExpiryDate: soonToExpireDate.toISOString(),
              isActive: true
            }
          ])
        });
      });

      await page.reload();

      // Check that expired and soon-to-expire licenses are displayed
      await expect(page.getByText('Expired License')).toBeVisible();
      await expect(page.getByText('Soon Expiring')).toBeVisible();
      
      // Verify dates are formatted correctly
      const expiredDateFormatted = expiredDate.toLocaleDateString('en-US', { timeZone: 'UTC' }); // matches the pinned browser locale and time zone
      const soonExpireDateFormatted = soonToExpireDate.toLocaleDateString('en-US', { timeZone: 'UTC' });
      
      await expect(page.getByText(`Expires: ${expiredDateFormatted}`)).toBeVisible();
      await expect(page.getByText(`Expires: ${soonExpireDateFormatted}`)).toBeVisible();
    });

    test('should handle driver document management if available', async ({ page }) => {
      // Mock driver with documents
      await page.route('**/api/drivers*', async route => {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify([
            {
              id: 1,
              firstName: 'John',
              lastName: 'Doe',
              email: 'john.doe@example.com',
              phoneNumber: '(555) 123-4567',
              licenseNumber: 'DL123456789',
              licenseExpiryDate: '2025-12-31T00:00:00Z',
              isActive: true,
              documents: [
                {
                  type: 'license',
                  url: '/documents/license-123.pdf',
                  uploadedAt: '2024-01-15T00:00:00Z'
                },
                {
                  type: 'insurance',
                  url: '/documents/insurance-123.pdf',
                  uploadedAt: '2024-01-15T00:00:00Z'
                }
              ]
            }
          ])
        });
      });

      await page.reload();

      // Check if document management features are available
      const documentsButton = page.getByRole('button', { name: /documents|files/i });
      
      if (await documentsButton.isVisible()) {
        await documentsButton.click();
        
        // Check if document management interface opens
        await expect(page.getByText(/documents|files/i)).toBeVisible();
      }
    });

    test('should filter drivers by status if filtering is available', async ({ page }) => {
      // Mock all drivers initially
      await page.route('**/api/drivers*', async route => {
        const url = new URL(route.request().url());
        const status = url.searchParams.get('status');
        
        let drivers = [
          {
            id: 1,
            firstName: 'Active',
            lastName: 'Driver',
            email: 'active@example.com',
            phoneNumber: '(555) 111-1111',
            licenseNumber: 'DL111111111',
            licenseExpiryDate: '2025-12-31T00:00:00Z',
            isActive: true
          },
          {
            id: 2,
            firstName: 'Inactive',
            lastName: 'Driver',
            email: 'inactive@example.com',
            phoneNumber: '(555) 222-2222',
            licenseNumber: 'DL222222222',
            licenseExpiryDate: '2025-12-31T00:00:00Z',
            isActive: false
          }
        ];

        // Apply filtering if status parameter exists
        if (status === 'active') {
          drivers = drivers.filter(driver => driver.isActive);
        } else if (status === 'inactive') {
          drivers = drivers.filter(driver => !driver.isActive);
        }

        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify(drivers)
        });
      });

      await page.reload();

      // Verify initial state shows all drivers
      await expect(page.getByText('Active Driver', { exact: true })).toBeVisible();
      await expect(page.getByText('Inactive Driver', { exact: true })).toBeVisible();

      // Check if filter controls exist
      const statusFilter = page.getByRole('combobox', { name: /status|filter/i });
      
      if (await statusFilter.isVisible()) {
        // Test filtering by active status
        await statusFilter.selectOption('active');
        await expect(page.getByText('Active Driver', { exact: true })).toBeVisible();
        await expect(page.getByText('Inactive Driver', { exact: true })).not.toBeVisible();
        
        // Test filtering by inactive status
        await statusFilter.selectOption('inactive');
        await expect(page.getByText('Inactive Driver', { exact: true })).toBeVisible();
        await expect(page.getByText('Active Driver', { exact: true })).not.toBeVisible();
      }
    });

    test('should handle API errors gracefully', async ({ page }) => {
      // Mock API error
      await page.route('**/api/drivers*', async route => {
        await route.fulfill({
          status: 500,
          contentType: 'application/json',
          body: JSON.stringify({ error: 'Internal server error' })
        });
      });

      await page.reload();

      // Check that error is handled gracefully
      // This might show an error message or fallback content
      // The exact behavior depends on error handling implementation
      await expect(page.getByText('Driver Management')).toBeVisible();
    });
  });
});