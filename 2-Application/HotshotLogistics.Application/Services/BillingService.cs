// <copyright file="BillingService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using HotshotLogistics.Contracts.Repositories;
using HotshotLogistics.Contracts.Services;
using HotshotLogistics.Core.Enums;
using HotshotLogistics.Core.Exceptions;
using HotshotLogistics.Domain.DTOs;
using HotshotLogistics.Domain.Entities;
using HotshotLogistics.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;

namespace HotshotLogistics.Application.Services
{
    /// <summary>
    ///     Service for billing and invoice management operations.
    /// </summary>
    public class BillingService : IBillingService
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IJobRepository _jobRepository;
        private readonly ILogger<BillingService> _logger;
        private readonly INotificationService _notificationService;
        private readonly IPaymentProcessorFactory _paymentProcessorFactory;
        private readonly IPaymentRepository _paymentRepository;

        private readonly AsyncRetryPolicy _retryPolicy;

        // Tax rates by state (simplified for demo)
        private readonly Dictionary<string, decimal> _stateTaxRates = new()
        {
            { "CA", 0.0875m }, // California
            { "TX", 0.0625m }, // Texas
            { "NY", 0.08m }, // New York
            { "FL", 0.06m }, // Florida
            { "WA", 0.065m } // Washington
            // Add more states as needed
        };

