namespace HotshotLogistics.Domain.DTOs
{
    /// <summary>
    ///     Represents the result of webhook processing.
    /// </summary>
    public class WebhookProcessingResult
    {
        /// <summary>
        ///     Gets or sets a value indicating whether the webhook was processed successfully.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether the request failed signature verification, so nothing was processed.
        /// </summary>
        public bool SignatureRejected { get; set; }

        /// <summary>
        ///     Gets or sets the payment status update if applicable.
        /// </summary>
        public PaymentStatusUpdate? StatusUpdate { get; set; }

        /// <summary>
        ///     Gets or sets the message.
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }
}
