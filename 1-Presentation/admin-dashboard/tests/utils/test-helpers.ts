import { Page, expect } from '@playwright/test';

/**
 * Common test utilities and helper functions for Playwright tests
 */

// Mock data interfaces
export interface MockJob {
  id: string;
  title: string;
  status: 'Pending' | 'Assigned' | 'InProgress' | 'Completed' | 'Cancelled';
  pickupAddress: string;
  dropoffAddress: string;
  assignedDriverId?: number | null;
  scheduledPickupTime: string;
  priority?: 'Low' | 'Medium' | 'High' | 'Urgent';
  cargoDetails?: {
    weight: number;
    dimensions: string;
    description: string;
  };
  pricingDetails?: {
    baseRate: number;
    mileageRate: number;
    totalAmount: number;
  };
}

export interface MockDriver {
  id: number;
  name: string;
  email: string;
  phone: string;
  licenseNumber: string;
  licenseExpiryDate: string;
  status: 'Available' | 'Busy' | 'Offline';
  vehicleInfo: {
    make: string;
    model: string;
    year: number;
    licensePlate: string;
  };
  performanceMetrics?: {
    completedJobs: number;
    rating: number;
    onTimeDeliveries: number;
  };
}

export interface MockInvoice {
  id: string;
  jobId: string;
  customerId: number;
  amount: number;
  status: 'Draft' | 'Sent' | 'Paid' | 'Overdue';
  dueDate: string;
  createdAt: string;
  lineItems: Array<{
    description: string;
    quantity: number;
    unitPrice: number;
    total: number;
  }>;
}

export interface MockCustomer {
  id: number;
  name: string;
  email: string;
  phone: string;
  address: string;
  creditTerms: number;
  status: 'Active' | 'Inactive';
}

/**
 * API Mocking Helpers
 */
export class ApiMocker {
  constructor(private page: Page) {}

  async mockJobsApi(jobs: MockJob[] = [], totalCount?: number) {
    await this.page.route('**/api/jobs*', async route => {
      const url = new URL(route.request().url());
      const status = url.searchParams.get('status');
      const search = url.searchParams.get('search');
      
      let filteredJobs = jobs;
      
      if (status && status !== 'all') {
        filteredJobs = jobs.filter(job => job.status === status);
      }
      
      if (search) {
        filteredJobs = jobs.filter(job => 
          job.title.toLowerCase().includes(search.toLowerCase()) ||
          job.pickupAddress.toLowerCase().includes(search.toLowerCase()) ||
          job.dropoffAddress.toLowerCase().includes(search.toLowerCase())
        );
      }

      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          items: filteredJobs,
          totalCount: totalCount || filteredJobs.length
        })
      });
    });
  }

  async mockDriversApi(drivers: MockDriver[] = [], totalCount?: number) {
    await this.page.route('**/api/drivers*', async route => {
      const url = new URL(route.request().url());
      const status = url.searchParams.get('status');
      const search = url.searchParams.get('search');
      
      let filteredDrivers = drivers;
      
      if (status && status !== 'all') {
        filteredDrivers = drivers.filter(driver => driver.status === status);
      }
      
      if (search) {
        filteredDrivers = drivers.filter(driver => 
          driver.name.toLowerCase().includes(search.toLowerCase()) ||
          driver.email.toLowerCase().includes(search.toLowerCase()) ||
          driver.licenseNumber.toLowerCase().includes(search.toLowerCase())
        );
      }

      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          items: filteredDrivers,
          totalCount: totalCount || filteredDrivers.length
        })
      });
    });
  }

  async mockInvoicesApi(invoices: MockInvoice[] = [], totalCount?: number) {
    await this.page.route('**/api/billing/invoices**', async route => {
      const url = new URL(route.request().url());
      const status = url.searchParams.get('status');
      const search = url.searchParams.get('search');

      let filteredInvoices = invoices;

      if (status && status !== 'all') {
        filteredInvoices = invoices.filter(invoice => invoice.status === status);
      }

      if (search) {
        filteredInvoices = invoices.filter(invoice =>
          invoice.id.toLowerCase().includes(search.toLowerCase()) ||
          invoice.jobId.toLowerCase().includes(search.toLowerCase())
        );
      }

      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          items: filteredInvoices,
          totalCount: totalCount || filteredInvoices.length
        })
      });
    });
  }

  async mockCustomersApi(customers: MockCustomer[] = []) {
    await this.page.route('**/api/customers*', async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          items: customers,
          totalCount: customers.length
        })
      });
    });
  }

  async mockDashboardStatsApi(stats: Record<string, unknown> = {}) {
    const defaultStats = {
      totalJobs: 0,
      activeDrivers: 0,
      revenue: 0,
      pendingJobs: 0,
      ...stats
    };

    await this.page.route('**/api/dashboard/stats', async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(defaultStats)
      });
    });
  }

  async mockInvoiceSummaryApi(summary: Record<string, unknown> = {}) {
    const defaultSummary = {
      totalInvoiced: 0,
      totalPaid: 0,
      totalOutstanding: 0,
      overdueAmount: 0,
      ...summary
    };

    await this.page.route('**/invoices/summary*', async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(defaultSummary)
      });
    });
  }

  async mockInvoiceAgingApi(aging: Record<string, unknown> = {}) {
    const defaultAging = {
      current: 0,
      days30: 0,
      days60: 0,
      days90: 0,
      over90: 0,
      total: 0,
      ...aging
    };

    await this.page.route('**/invoices/aging*', async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(defaultAging)
      });
    });
  }

  async mockApiError(endpoint: string, status: number = 500, message: string = 'Internal server error') {
    await this.page.route(`**${endpoint}*`, async route => {
      await route.fulfill({
        status,
        contentType: 'application/json',
        body: JSON.stringify({ error: message })
      });
    });
  }

  async mockApiDelay(endpoint: string, delay: number = 1000, response: Record<string, unknown> = {}) {
    await this.page.route(`**${endpoint}*`, async route => {
      await new Promise(resolve => setTimeout(resolve, delay));
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(response)
      });
    });
  }
}

