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
        Task<PaymentProcessingResultDto> ProcessPaymentAsync(
            decimal amount,
            string currency,
            PaymentMethodDetailsDto paymentMethod,
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
        Task<PaymentProcessingResultDto> RefundPaymentAsync(
            string transactionId,
            decimal amount,
            string reason,
            CancellationToken cancellationToken = default);

        /// <summary>
        ///     Verifies that a webhook request was signed by the provider, using the provider's webhook
        ///     secret or id from configuration. A request that is unsigned, wrongly signed or outside the
        ///     provider's replay window is not valid.
        /// </summary>
        /// <param name="webhookData">The webhook payload and request headers.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>True if the signature is valid, false otherwise.</returns>
        Task<bool> ValidateWebhookSignatureAsync(
            WebhookEventDataDto webhookData,
            CancellationToken cancellationToken = default);

        /// <summary>
        ///     Processes a webhook event. The signature is verified first; a request that fails verification is
        ///     not processed and the result has <see cref="WebhookProcessingResultDto.SignatureRejected" /> set.
        /// </summary>
        /// <param name="webhookData">The webhook event data.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The webhook processing result.</returns>
        Task<WebhookProcessingResultDto> ProcessWebhookAsync(
            WebhookEventDataDto webhookData,
            CancellationToken cancellationToken = default);
    }
}