        /// <summary>
        ///     Initializes a new instance of the <see cref="BillingService" /> class.
        /// </summary>
        /// <param name="invoiceRepository">The invoice repository.</param>
        /// <param name="jobRepository">The job repository.</param>
        /// <param name="customerRepository">The customer repository.</param>
        /// <param name="paymentRepository">The payment repository.</param>
        /// <param name="notificationService">The notification service.</param>
        /// <param name="paymentProcessorFactory">The payment processor factory.</param>
        /// <param name="logger">The _logger.</param>
        /// <param name="retryOptions">The payment retry settings; defaults apply when omitted.</param>
        public BillingService(
            IInvoiceRepository invoiceRepository,
            IJobRepository jobRepository,
            ICustomerRepository customerRepository,
            IPaymentRepository paymentRepository,
            INotificationService notificationService,
            IPaymentProcessorFactory paymentProcessorFactory,
            ILogger<BillingService> logger,
            IOptions<PaymentRetryOptions>? retryOptions = null)
        {
            _invoiceRepository = invoiceRepository ?? throw new ArgumentNullException(nameof(invoiceRepository));
            _jobRepository = jobRepository ?? throw new ArgumentNullException(nameof(jobRepository));
            _customerRepository = customerRepository ?? throw new ArgumentNullException(nameof(customerRepository));
            _paymentRepository = paymentRepository ?? throw new ArgumentNullException(nameof(paymentRepository));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _paymentProcessorFactory = paymentProcessorFactory ??
                                       throw new ArgumentNullException(nameof(paymentProcessorFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Retry only what cannot have charged the customer: transport failures and the processor's
            // explicitly transient error codes. Caller cancellation and unknown exceptions are never retried.
            PaymentRetryOptions retry = retryOptions?.Value ?? new PaymentRetryOptions();
            _retryPolicy = Policy
                .Handle<HttpRequestException>()
                .Or<TimeoutException>()
                .Or<ExternalServiceException>()
                .Or<PaymentProcessingException>(ex =>
                    ex.ProviderErrorCode is not null && retry.TransientErrorCodes.Contains(ex.ProviderErrorCode))
                .WaitAndRetryAsync(
                    retry.MaxRetries,
                    retryAttempt => TimeSpan.FromTicks(retry.BaseDelay.Ticks << (retryAttempt - 1)),
                    (exception, timeSpan, retryCount, _) =>
                    {
                        _logger.LogWarning(exception,
                            "Payment processing failed, retrying in {RetryTimeSpan}. Retry attempt {RetryCount}",
                            timeSpan, retryCount);
                    });
        }

        /// <inheritdoc />
        public async Task<Invoice> GenerateInvoiceAsync(string jobId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Generating invoice for job");

            Job? job = await _jobRepository.GetJobByIdAsync(jobId, cancellationToken);
            if (job == null)
            {
                throw new ArgumentException($"Job {jobId} not found", nameof(jobId));
            }

            if (job.Status != JobStatus.Received)
            {
                throw new InvalidOperationException($"Cannot generate invoice for job with status: {job.Status}");
            }

            Customer? customer = await _customerRepository.GetByIdAsync(job.CustomerId, cancellationToken);
            if (customer == null)
            {
                throw new ArgumentException($"Customer {job.CustomerId} not found");
            }

            // Check if invoice already exists for this job
            List<Invoice> existingInvoices = (await _invoiceRepository.GetByJobIdAsync(jobId)).ToList();
            if (existingInvoices.Count > 0)
            {
                _logger.LogWarning("Invoice already exists for job");
                return existingInvoices[0];
            }

            // Generate invoice number
            string invoiceNumber = await _invoiceRepository.GetNextInvoiceNumberAsync();

            // Create invoice
            Invoice invoice = new()
            {
                Id = Guid.NewGuid().ToString(),
                InvoiceNumber = invoiceNumber,
                CustomerId = job.CustomerId,
                JobId = jobId,
                InvoiceDate = DateTime.UtcNow.Date,
                Status = InvoiceStatus.Draft,
                Terms = new PaymentTerms
                {
                    Days = 30, // NET 30
                    EarlyPaymentDiscount = 0.02m, // 2% discount
                    EarlyPaymentDiscountDays = 10, // if paid within 10 days
                    LatePaymentPenalty = 0.015m, // 1.5% penalty
                    LatePaymentPenaltyDays = 5 // after 5 days past due
                },
                Notes = $"Invoice for job: {job.Title}"
            };

            invoice.DueDate = invoice.InvoiceDate.AddDays(invoice.Terms.Days);

            // Add line items
            await AddInvoiceLineItemsAsync(invoice, job);

            // Calculate tax
            decimal taxRate = await CalculateTaxAsync(invoice.SubTotal, customer.BillingAddress.State,
                cancellationToken);
            invoice.TaxRate = taxRate;
            invoice.CalculateTotals();

            // Save invoice
            Invoice createdInvoice = await _invoiceRepository.AddAsync(invoice, cancellationToken);

            // Send notification to customer
            try
            {
                await _notificationService.SendNotificationAsync(
                    customer.Id,
                    NotificationType.InvoiceGenerated,
                    "New Invoice Generated",
                    $"Invoice {invoice.InvoiceNumber} has been generated for ${invoice.TotalAmount:F2}",
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send invoice notification to customer");
            }

            _logger.LogInformation("Invoice generated successfully: {InvoiceNumber} for job: {JobId}", invoiceNumber,
                jobId);
            return createdInvoice;
        }

        /// <inheritdoc />
        public Task<decimal> CalculateTaxAsync(decimal amount, string state,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(state))
            {
                return Task.FromResult(0m);
            }

            string stateCode = state.ToUpperInvariant();
            if (_stateTaxRates.TryGetValue(stateCode, out decimal taxRate))
            {
                return Task.FromResult(taxRate);
            }

            // Default tax rate if state not found
            _logger.LogWarning("Tax rate not found for state: {State}, using default rate", state);
            return Task.FromResult(0.07m); // 7% default
        }

        /// <inheritdoc />
        public async Task<bool> ProcessPaymentAsync(string invoiceId, decimal paymentAmount, string paymentMethod,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Processing payment of ${Amount} for invoice: {InvoiceId} using {PaymentMethod}",
                paymentAmount, invoiceId, paymentMethod);

            Invoice? invoice = await _invoiceRepository.GetByIdAsync(invoiceId, cancellationToken);
            if (invoice == null)
            {
                throw new ArgumentException($"Invoice {invoiceId} not found", nameof(invoiceId));
            }

            if (paymentAmount <= 0)
            {
                throw new ArgumentException("Payment amount must be greater than zero", nameof(paymentAmount));
            }

            if (paymentAmount > invoice.BalanceDue)
            {
                throw new ArgumentException("Payment amount cannot exceed balance due", nameof(paymentAmount));
            }

            // Create payment record
            Payment payment = new()
            {
                Id = Guid.NewGuid().ToString(),
                InvoiceId = invoiceId,
                Amount = paymentAmount,
                PaymentMethod = ParsePaymentMethod(paymentMethod),
                Status = PaymentStatus.Processing
            };

            await _paymentRepository.AddAsync(payment, cancellationToken);

            try
            {
                // Get the appropriate payment processor
                IPaymentProcessor processor =
                    _paymentProcessorFactory.GetProcessorForPaymentMethod(payment.PaymentMethod);

                // Prepare payment method details
                PaymentMethodDetailsDto paymentMethodDetails = new()
                {
                    Type = payment.PaymentMethod,
                    Token = paymentMethod, // In real implementation, this would be a secure token
                    AdditionalData = new Dictionary<string, string>
                    {
                        ["invoice_id"] = invoiceId,
                        ["customer_id"] = invoice.CustomerId
                    }
                };

                // Process payment with retry logic. The payment id is stable across attempts, so a
                // processor that honours idempotency keys cannot charge twice for one payment.
                PaymentProcessingResultDto result = await _retryPolicy.ExecuteAsync(async retryToken =>
                {
                    PaymentProcessingResultDto processingResult = await processor.ProcessPaymentAsync(
                        paymentAmount,
                        "USD", // Default currency
                        paymentMethodDetails,
                        new Dictionary<string, string>
                        {
                            ["invoice_number"] = invoice.InvoiceNumber,
                            ["customer_id"] = invoice.CustomerId,
                            ["idempotency_key"] = payment.Id
                        },
                        retryToken);

                    if (!processingResult.Success)
                    {
                        throw new PaymentProcessingException(
                            $"Payment processing failed: {processingResult.Message}",
                            processingResult.TransactionId,
                            processingResult.ErrorCode ?? string.Empty);
                    }

                    return processingResult;
                }, cancellationToken);

                // Update payment with successful result
                payment.MarkAsCompleted(result.TransactionId, result.Message);
                await _paymentRepository.UpdateAsync(payment, cancellationToken);

                // Update invoice with payment
                bool success =
                    await _invoiceRepository.UpdatePaidAmountAsync(invoiceId, invoice.PaidAmount + paymentAmount);
                if (!success)
                {
                    _logger.LogError("Failed to update invoice payment amount for invoice: {InvoiceId}", invoiceId);
                    return false;
                }

                // Send payment confirmation notification
                try
                {
                    await _notificationService.SendNotificationAsync(
                        invoice.CustomerId,
                        NotificationType.PaymentReceived,
                        "Payment Received",
                        $"Payment of ${paymentAmount:F2} received for invoice {invoice.InvoiceNumber}",
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send payment notification for invoice {InvoiceId}", invoiceId);
                }

                _logger.LogInformation(
                    "Payment processed successfully for invoice: {InvoiceId}, TransactionId: {TransactionId}",
                    invoiceId, result.TransactionId);
                return true;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // The caller gave up: the outcome at the processor is unknown, so leave the payment
                // in Processing for reconciliation instead of recording a failure.
                throw;
            }
            catch (Exception ex)
            {
                // Deliberately broad: whatever went wrong, the payment record must leave Processing.
                // Update payment with failed status
                payment.MarkAsFailed(ex.Message);
                await _paymentRepository.UpdateAsync(payment, cancellationToken);

                _logger.LogError(ex, "Payment processing failed for invoice: {InvoiceId}", invoiceId);
                return false;
            }
        }

        /// <inheritdoc />
        public Task<IEnumerable<Invoice>> GetCustomerInvoicesAsync(string customerId,
            CancellationToken cancellationToken = default)
        {
            return _invoiceRepository.GetByCustomerIdAsync(customerId);
        }

        /// <inheritdoc />
        public async Task<Invoice?> GetInvoiceByIdAsync(string invoiceId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Retrieving invoice: {InvoiceId}", invoiceId);
            return await _invoiceRepository.GetByIdAsync(invoiceId, cancellationToken);
        }

        /// <inheritdoc />
        public Task<IEnumerable<Invoice>> GetOverdueInvoicesAsync(CancellationToken cancellationToken = default)
        {
            return _invoiceRepository.GetOverdueInvoicesAsync();
        }

        /// <inheritdoc />
        public Task<InvoiceSummaryDto> GetInvoiceSummaryAsync(CancellationToken cancellationToken = default)
        {
            return _invoiceRepository.GetInvoiceSummaryAsync();
        }

        /// <inheritdoc />
        public Task<IEnumerable<AgingReportEntryDto>> GetAgingReportAsync(CancellationToken cancellationToken = default)
        {
            return _invoiceRepository.GetAgingReportAsync();
        }

        /// <summary>
        ///     Creates a custom invoice not tied to a specific job.
        /// </summary>
        /// <param name="customerId">The customer identifier.</param>
        /// <param name="lineItems">The line items for the invoice.</param>
        /// <param name="notes">Optional notes for the invoice.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The created invoice.</returns>
        public async Task<Invoice> CreateCustomInvoiceAsync(string customerId, List<InvoiceLineItem> lineItems,
            string? notes = null, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Creating custom invoice for customer: {CustomerId}", customerId);

            Customer? customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken);
            if (customer == null)
            {
                throw new ArgumentException($"Customer {customerId} not found", nameof(customerId));
            }

            if (lineItems == null || !lineItems.Any())
            {
                throw new ArgumentException("At least one line item is required", nameof(lineItems));
            }

            // Generate invoice number
            string invoiceNumber = await _invoiceRepository.GetNextInvoiceNumberAsync();

            // Create invoice
            Invoice invoice = new()
            {
                Id = Guid.NewGuid().ToString(),
                InvoiceNumber = invoiceNumber,
                CustomerId = customerId,
                JobId = null, // Custom invoice not tied to a job
                InvoiceDate = DateTime.UtcNow.Date,
                Status = InvoiceStatus.Draft,
                Terms = new PaymentTerms
                {
                    Days = 30, // NET 30 default
                    EarlyPaymentDiscount = 0.02m, // 2% discount
                    EarlyPaymentDiscountDays = 10, // if paid within 10 days
                    LatePaymentPenalty = 0.015m, // 1.5% penalty
                    LatePaymentPenaltyDays = 5 // after 5 days past due
                },
                Notes = notes ?? string.Empty
            };

            invoice.DueDate = invoice.InvoiceDate.AddDays(invoice.Terms.Days);

            // Add line items
            foreach (InvoiceLineItem lineItem in lineItems)
            {
                invoice.AddLineItem(lineItem);
            }

            // Calculate tax based on customer location
            // Note: Assuming customer has a State property or we use a default
            string
                customerState =
                    "CA"; // Default to California - in real implementation, get from customer.BillingAddress
            decimal taxRate = await CalculateTaxAsync(invoice.SubTotal, customerState, cancellationToken);
            invoice.TaxRate = taxRate;
            invoice.CalculateTotals();

            // Save invoice
            Invoice createdInvoice = await _invoiceRepository.AddAsync(invoice, cancellationToken);

            _logger.LogInformation("Custom invoice created successfully: {InvoiceNumber} for customer: {CustomerId}",
                invoiceNumber, customerId);
            return createdInvoice;
        }

        /// <summary>
        ///     Sends an invoice to the customer.
        /// </summary>
        /// <param name="invoiceId">The invoice identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>True if the invoice was sent successfully.</returns>
        public async Task<bool> SendInvoiceAsync(string invoiceId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Sending invoice: {InvoiceId}", invoiceId);

            Invoice? invoice = await _invoiceRepository.GetByIdAsync(invoiceId, cancellationToken);
            if (invoice == null)
            {
                throw new ArgumentException($"Invoice {invoiceId} not found", nameof(invoiceId));
            }

            Customer? customer = await _customerRepository.GetByIdAsync(invoice.CustomerId, cancellationToken);
            if (customer == null)
            {
                throw new ArgumentException($"Customer {invoice.CustomerId} not found");
            }

            try
            {
                // Update invoice status to sent
                invoice.Status = InvoiceStatus.Sent;
                invoice.UpdatedAt = DateTime.UtcNow;
                await _invoiceRepository.UpdateAsync(invoice, cancellationToken);

                // Send notification to customer
                await _notificationService.SendNotificationAsync(
                    customer.Id,
                    NotificationType.InvoiceGenerated,
                    "Invoice Sent",
                    $"Invoice {invoice.InvoiceNumber} for ${invoice.TotalAmount:F2} has been sent. Due date: {invoice.DueDate:MM/dd/yyyy}",
                    cancellationToken);

                _logger.LogInformation("Invoice sent successfully: {InvoiceNumber}", invoice.InvoiceNumber);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send invoice: {InvoiceId}", invoiceId);
                return false;
            }
        }

        /// <summary>
        ///     Generates an account statement for a customer.
        /// </summary>
        /// <param name="customerId">The customer identifier.</param>
        /// <param name="startDate">The start date for the statement period.</param>
        /// <param name="endDate">The end date for the statement period.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The account statement data.</returns>
        public async Task<AccountStatement> GenerateAccountStatementAsync(string customerId, DateTime startDate,
            DateTime endDate, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "Generating account statement for customer: {CustomerId} from {StartDate} to {EndDate}", customerId,
                startDate, endDate);

            Customer? customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken);
            if (customer == null)
            {
                throw new ArgumentException($"Customer {customerId} not found", nameof(customerId));
            }