/**
 * Form Interaction Helpers
 */
export class FormHelpers {
  constructor(private page: Page) {}

  async fillJobForm(jobData: Partial<MockJob>) {
    if (jobData.title) {
      await this.page.fill('input[name="title"]', jobData.title);
    }
    
    if (jobData.pickupAddress) {
      await this.page.fill('input[name="pickupAddress"]', jobData.pickupAddress);
    }
    
    if (jobData.dropoffAddress) {
      await this.page.fill('input[name="dropoffAddress"]', jobData.dropoffAddress);
    }
    
    if (jobData.scheduledPickupTime) {
      await this.page.fill('input[name="scheduledPickupTime"]', jobData.scheduledPickupTime);
    }
    
    if (jobData.priority) {
      await this.page.selectOption('select[name="priority"]', jobData.priority);
    }
    
    if (jobData.cargoDetails) {
      await this.page.fill('input[name="weight"]', jobData.cargoDetails.weight.toString());
      await this.page.fill('input[name="dimensions"]', jobData.cargoDetails.dimensions);
      await this.page.fill('textarea[name="description"]', jobData.cargoDetails.description);
    }
  }

  async fillDriverForm(driverData: Partial<MockDriver>) {
    if (driverData.name) {
      await this.page.fill('input[name="name"]', driverData.name);
    }
    
    if (driverData.email) {
      await this.page.fill('input[name="email"]', driverData.email);
    }
    
    if (driverData.phone) {
      await this.page.fill('input[name="phone"]', driverData.phone);
    }
    
    if (driverData.licenseNumber) {
      await this.page.fill('input[name="licenseNumber"]', driverData.licenseNumber);
    }
    
    if (driverData.licenseExpiryDate) {
      await this.page.fill('input[name="licenseExpiryDate"]', driverData.licenseExpiryDate);
    }
    
    if (driverData.vehicleInfo) {
      await this.page.fill('input[name="vehicleMake"]', driverData.vehicleInfo.make);
      await this.page.fill('input[name="vehicleModel"]', driverData.vehicleInfo.model);
      await this.page.fill('input[name="vehicleYear"]', driverData.vehicleInfo.year.toString());
      await this.page.fill('input[name="licensePlate"]', driverData.vehicleInfo.licensePlate);
    }
  }

  async submitForm(buttonText: string = 'Save') {
    await this.page.click(`button:has-text("${buttonText}")`);
  }

  async cancelForm(buttonText: string = 'Cancel') {
    await this.page.click(`button:has-text("${buttonText}")`);
  }
}

/**
 * Navigation Helpers
 */
export class NavigationHelpers {
  constructor(private page: Page) {}

  async navigateToJobs() {
    await this.page.click('nav a[href="/jobs"]');
    await expect(this.page).toHaveURL('/jobs');
  }

  async navigateToDrivers() {
    await this.page.click('nav a[href="/drivers"]');
    await expect(this.page).toHaveURL('/drivers');
  }

  async navigateToBilling() {
    await this.page.click('nav a[href="/billing"]');
    await expect(this.page).toHaveURL('/billing');
  }

