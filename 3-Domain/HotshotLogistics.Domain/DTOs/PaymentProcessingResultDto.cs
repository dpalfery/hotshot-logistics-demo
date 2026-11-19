namespace HotshotLogistics.Domain.DTOs
{
    /// <summary>
    ///     Represents the result of a payment processing operation.
    /// </summary>
    public class PaymentProcessingResultDto
    {
        /// <summary>
        ///     Gets or sets a value indicating whether the operation was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        ///     Gets or sets the transaction ID.
        /// </summary>
        public string TransactionId { get; set; } = string.Empty;

        /// <summary>
        ///     Gets or sets the processor response message.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        ///     Gets or sets additional metadata from the processor.
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; } = new();

        /// <summary>
        ///     Gets or sets the error code if the operation failed.
        /// </summary>
        public string? ErrorCode { get; set; }
    }
}