            IEnumerable<Invoice> invoices = await _invoiceRepository.GetByCustomerIdAsync(customerId);
            List<Invoice> statementInvoices =
                invoices.Where(i => i.InvoiceDate >= startDate && i.InvoiceDate <= endDate).ToList();

            AccountStatement statement = new()
            {
                CustomerId = customerId,
                CustomerName = customer.CompanyName,
                StatementDate = DateTime.UtcNow,
                PeriodStart = startDate,
                PeriodEnd = endDate,
                Invoices = statementInvoices.ToList(),
                TotalInvoiced = statementInvoices.Sum(i => i.TotalAmount),
                TotalPaid = statementInvoices.Sum(i => i.PaidAmount),
                TotalOutstanding = statementInvoices.Sum(i => i.BalanceDue),
                OverdueAmount = statementInvoices.Where(i => i.IsOverdue()).Sum(i => i.BalanceDue)
            };

            _logger.LogInformation(
                "Account statement generated for customer: {CustomerId}, Total Outstanding: ${TotalOutstanding:F2}",
                customerId, statement.TotalOutstanding);
            return statement;
        }

        /// <summary>
        ///     Applies late fees to all overdue invoices.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The number of invoices that had late fees applied.</returns>
        public async Task<int> ApplyLateFeeAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Applying late fees to overdue invoices");

            IEnumerable<Invoice> overdueInvoices = await _invoiceRepository.GetOverdueInvoicesAsync();
            int feesApplied = 0;

            foreach (Invoice invoice in overdueInvoices)
            {
                decimal latePenalty = invoice.CalculateLatePenalty();
                if (latePenalty > 0)
                {
                    // Add late fee as a line item
                    invoice.AddLineItem(new InvoiceLineItem
                    {
                        Description = $"Late Payment Fee ({invoice.DaysOverdue()} days overdue)",
                        Quantity = 1,
                        UnitPrice = latePenalty,
                        TaxApplicable = false
                    });

                    invoice.CalculateTotals();
                    await _invoiceRepository.UpdateAsync(invoice, cancellationToken);
                    feesApplied++;

                    _logger.LogInformation("Applied late fee of ${LateFee:F2} to invoice {InvoiceNumber}",
                        latePenalty, invoice.InvoiceNumber);
                }
            }

            _logger.LogInformation("Late fees applied to {Count} invoices", feesApplied);
            return feesApplied;
        }

        /// <summary>
        ///     Adds line items to an invoice based on the job details.
        /// </summary>
        /// <param name="invoice">The invoice to add line items to.</param>
        /// <param name="job">The job to create line items from.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task AddInvoiceLineItemsAsync(Invoice invoice, Job job)
        {
            // Base service charge
            if (job.Pricing.BaseRate > 0)
            {
                invoice.AddLineItem(new InvoiceLineItem
                {
                    Description = "Base Service Charge",
                    Quantity = 1,
                    UnitPrice = job.Pricing.BaseRate,
                    TaxApplicable = true
                });
            }

            // Mileage charge
            if (job.Pricing.MileageRate > 0)
            {
                // Calculate distance using location coordinates or estimate
                double? distanceFromCoords = job.PickupLocation.DistanceTo(job.DeliveryLocation);
                decimal estimatedMiles = distanceFromCoords.HasValue
                    ? (decimal)distanceFromCoords.Value
                    : (decimal)await EstimateDistanceAsync(job.PickupLocation.FullAddress,
                        job.DeliveryLocation.FullAddress);

                invoice.AddLineItem(new InvoiceLineItem
                {
                    Description = $"Mileage Charge ({estimatedMiles:F1} miles)",
                    Quantity = estimatedMiles,
                    UnitPrice = job.Pricing.MileageRate,
                    TaxApplicable = true
                });
            }

            // Fuel surcharge
            if (job.Pricing.FuelSurcharge > 0)
            {
                invoice.AddLineItem(new InvoiceLineItem
                {
                    Description = "Fuel Surcharge",
                    Quantity = 1,
                    UnitPrice = job.Pricing.FuelSurcharge,
                    TaxApplicable = true
                });
            }

            // Toll charges
            if (job.Pricing.TollCharges > 0)
            {
                invoice.AddLineItem(new InvoiceLineItem
                {
                    Description = "Toll Charges",
                    Quantity = 1,
                    UnitPrice = job.Pricing.TollCharges,
                    TaxApplicable = false // Tolls typically not taxed
                });
            }

            // Additional charges based on cargo
            if (job.Cargo.Weight > 1000) // Over 1000 lbs
            {
                decimal overweightCharge = (job.Cargo.Weight - 1000) * 0.10m; // $0.10 per lb over 1000
                invoice.AddLineItem(new InvoiceLineItem
                {
                    Description = $"Overweight Charge ({job.Cargo.Weight - 1000:F1} lbs over standard)",
                    Quantity = 1,
                    UnitPrice = overweightCharge,
                    TaxApplicable = true
                });
            }

            // High-value cargo insurance
            if (job.Cargo.Value > 10000) // Over $10,000
            {
                decimal insuranceCharge = job.Cargo.Value * 0.005m; // 0.5% of cargo value
                invoice.AddLineItem(new InvoiceLineItem
                {
                    Description = "High-Value Cargo Insurance",
                    Quantity = 1,
                    UnitPrice = insuranceCharge,
                    TaxApplicable = false // Insurance typically not taxed
                });
            }

            // Special handling charges
            if (job.Cargo.IsFragile || job.Cargo.IsHazardous || job.Cargo.RequiresTemperatureControl)
            {
                decimal specialHandlingCharge =
                    (job.Pricing.BaseRate > 0 ? job.Pricing.BaseRate : 100) * 0.15m; // 15% surcharge
                string requirements = job.Cargo.GetSpecialRequirements();

                invoice.AddLineItem(new InvoiceLineItem
                {
                    Description = $"Special Handling ({requirements})",
                    Quantity = 1,
                    UnitPrice = specialHandlingCharge,
                    TaxApplicable = true
                });
            }

            // Priority surcharge
            if (job.Priority == JobPriority.High)
            {
                decimal priorityCharge =
                    (job.Pricing.BaseRate > 0 ? job.Pricing.BaseRate : 100) *
                    0.25m; // 25% surcharge for high priority jobs
                invoice.AddLineItem(new InvoiceLineItem
                {
                    Description = "High Priority Surcharge",
                    Quantity = 1,
                    UnitPrice = priorityCharge,
                    TaxApplicable = true
                });
            }

            // Additional charges from pricing details
            if (job.Pricing.AdditionalCharges > 0)
            {
                invoice.AddLineItem(new InvoiceLineItem
                {
                    Description = "Additional Charges",
                    Quantity = 1,
                    UnitPrice = job.Pricing.AdditionalCharges,
                    TaxApplicable = true
                });
            }
        }

        /// <summary>
        ///     Estimates the distance between two addresses.
        /// </summary>
        /// <param name="fromAddress">The origin address.</param>
        /// <param name="toAddress">The destination address.</param>
        /// <returns>The estimated distance in miles.</returns>
        private Task<double> EstimateDistanceAsync(string fromAddress, string toAddress)
        {
            // Simplified distance calculation - in real implementation, use mapping service
            // For demo purposes, calculate based on address length as a rough estimate
            if (string.IsNullOrWhiteSpace(fromAddress) || string.IsNullOrWhiteSpace(toAddress))
            {
                return Task.FromResult(100.0); // Default 100 miles
            }

            // Simple heuristic: longer addresses are typically farther apart
            double addressLengthFactor = (fromAddress.Length + toAddress.Length) / 10.0;
            double estimatedDistance = Math.Max(10.0, Math.Min(500.0, addressLengthFactor * 5.0));

            return Task.FromResult(estimatedDistance);
        }

        /// <summary>
        ///     Parses the payment method string to PaymentMethodType enum.
        /// </summary>
        /// <param name="paymentMethod">The payment method string.</param>
        /// <returns>The PaymentMethodType.</returns>
        private PaymentMethodType ParsePaymentMethod(string paymentMethod)
        {
            return paymentMethod.ToLowerInvariant() switch
            {
                "credit card" or "card" or "stripe" => PaymentMethodType.CreditCard,
                "paypal" => PaymentMethodType.DigitalWallet,
                "ach" or "bank transfer" => PaymentMethodType.Ach,
                "check" => PaymentMethodType.Check,
                "wire transfer" => PaymentMethodType.WireTransfer,
                "cash" => PaymentMethodType.Cash,
                _ => PaymentMethodType.CreditCard // Default to credit card
            };
        }
    }
}
