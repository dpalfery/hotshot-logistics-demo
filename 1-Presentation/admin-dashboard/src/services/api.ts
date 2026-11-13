import { msalInstance } from '@/lib/providers';
import { loginRequest } from '@/config/auth';
import { Job, Driver, Invoice, Customer, PagedResult, JobFilter, InvoiceFilter, PaginationParameters, InvoiceSummaryMetrics, InvoiceAgingBuckets } from '@/types';

const API_BASE_URL = (process.env.NEXT_PUBLIC_API_BASE_URL || '/api').trim();

class ApiService {
  private async request<T>(
    endpoint: string,
    options: RequestInit = {},
    query?: string
  ): Promise<T> {
    const baseUrl = API_BASE_URL.replace(/\/$/, '');
    const normalizedEndpoint = endpoint.startsWith('/') ? endpoint : `/${endpoint}`;
    const url = `${baseUrl}${normalizedEndpoint}${query ? `?${query}` : ''}`;

    const isTestMode = typeof window !== 'undefined' && (window as typeof window & { __BYPASS_AUTH__?: boolean }).__BYPASS_AUTH__ === true;
    // Test mode uses the configured base URL like every other mode, so Playwright mocks match /api/...
    const finalUrl = url;

    console.log('=== API REQUEST DEBUG ===');
    console.log('Making request to:', finalUrl);
    console.log('Base URL from env:', API_BASE_URL);
    console.log('Is test mode:', isTestMode);

    const config: RequestInit = {
      headers: {
        'Content-Type': 'application/json',
        ...options.headers,
      },
      ...options,
    };

    // Add authentication token if available
    const token = await this.getAuthToken();
    console.log('Auth token obtained:', token ? 'Yes' : 'No');
    if (token) {
      // Use Test scheme for test token, Bearer for real tokens
      const scheme = token === 'test-token' ? 'Test' : 'Bearer';
      console.log('Using auth scheme:', scheme);
      config.headers = {
        ...config.headers,
        Authorization: `${scheme} ${token}`,
      };
    }

    try {
      const response = await fetch(finalUrl, config);
      console.log('Response status:', response.status);
      console.log('Response ok:', response.ok);

      if (!response.ok) {
        const error = await response.text();
        console.error('API Error response:', error);
        throw new Error(`API Error: ${response.status} ${error}`);
      }

      const data = await response.json();
      console.log('Response data length/type:', Array.isArray(data) ? data.length : typeof data);
      console.log('=== END API REQUEST DEBUG ===');
      return data;
    } catch (error) {
      console.error('Fetch error:', error);
      console.log('=== END API REQUEST DEBUG (ERROR) ===');
      throw error;
    }
  }

  private async getAuthToken(): Promise<string | null> {
    // Always use test authentication in development
    if (process.env.NODE_ENV === 'development') {
      console.log('Development mode: using test authentication');
      return 'test-token';
    }

    // Check for test mode bypass
    if (typeof window !== 'undefined' && (window as typeof window & { __BYPASS_AUTH__?: boolean }).__BYPASS_AUTH__ === true) {
      return 'test-token';
    }

    const account = msalInstance.getActiveAccount();
    if (!account) {
      return null;
    }

    try {
      const response = await msalInstance.acquireTokenSilent({
        ...loginRequest,
        account,
      });
      return response.accessToken;
    } catch (error) {
      console.error('Silent token acquisition failed:', error);
      // Fallback to interactive method if silent acquisition fails
      try {
        const response = await msalInstance.acquireTokenPopup({
          ...loginRequest,
          account,
        });
        return response.accessToken;
      } catch (popupError) {
        console.error('Popup token acquisition failed:', popupError);
        return null;
      }
    }
  }

  // Job API methods
  async getJobs(
    filter?: JobFilter,
    pagination?: PaginationParameters
  ): Promise<PagedResult<Job>> {
    const params = new URLSearchParams();

    if (filter) {
      Object.entries(filter).forEach(([key, value]) => {
        if (value !== undefined && value !== null) {
          params.append(key, value.toString());
        }
      });
    }

    if (pagination) {
      params.append('pageNumber', pagination.pageNumber.toString());
      params.append('pageSize', pagination.pageSize.toString());
    }

    const query = params.toString();
    if (query) {
      return this.request<PagedResult<Job>>('/job', {}, query);
    } else {
      return this.request<PagedResult<Job>>('/job');
    }
  }

  async getJobById(id: string): Promise<Job> {
    return this.request<Job>(`/job/${id}`);
  }

