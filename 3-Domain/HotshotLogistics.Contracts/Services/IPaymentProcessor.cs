using HotshotLogistics.Domain.DTOs;

namespace HotshotLogistics.Contracts.Services
{
    /// <summary>
    ///     Interface for payment processing operations.
    /// </summary>
    public interface IPaymentProcessor
    {
        /// <summary>
        ///     Gets the name of the payment processor.
        /// </summary>
        string ProcessorName { get; }

        /// <summary>
        ///     Processes a payment through the payment gateway.
        /// </summary>
        /// <param name="amount">The payment amount.</param>
        /// <param name="currency">The currency code (e.g., "USD").</param>
        /// <param name="paymentMethod">The payment method details.</param>
        /// <param name="metadata">Additional metadata for the payment.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The payment processing result.</returns>
        Task<PaymentProcessingResult> ProcessPaymentAsync(
            decimal amount,
            string currency,
            PaymentMethodDetails paymentMethod,
            Dictionary<string, string> metadata,
            CancellationToken cancellationToken = default);

        /// <summary>
        ///     Refunds a payment.
        /// </summary>
        /// <param name="transactionId">The original transaction ID.</param>
        /// <param name="amount">The refund amount.</param>
        /// <param name="reason">The reason for the refund.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The refund processing result.</returns>
        Task<PaymentProcessingResult> RefundPaymentAsync(
            string transactionId,
            decimal amount,
            string reason,
            CancellationToken cancellationToken = default);

        /// <summary>
        ///     Validates webhook signature for security.
        /// </summary>
        /// <param name="payload">The webhook payload.</param>
        /// <param name="signature">The signature to validate.</param>
        /// <param name="secret">The webhook secret.</param>
        /// <returns>True if the signature is valid, false otherwise.</returns>
        bool ValidateWebhookSignature(string payload, string signature, string secret);

        /// <summary>
        ///     Processes a webhook event.
        /// </summary>
        /// <param name="webhookData">The webhook event data.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The webhook processing result.</returns>
        Task<WebhookProcessingResult> ProcessWebhookAsync(
            WebhookEventData webhookData,
            CancellationToken cancellationToken = default);
    }
}
