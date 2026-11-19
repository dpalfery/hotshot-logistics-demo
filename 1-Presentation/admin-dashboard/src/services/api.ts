import { msalInstance } from '@/lib/providers';
import { loginRequest } from '@/config/auth';
import { Job, JobStatus, Driver, Invoice, Customer, PagedResult, JobFilter, InvoiceFilter, PaginationParameters, InvoiceSummaryMetrics, InvoiceAgingBuckets } from '@/types';

const API_BASE_URL = (process.env.NEXT_PUBLIC_API_BASE_URL || '/api').trim();

/**
 * Response-shape checks at the service boundary: a response that fails one is an error the
 * calling view shows, never data a component has to guess about.
 */
function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function unexpectedResponse(endpoint: string): Error {
  return new Error(`Unexpected response shape from ${endpoint}`);
}

// The API sends JobStatus as its number, in the order of the server enum
const JOB_STATUS_BY_NUMBER: readonly JobStatus[] = [
  JobStatus.Pending,
  JobStatus.Assigned,
  JobStatus.EnRoute,
  JobStatus.Received
];

/** Reads a status from its number, a numeric string, or its name. Anything else is not a status. */
function toJobStatus(value: unknown): JobStatus | undefined {
  if (typeof value === 'number' && Number.isInteger(value)) {
    return value >= 0 && value < JOB_STATUS_BY_NUMBER.length ? JOB_STATUS_BY_NUMBER[value] : undefined;
  }

  if (typeof value === 'string') {
    const trimmed = value.trim();
    if (/^\d+$/.test(trimmed)) {
      const index = Number(trimmed);
      return index >= 0 && index < JOB_STATUS_BY_NUMBER.length ? JOB_STATUS_BY_NUMBER[index] : undefined;
    }

    return (Object.values(JobStatus) as string[]).includes(trimmed) ? (trimmed as JobStatus) : undefined;
  }

  return undefined;
}

function parseJobsPage(data: unknown): PagedResult<Job> {
  if (!isRecord(data) || !Array.isArray(data.items) || typeof data.totalCount !== 'number') {
    throw unexpectedResponse('/job');
  }

  const items = data.items.flatMap(item => {
    if (!isRecord(item) || typeof item.id !== 'string') {
      throw unexpectedResponse('/job');
    }

    const status = toJobStatus(item.status);
    if (status === undefined) {
      return [];
    }

    // amount is rendered with toFixed; a missing value must be the error state, not a crash in the view.
    if (typeof item.amount !== 'number' || !Number.isFinite(item.amount)) {
      throw unexpectedResponse('/job');
    }

    return [{ ...item, status }];
  });

  if (data.items.length > 0 && items.length === 0) {
    throw unexpectedResponse('/job');
  }

  return { ...data, items } as unknown as PagedResult<Job>;
}

function parseDrivers(data: unknown): Driver[] {
  if (
    !Array.isArray(data) ||
    !data.every(item => isRecord(item) && typeof item.id === 'number' && typeof item.isActive === 'boolean')
  ) {
    throw unexpectedResponse('/drivers');
  }
  return data as Driver[];
}

function parseOverdueInvoices(data: unknown): PagedResult<Invoice> {
  const items = Array.isArray(data) ? data : isRecord(data) ? data.items : undefined;
  if (!Array.isArray(items) || !items.every(isRecord)) {
    throw unexpectedResponse('/billing/invoices/overdue');
  }
  const totalCount = isRecord(data) && typeof data.totalCount === 'number' ? data.totalCount : items.length;
  return {
    items: items as unknown as Invoice[],
    totalCount,
    pageNumber: 1,
    pageSize: items.length || 10,
    totalPages: 1
  };
}

class ApiService {
  private async request<T>(
    endpoint: string,
    options: RequestInit = {},
    query?: string
  ): Promise<T> {
    const baseUrl = API_BASE_URL.replace(/\/$/, '');
    const normalizedEndpoint = endpoint.startsWith('/') ? endpoint : `/${endpoint}`;
    const url = `${baseUrl}${normalizedEndpoint}${query ? `?${query}` : ''}`;

    const config: RequestInit = {
      headers: {
        'Content-Type': 'application/json',
        ...options.headers,
      },
      ...options,
    };

    // Add authentication token if available
    const token = await this.getAuthToken();
    if (token) {
      // Use Test scheme for test token, Bearer for real tokens
      const scheme = token === 'test-token' ? 'Test' : 'Bearer';
      config.headers = {
        ...config.headers,
        Authorization: `${scheme} ${token}`,
      };
    }

    const response = await fetch(url, config);
    if (!response.ok) {
      const error = await response.text();
      throw new Error(`API Error: ${response.status} ${error}`);
    }

    if (response.status === 204 || response.status === 205) {
      return undefined as T;
    }

    const body = await response.text();
    if (body.trim().length === 0) {
      return undefined as T;
    }

    return JSON.parse(body) as T;
  }

  /** The token the API accepts for the signed-in user, or null when nobody is signed in. */
  async getAuthToken(): Promise<string | null> {
    // Always use test authentication in development
    if (process.env.NODE_ENV === 'development') {
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
    return parseJobsPage(await this.request<unknown>('/job', {}, query || undefined));
  }

  /**
   * Counts the active jobs across every page. The API reports the total of a filtered query in
   * `totalCount`, so a one-item page is enough.
   */
  async getActiveJobCount(): Promise<number> {
    const page = await this.getJobs({ isActive: true }, { pageNumber: 1, pageSize: 1 });
    return page.totalCount;
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
    return parseDrivers(await this.request<unknown>('/drivers'));
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
    return parseOverdueInvoices(await this.request<unknown>('/billing/invoices/overdue'));
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