  async createJob(job: Partial<Job>): Promise<Job> {
    return this.request<Job>('/job', {
      method: 'POST',
      body: JSON.stringify(job),
    });
  }

  async updateJob(id: string, job: Partial<Job>): Promise<Job> {
    return this.request<Job>(`/job/${id}`, {
      method: 'PUT',
      body: JSON.stringify(job),
    });
  }

  async deleteJob(id: string): Promise<void> {
    await this.request(`/job/${id}`, {
      method: 'DELETE',
    });
  }

  async assignDriver(jobId: string, driverId: number): Promise<Job> {
    return this.request<Job>(`/job/${jobId}/assign-driver`, {
      method: 'POST',
      body: JSON.stringify({ driverId }),
    });
  }

  async updateJobStatus(jobId: string, status: string): Promise<Job> {
    return this.request<Job>(`/job/${jobId}/status`, {
      method: 'PUT',
      body: JSON.stringify({ status }),
    });
  }

  // Driver API methods
  async getDrivers(): Promise<Driver[]> {
    return this.request<Driver[]>('/drivers');
  }

  async getDriverById(id: number): Promise<Driver> {
    return this.request<Driver>(`/drivers/${id}`);
  }

  async createDriver(driver: Partial<Driver>): Promise<Driver> {
    return this.request<Driver>('/drivers', {
      method: 'POST',
      body: JSON.stringify(driver),
    });
  }

  async updateDriver(id: number, driver: Partial<Driver>): Promise<Driver> {
    return this.request<Driver>(`/drivers/${id}`, {
      method: 'PUT',
      body: JSON.stringify(driver),
    });
  }

  async deleteDriver(id: number): Promise<void> {
    await this.request(`/drivers/${id}`, {
      method: 'DELETE',
    });
  }

  // Invoice API methods - Get overdue invoices for dashboard
  async getInvoices(
    _filter?: InvoiceFilter,
    _pagination?: PaginationParameters
  ): Promise<PagedResult<Invoice>> {
    const response: unknown = await this.request<unknown>('/billing/invoices/overdue');
    const responseRecord =
      typeof response === 'object' && response !== null
        ? (response as { items?: unknown; totalCount?: unknown })
        : undefined;
    const items = Array.isArray(response)
      ? (response as Invoice[])
      : Array.isArray(responseRecord?.items)
        ? (responseRecord?.items as Invoice[])
        : [];
    const totalCount = Array.isArray(response)
      ? response.length
      : typeof responseRecord?.totalCount === 'number'
        ? responseRecord.totalCount
        : items.length;

    return {
      items,
      totalCount,
      pageNumber: 1,
      pageSize: items.length || 10,
      totalPages: 1
    };
  }

  async getInvoiceById(id: string): Promise<Invoice> {
    return this.request<Invoice>(`/billing/invoices/${id}`);
  }

  async createInvoice(invoice: Partial<Invoice>): Promise<Invoice> {
    return this.request<Invoice>('/billing/invoices', {
      method: 'POST',
      body: JSON.stringify(invoice),
    });
  }

  async updateInvoice(id: string, invoice: Partial<Invoice>): Promise<Invoice> {
    return this.request<Invoice>(`/billing/invoices/${id}`, {
      method: 'PUT',
      body: JSON.stringify(invoice),
    });
  }

  async deleteInvoice(id: string): Promise<void> {
    await this.request(`/billing/invoices/${id}`, {
      method: 'DELETE',
    });
  }

  async getInvoiceSummary(): Promise<InvoiceSummaryMetrics> {
    return this.request<InvoiceSummaryMetrics>('/invoices/summary');
  }

  async getInvoiceAging(): Promise<InvoiceAgingBuckets> {
    return this.request<InvoiceAgingBuckets>('/invoices/aging');
  }

  // Customer API methods
  async getCustomers(): Promise<Customer[]> {
    return this.request<Customer[]>('/customer');
  }

  async getCustomerById(id: string): Promise<Customer> {
    return this.request<Customer>(`/customer/${id}`);
  }

  async createCustomer(customer: Partial<Customer>): Promise<Customer> {
    return this.request<Customer>('/customer', {
      method: 'POST',
      body: JSON.stringify(customer),
    });
  }

  async updateCustomer(id: string, customer: Partial<Customer>): Promise<Customer> {
    return this.request<Customer>(`/customer/${id}`, {
      method: 'PUT',
      body: JSON.stringify(customer),
    });
  }

  async deleteCustomer(id: string): Promise<void> {
    await this.request(`/customer/${id}`, {
      method: 'DELETE',
    });
  }
}

export const apiService = new ApiService();