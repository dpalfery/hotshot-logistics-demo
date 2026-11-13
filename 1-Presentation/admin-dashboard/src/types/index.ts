// Job types
export interface Job {
  id: string;
  title: string;
  pickupAddress: string;
  dropoffAddress: string;
  status: JobStatus;
  priority: JobPriority;
  amount: number;
  estimatedDeliveryTime: string;
  assignedDriverId?: number;
  createdAt: string;
  updatedAt?: string;
  scheduledPickupTime: string;
  specialInstructions: string;
  customerId: string;
  pickupLocation?: Location;
  deliveryLocation?: Location;
  cargo?: CargoDetails;
  pricing?: PricingDetails;
  actualPickupTime?: string;
  actualDeliveryTime?: string;
  documents: JobDocument[];
  tracking: TrackingInfo;
}

export enum JobStatus {
  Pending = 'Pending',
  Assigned = 'Assigned',
  InProgress = 'InProgress',
  Completed = 'Completed',
  Cancelled = 'Cancelled'
}

export interface JobStatusSummary {
  pending: number;
  assigned: number;
  enRoute: number;
  received: number;
}

export enum JobPriority {
  Low = 'Low',
  Normal = 'Normal',
  High = 'High',
  Urgent = 'Urgent'
}

export interface Location {
  latitude: number;
  longitude: number;
  address: string;
}

export interface CargoDetails {
  description: string;
  weight?: number;
  dimensions?: string;
  value?: number;
  specialHandling?: string[];
}

export interface PricingDetails {
  baseRate: number;
  distance: number;
  weight?: number;
  urgencyMultiplier: number;
  total: number;
}

export interface JobDocument {
  id: string;
  type: string;
  url: string;
  uploadedAt: string;
}

export interface TrackingInfo {
  currentLocation?: Location;
  lastUpdate?: string;
  eta?: string;
  status: string;
}

// Driver types
export interface Driver {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
  licenseNumber: string;
  licenseExpiryDate: string;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string;
}

// Invoice types
export interface Invoice {
  id: string;
  invoiceNumber: string;
  customerId: string;
  jobId?: string;
  invoiceDate: string;
  dueDate: string;
  status: InvoiceStatus;
  lineItems: InvoiceLineItem[];
  subTotal: number;
  taxRate: number;
  taxAmount: number;
  discountAmount: number;
  totalAmount: number;
  paidAmount: number;
  balanceDue: number;
  terms?: PaymentTerms;
  notes: string;
  createdAt: string;
  updatedAt?: string;
}

export enum InvoiceStatus {
  Draft = 'Draft',
  Sent = 'Sent',
  Viewed = 'Viewed',
  PartiallyPaid = 'PartiallyPaid',
  Paid = 'Paid',
  Overdue = 'Overdue',
  Cancelled = 'Cancelled'
}

export interface InvoiceLineItem {
  id: number;
  description: string;
  quantity: number;
  unitPrice: number;
  amount: number;
  taxApplicable: boolean;
  sortOrder: number;
}

export interface PaymentTerms {
  netDays: number;
  earlyPaymentDiscount: number;
  earlyPaymentDiscountDays: number;
  latePaymentPenalty: number;
  latePaymentPenaltyDays: number;
}

export interface InvoiceSummaryMetrics {
  totalInvoiced: number;
  totalPaid: number;
  totalOutstanding: number;
  overdueAmount: number;
}

export interface InvoiceAgingBuckets {
  current: number;
  days30: number;
  days60: number;
  days90: number;
  over90: number;
  total: number;
}

// Customer types
export interface Customer {
  id: string;
  name: string;
  email: string;
  phoneNumber: string;
  address: string;
  creditTerms: PaymentTerms;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string;
}

// API response types
export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

export interface PaginationParameters {
  pageNumber: number;
  pageSize: number;
}

// Filter types
export interface JobFilter {
  status?: JobStatus;
  priority?: JobPriority;
  assignedDriverId?: number;
  customerId?: string;
  dateFrom?: string;
  dateTo?: string;
}

export interface InvoiceFilter {
  status?: InvoiceStatus;
  customerId?: string;
  dateFrom?: string;
  dateTo?: string;
  overdueOnly?: boolean;
}

// Real-time types
export interface LocationUpdate {
  jobId: string;
  driverId: number;
  location: Location;
  timestamp: string;
  speed?: number;
  heading?: number;
}

export interface NotificationMessage {
  id: string;
  type: string;
  title: string;
  message: string;
  timestamp: string;
  read: boolean;
  data?: Record<string, unknown>;
}