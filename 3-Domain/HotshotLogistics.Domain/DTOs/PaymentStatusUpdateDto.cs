using HotshotLogistics.Core.Enums;

namespace HotshotLogistics.Domain.DTOs
{
    /// <summary>
    ///     Represents a payment status update from a webhook.
    /// </summary>
    public class PaymentStatusUpdateDto
    {
        /// <summary>
        ///     Gets or sets the transaction ID.
        /// </summary>
        public string TransactionId { get; set; } = string.Empty;

        /// <summary>
        ///     Gets or sets the new payment status.
        /// </summary>
        public PaymentStatus Status { get; set; }

        /// <summary>
        ///     Gets or sets the invoice ID.
        /// </summary>
        public string InvoiceId { get; set; } = string.Empty;

        /// <summary>
        ///     Gets or sets the amount.
        /// </summary>
        public decimal Amount { get; set; }
    }
}
