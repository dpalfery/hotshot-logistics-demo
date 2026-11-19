// <copyright file="BillingController.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using HotshotLogistics.Api.Models;
using HotshotLogistics.Application.Authorization;
using HotshotLogistics.Contracts.Services;
using HotshotLogistics.Core.Enums;
using HotshotLogistics.Domain.DTOs;
using HotshotLogistics.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using HotshotLogistics.Api.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HotshotLogistics.Api.Controllers
{
    /// <summary>
    ///     API controller for billing and financial operations.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public sealed class BillingController : ApiControllerBase
    {
        private const int s_maxWebhookBodyBytes = 256 * 1024;

        private readonly IBillingService _billingService;
        private readonly ILogger<BillingController> _logger;
        private readonly IPaymentProcessorFactory _paymentProcessorFactory;

        /// <summary>
        ///     Initializes a new instance of the <see cref="BillingController" /> class.
        /// </summary>
        /// <param name="billingService">The billing service.</param>
        /// <param name="paymentProcessorFactory">The payment processor factory.</param>
        /// <param name="logger">The _logger.</param>
        public BillingController(
            IBillingService billingService,
            IPaymentProcessorFactory paymentProcessorFactory,
            ILogger<BillingController> logger)
        {
            _billingService = billingService ?? throw new ArgumentNullException(nameof(billingService));
            _paymentProcessorFactory = paymentProcessorFactory ??
                                       throw new ArgumentNullException(nameof(paymentProcessorFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        ///     Generates an invoice for a completed job.
        /// </summary>
        /// <param name="jobId">The job ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The generated invoice.</returns>
        [HttpPost("invoices/generate/{jobId}")]
        [Authorize(Policy = AuthorizationPolicies.ManagerOrAdmin)]
        [ProducesResponseType(typeof(Invoice), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Invoice>> GenerateInvoice(string jobId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                Invoice invoice = await _billingService.GenerateInvoiceAsync(jobId, cancellationToken);
                return CreatedAtAction(
                    nameof(GetInvoice),
                    new { id = invoice.Id },
                    invoice);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid job ID provided for invoice generation");
                return BadRequestProblem(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Job not found for invoice generation");
                return NotFoundProblem(ex.Message);
            }
        }

        /// <summary>
        ///     Gets an invoice by ID.
        /// </summary>
        /// <param name="id">The invoice ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The invoice if found; otherwise, 404 Not Found.</returns>
        [HttpGet("invoices/{id}")]
        [Authorize(Policy = AuthorizationPolicies.OwnResource)]
        [ProducesResponseType(typeof(Invoice), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Invoice>> GetInvoice(string id, CancellationToken cancellationToken = default)
        {
            Invoice? invoice = await _billingService.GetInvoiceByIdAsync(id, cancellationToken);
            if (invoice == null)
            {
                return NotFoundProblem($"Invoice with ID {id} not found");
            }

            return Ok(invoice);
        }

        /// <summary>
        ///     Gets invoices for a specific customer.
        /// </summary>
        /// <param name="customerId">The customer ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of invoices for the customer.</returns>
        [HttpGet("invoices/customer/{customerId}")]
        [Authorize(Policy = AuthorizationPolicies.CustomerResource)]
        [ProducesResponseType(typeof(IEnumerable<Invoice>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<Invoice>>> GetCustomerInvoices(string customerId,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<Invoice> invoices =
                await _billingService.GetCustomerInvoicesAsync(customerId, cancellationToken);
            return Ok(invoices);
        }

        /// <summary>
        ///     Gets invoice summary metrics for the dashboard.
        /// </summary>
        [HttpGet("/api/invoices/summary")]
        [Authorize(Policy = AuthorizationPolicies.ManagerOrAdmin)]
        [ProducesResponseType(typeof(InvoiceSummaryMetricsResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<InvoiceSummaryMetricsResponse>> GetInvoiceSummary(
            CancellationToken cancellationToken = default)
        {
            InvoiceSummary summary = await _billingService.GetInvoiceSummaryAsync(cancellationToken);
            return Ok(new InvoiceSummaryMetricsResponse
            {
                TotalInvoiced = summary.TotalAmount,
                TotalPaid = summary.TotalPaid,
                TotalOutstanding = summary.TotalOutstanding,
                OverdueAmount = summary.OverdueAmount
            });
        }

        /// <summary>
        ///     Gets invoice aging buckets for the dashboard.
        /// </summary>
        [HttpGet("/api/invoices/aging")]
        [Authorize(Policy = AuthorizationPolicies.ManagerOrAdmin)]
        [ProducesResponseType(typeof(InvoiceAgingBucketsResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<InvoiceAgingBucketsResponse>> GetInvoiceAging(
            CancellationToken cancellationToken = default)
        {
            List<AgingReportEntry> entries =
                (await _billingService.GetAgingReportAsync(cancellationToken)).ToList();
            return Ok(new InvoiceAgingBucketsResponse
            {
                Current = entries.Sum(e => e.Current),
                Days30 = entries.Sum(e => e.Days31To60),
                Days60 = 0,
                Days90 = entries.Sum(e => e.Days61To90),
                Over90 = entries.Sum(e => e.Over90Days),
                Total = entries.Sum(e => e.TotalBalance)
            });
        }

        /// <summary>
        ///     Gets overdue invoices.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A list of overdue invoices.</returns>
        [HttpGet("invoices/overdue")]
        [Authorize(Policy = AuthorizationPolicies.ManagerOrAdmin)]
        [ProducesResponseType(typeof(IEnumerable<Invoice>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<Invoice>>> GetOverdueInvoices(
            CancellationToken cancellationToken = default)
        {
            IEnumerable<Invoice> invoices = await _billingService.GetOverdueInvoicesAsync(cancellationToken);
            return Ok(invoices);
        }

        /// <summary>
        ///     Processes a payment for an invoice.
        /// </summary>
        /// <param name="invoiceId">The invoice ID.</param>
        /// <param name="request">The payment request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Success status of the payment processing.</returns>
        [HttpPost("invoices/{invoiceId}/payments")]
        [Authorize(Policy = AuthorizationPolicies.CustomerResource)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PaymentResult>> ProcessPayment(
            string invoiceId,
            [FromBody] ProcessPaymentRequest? request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (request == null)
                {
                    return BadRequestProblem("Payment request is required");
                }

                if (request.Amount <= 0)
                {
                    return BadRequestProblem("Payment amount must be greater than zero");
                }

                if (string.IsNullOrWhiteSpace(request.PaymentMethod))
                {
                    return BadRequestProblem("Payment method is required");
                }

                bool success = await _billingService.ProcessPaymentAsync(
                    invoiceId,
                    request.Amount,
                    request.PaymentMethod,
                    cancellationToken);

                PaymentResult result = new()
                {
                    Success = success,
                    InvoiceId = invoiceId,
                    Amount = request.Amount,
                    PaymentMethod = request.PaymentMethod,
                    ProcessedAt = DateTime.UtcNow
                };

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid payment request for invoice: {Message}", ex.Message);
                return BadRequestProblem(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Invoice not found for payment processing");
                return NotFoundProblem(ex.Message);
            }
        }

        /// <summary>
        ///     Calculates tax for a given amount and location.
        /// </summary>
        /// <param name="request">The tax calculation request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The calculated tax amount.</returns>
        [HttpPost("tax/calculate")]
        [Authorize(Policy = AuthorizationPolicies.ManagerOrAdmin)]
        [ProducesResponseType(typeof(TaxCalculationResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<TaxCalculationResult>> CalculateTax(
            [FromBody] TaxCalculationRequest? request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (request == null)
                {
                    return BadRequestProblem("Tax calculation request is required");
                }

                if (request.Amount <= 0)
                {
                    return BadRequestProblem("Amount must be greater than zero");
                }

                if (string.IsNullOrWhiteSpace(request.State))
                {
                    return BadRequestProblem("State is required for tax calculation");
                }

                decimal taxAmount =
                    await _billingService.CalculateTaxAsync(request.Amount, request.State, cancellationToken);

                TaxCalculationResult result = new()
                {
                    Amount = request.Amount,
                    State = request.State,
                    TaxAmount = taxAmount,
                    TotalAmount = request.Amount + taxAmount,
                    TaxRate = request.Amount > 0 ? taxAmount / request.Amount : 0
                };

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid tax calculation request: {Message}", ex.Message);
                return BadRequestProblem(ex.Message);
            }
        }

        /// <summary>
        ///     Gets accounts receivable report.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Accounts receivable summary.</returns>
        [HttpGet("reports/accounts-receivable")]
        [Authorize(Policy = AuthorizationPolicies.ManagerOrAdmin)]
        [ProducesResponseType(typeof(AccountsReceivableReport), StatusCodes.Status200OK)]
        public async Task<ActionResult<AccountsReceivableReport>> GetAccountsReceivableReport(
            CancellationToken cancellationToken = default)
        {
            List<Invoice> overdueInvoices =
                (await _billingService.GetOverdueInvoicesAsync(cancellationToken)).ToList();

            AccountsReceivableReport report = new()
            {
                TotalOverdueAmount = overdueInvoices.Sum(i => i.BalanceDue),
                OverdueInvoiceCount = overdueInvoices.Count,
                GeneratedAt = DateTime.UtcNow,
                OverdueInvoices = overdueInvoices.Select(i => new OverdueInvoiceSummary
                {
                    InvoiceId = i.Id,
                    InvoiceNumber = i.InvoiceNumber,
                    CustomerId = i.CustomerId,
                    Amount = i.TotalAmount,
                    BalanceDue = i.BalanceDue,
                    DueDate = i.DueDate,
                    DaysOverdue = (int)(DateTime.UtcNow - i.DueDate).TotalDays
                }).ToList()
            };

            return Ok(report);
        }

        /// <summary>
        ///     Handles Stripe webhook events. Anonymous because Stripe cannot sign in; the
        ///     <c>Stripe-Signature</c> header is the authentication, and a request that fails it is rejected
        ///     before anything is processed.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>200 when the event was accepted, 401 when the signature is missing, wrong or stale.</returns>
        [HttpPost("webhooks/stripe")]
        [AllowAnonymous]
        [RequestSizeLimit(s_maxWebhookBodyBytes)]
        [EnableRateLimiting(RateLimitPolicies.Public)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public Task<IActionResult> HandleStripeWebhook(CancellationToken cancellationToken = default)
        {
            return HandleWebhookAsync("Stripe", "Stripe-Signature", ExtractStripeEventType, cancellationToken);
        }

        /// <summary>
        ///     Handles PayPal webhook events. Anonymous because PayPal cannot sign in; the
        ///     <c>PAYPAL-TRANSMISSION-SIG</c> signature and its companion headers are the authentication, and a
        ///     request that fails verification is rejected before anything is processed.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>200 when the event was accepted, 401 when the signature is missing, wrong or stale.</returns>
        [HttpPost("webhooks/paypal")]
        [AllowAnonymous]
        [RequestSizeLimit(s_maxWebhookBodyBytes)]
        [EnableRateLimiting(RateLimitPolicies.Public)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public Task<IActionResult> HandlePayPalWebhook(CancellationToken cancellationToken = default)
        {
            return HandleWebhookAsync("PayPal", "PAYPAL-TRANSMISSION-SIG", ExtractPayPalEventType, cancellationToken);
        }

        private async Task<IActionResult> HandleWebhookAsync(
            string processorName,
            string signatureHeaderName,
            Func<string, string> extractEventType,
            CancellationToken cancellationToken)
        {
            try
            {
                IPaymentProcessor processor = _paymentProcessorFactory.GetProcessor(processorName);

                using StreamReader reader = new(Request.Body);
                string payload = await reader.ReadToEndAsync(cancellationToken);

                WebhookEventData webhookData = new()
                {
                    Payload = payload,
                    Signature = Request.Headers[signatureHeaderName].FirstOrDefault() ?? string.Empty,
                    Headers = Request.Headers.ToDictionary(
                        h => h.Key,
                        h => h.Value.FirstOrDefault() ?? string.Empty,
                        StringComparer.OrdinalIgnoreCase),
                    EventType = extractEventType(payload)
                };

                WebhookProcessingResult result = await processor.ProcessWebhookAsync(webhookData, cancellationToken);

                if (result.SignatureRejected)
                {
                    _logger.LogWarning("{Processor} webhook rejected: the signature did not verify", processorName);
                    return UnauthorizedProblem("The webhook signature is invalid.");
                }

                if (!result.Success)
                {
                    _logger.LogWarning("{Processor} webhook processing failed", processorName);
                    return BadRequestProblem("Webhook processing failed");
                }

                if (result.StatusUpdate is not null)
                {
                    await HandlePaymentStatusUpdateAsync(result.StatusUpdate, cancellationToken);
                }

                _logger.LogInformation("{Processor} webhook processed", processorName);

                return Ok(new { received = true });
            }
            catch (Exception ex)
            {
                // Deliberately broad: the provider retries on any 4xx and must never see internals.
                _logger.LogError(ex, "Error processing {Processor} webhook", processorName);
                return BadRequestProblem("Webhook processing failed");
            }
        }

        /// <summary>
        ///     Handles payment status updates from webhooks.
        /// </summary>
        /// <param name="statusUpdate">The payment status update.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task HandlePaymentStatusUpdateAsync(PaymentStatusUpdate statusUpdate,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling payment status update: Status={Status}", statusUpdate.Status);

            try
            {
                // Update payment status in the database
                // Note: In a real implementation, you'd have a payment repository method for this
                // For now, we'll assume the payment record exists and update it

                if (statusUpdate.Status == PaymentStatus.Completed)
                {
                    // Update invoice with payment amount
                    IEnumerable<Invoice> invoice =
                        await _billingService.GetCustomerInvoicesAsync(statusUpdate.InvoiceId, cancellationToken);
                    Invoice? targetInvoice = invoice.FirstOrDefault(i => i.Id == statusUpdate.InvoiceId);

                    if (targetInvoice != null)
                    {
                        // Send payment confirmation notification
                        try
                        {
                            await _billingService.GetCustomerInvoicesAsync(targetInvoice.CustomerId,
                                cancellationToken); // Just to get customer context
                            // Note: In real implementation, you'd have a method to send payment notifications
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to send payment notification for invoice {InvoiceId}",
                                statusUpdate.InvoiceId);
                        }
                    }
                }

                _logger.LogInformation("Payment status update handled successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling payment status update");
                throw;
            }
        }

        /// <summary>
        ///     Extracts the event type from a Stripe webhook payload.
        /// </summary>
        /// <param name="payload">The webhook payload.</param>
        /// <returns>The event type.</returns>
        private string ExtractStripeEventType(string payload)
        {
            // Simplified extraction - in real implementation, parse JSON properly
            if (payload.Contains("payment_intent.succeeded"))
            {
                return "payment_intent.succeeded";
            }

            if (payload.Contains("payment_intent.payment_failed"))
            {
                return "payment_intent.payment_failed";
            }

            return "unknown";
        }

        /// <summary>
        ///     Extracts the event type from a PayPal webhook payload.
        /// </summary>
        /// <param name="payload">The webhook payload.</param>
        /// <returns>The event type.</returns>
        private string ExtractPayPalEventType(string payload)
        {
            // Simplified extraction - in real implementation, parse JSON properly
            if (payload.Contains("PAYMENT.CAPTURE.COMPLETED"))
            {
                return "PAYMENT.CAPTURE.COMPLETED";
            }

            if (payload.Contains("PAYMENT.CAPTURE.DENIED"))
            {
                return "PAYMENT.CAPTURE.DENIED";
            }

            return "unknown";
        }
    }
}