  async navigateToTracking() {
    await this.page.click('nav a[href="/tracking"]');
    await expect(this.page).toHaveURL('/tracking');
  }

  async navigateToDashboard() {
    await this.page.click('nav a[href="/"]');
    await expect(this.page).toHaveURL('/');
  }
}

/**
 * Table Interaction Helpers
 */
export class TableHelpers {
  constructor(private page: Page) {}

  async searchTable(searchTerm: string) {
    await this.page.fill('input[placeholder*="Search"]', searchTerm);
    await this.page.keyboard.press('Enter');
  }

  async filterByStatus(status: string) {
    await this.page.selectOption('select[name="status"]', status);
  }

  async sortByColumn(columnName: string) {
    await this.page.click(`th:has-text("${columnName}")`);
  }

  async clickRowAction(rowIndex: number, actionText: string) {
    const row = this.page.locator('tbody tr').nth(rowIndex);
    await row.locator(`button:has-text("${actionText}")`).click();
  }

  async getRowCount() {
    return await this.page.locator('tbody tr').count();
  }

  async getRowData(rowIndex: number) {
    const row = this.page.locator('tbody tr').nth(rowIndex);
    const cells = await row.locator('td').allTextContents();
    return cells;
  }
}

/**
 * Modal Interaction Helpers
 */
export class ModalHelpers {
  constructor(private page: Page) {}

  async waitForModal(title?: string) {
    await this.page.waitForSelector('[role="dialog"]');
    if (title) {
      await expect(this.page.getByText(title)).toBeVisible();
    }
  }

  async closeModal() {
    // Try clicking the X button first
    const closeButton = this.page.locator('[role="dialog"] button[aria-label="Close"]');
    if (await closeButton.isVisible()) {
      await closeButton.click();
    } else {
      // Fallback to pressing Escape
      await this.page.keyboard.press('Escape');
    }
  }

  async clickModalButton(buttonText: string) {
    await this.page.locator('[role="dialog"]').locator(`button:has-text("${buttonText}")`).click();
  }
}

/**
 * Assertion Helpers
 */
export class AssertionHelpers {
  constructor(private page: Page) {}

  async expectToastMessage(message: string, type: 'success' | 'error' | 'info' = 'success') {
    const toast = this.page.locator(`[data-testid="toast-${type}"]`);
    await expect(toast).toBeVisible();
    await expect(toast).toContainText(message);
  }

  async expectLoadingState() {
    await expect(this.page.locator('[data-testid="loading-spinner"]')).toBeVisible();
  }

  async expectEmptyState(message?: string) {
    await expect(this.page.locator('[data-testid="empty-state"]')).toBeVisible();
    if (message) {
      await expect(this.page.getByText(message)).toBeVisible();
    }
  }

  async expectErrorState(message?: string) {
    await expect(this.page.locator('[data-testid="error-state"]')).toBeVisible();
    if (message) {
      await expect(this.page.getByText(message)).toBeVisible();
    }
  }

  async expectStatusBadge(status: string, expectedClass?: string) {
    const badge = this.page.locator(`text=${status}`).first();
    await expect(badge).toBeVisible();
    if (expectedClass) {
      await expect(badge).toHaveClass(new RegExp(expectedClass));
    }
  }
}

/**
 * Mock Data Generators
 */
export class MockDataGenerator {
  static generateJob(overrides: Partial<MockJob> = {}): MockJob {
    return {
      id: `job-${Math.random().toString(36).substr(2, 9)}`,
      title: 'Test Delivery',
      status: 'Pending',
      pickupAddress: '123 Main St, City, State',
      dropoffAddress: '456 Oak Ave, City, State',
      assignedDriverId: null,
      scheduledPickupTime: new Date().toISOString(),
      priority: 'Medium',
      cargoDetails: {
        weight: 100,
        dimensions: '10x10x10',
        description: 'Test cargo'
      },
      pricingDetails: {
        baseRate: 50,
        mileageRate: 2.5,
        totalAmount: 125
      },
      ...overrides
    };
  }

  static generateDriver(overrides: Partial<MockDriver> = {}): MockDriver {
    const id = Math.floor(Math.random() * 1000) + 1;
    return {
      id,
      name: `Driver ${id}`,
      email: `driver${id}@example.com`,
      phone: `555-${String(Math.floor(Math.random() * 10000)).padStart(4, '0')}`,
      licenseNumber: `DL${String(Math.floor(Math.random() * 100000)).padStart(5, '0')}`,
      licenseExpiryDate: new Date(Date.now() + 365 * 24 * 60 * 60 * 1000).toISOString().split('T')[0],
      status: 'Available',
      vehicleInfo: {
        make: 'Ford',
        model: 'Transit',
        year: 2020,
        licensePlate: `ABC${Math.floor(Math.random() * 1000)}`
      },
      performanceMetrics: {
        completedJobs: Math.floor(Math.random() * 100),
        rating: 4.5,
        onTimeDeliveries: Math.floor(Math.random() * 90) + 10
      },
      ...overrides
    };
  }

  static generateInvoice(overrides: Partial<MockInvoice> = {}): MockInvoice {
    const id = `INV-${String(Math.floor(Math.random() * 10000)).padStart(4, '0')}`;
    return {
      id,
      jobId: `job-${Math.random().toString(36).substr(2, 9)}`,
      customerId: Math.floor(Math.random() * 100) + 1,
      amount: Math.floor(Math.random() * 1000) + 100,
      status: 'Draft',
      dueDate: new Date(Date.now() + 30 * 24 * 60 * 60 * 1000).toISOString(),
      createdAt: new Date().toISOString(),
      lineItems: [
        {
          description: 'Delivery Service',
          quantity: 1,
          unitPrice: 100,
          total: 100
        }
      ],
      ...overrides
    };
  }

  static generateCustomer(overrides: Partial<MockCustomer> = {}): MockCustomer {
    const id = Math.floor(Math.random() * 1000) + 1;
    return {
      id,
      name: `Customer ${id}`,
      email: `customer${id}@example.com`,
      phone: `555-${String(Math.floor(Math.random() * 10000)).padStart(4, '0')}`,
      address: `${Math.floor(Math.random() * 9999) + 1} Main St, City, State`,
      creditTerms: 30,
      status: 'Active',
      ...overrides
    };
  }
}

/**
 * Test Helper Factory
 */
export class TestHelpers {
  public apiMocker: ApiMocker;
  public formHelpers: FormHelpers;
  public navigationHelpers: NavigationHelpers;
  public tableHelpers: TableHelpers;
  public modalHelpers: ModalHelpers;
  public assertionHelpers: AssertionHelpers;

  constructor(private page: Page) {
    this.apiMocker = new ApiMocker(page);
    this.formHelpers = new FormHelpers(page);
    this.navigationHelpers = new NavigationHelpers(page);
    this.tableHelpers = new TableHelpers(page);
    this.modalHelpers = new ModalHelpers(page);
    this.assertionHelpers = new AssertionHelpers(page);
  }

  async setupBasicMocks() {
    await this.apiMocker.mockJobsApi([]);
    await this.apiMocker.mockDriversApi([]);
    // NOTE: Do NOT register a default invoices mock here.
    // Registering a blanket invoices mock in setupBasicMocks causes the handler
    // to fulfill requests before any per-test invoice mocks are registered,
    // which makes tests that register their own invoice routes non-deterministic
    // across browsers. Tests should call `page.route(...)` or
    // `this.apiMocker.mockInvoicesApi(...)` explicitly when they need invoice data.
    await this.apiMocker.mockCustomersApi([]);
    await this.apiMocker.mockDashboardStatsApi();
    await this.apiMocker.mockInvoiceSummaryApi();
    await this.apiMocker.mockInvoiceAgingApi();
  }

  /**
   * Wait for the invoices API network response to complete.
   * Use this after calling `page.reload()` in tests that register their own
   * invoices route handlers, so assertions wait for the deterministic mocked response.
   *
   * @param timeout - how long to wait (ms), defaults to 5000
   */
  async waitForInvoicesResponse(expectedStatus: number | 'any' = 200, timeout: number = 5000) {
    await this.page.waitForResponse(response => {
      const url = response.url();
      // match both /api/billing/invoices and any query variations
      if (!url.includes('/api/billing/invoices')) return false;
      if (expectedStatus === 'any') return true;
      return response.status() === expectedStatus;
    }, { timeout });
  }

  /**
   * Reload the page and wait for the invoices network response to complete.
   * Uses Promise.all to avoid the race where the response happens before
   * waitForResponse starts listening.
   *
   * @param expectedStatus - expected HTTP status (number) or 'any' to accept any status
   * @param timeout - timeout in ms
   */
  async reloadAndWaitForInvoices(expectedStatus: number | 'any' = 200, timeout: number = 5000) {
    await Promise.all([
      this.page.waitForResponse(response => {
        const url = response.url();
        if (!url.includes('/api/billing/invoices')) return false;
        if (expectedStatus === 'any') return true;
        return response.status() === expectedStatus;
      }, { timeout }),
      this.page.reload()
    ]);
  }
}