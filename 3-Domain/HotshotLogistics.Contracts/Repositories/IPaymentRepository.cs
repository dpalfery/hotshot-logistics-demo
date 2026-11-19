using HotshotLogistics.Core.Enums;
using HotshotLogistics.Domain.Entities;

namespace HotshotLogistics.Contracts.Repositories
{
    /// <summary>
    ///     Repository interface for payment operations.
    /// </summary>
    public interface IPaymentRepository : IRepository<Payment>
    {
        /// <summary>
        ///     Gets payments by invoice ID.
        /// </summary>
        /// <param name="invoiceId">The invoice ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The payments for the invoice.</returns>
        Task<IEnumerable<Payment>> GetByInvoiceIdAsync(string invoiceId, CancellationToken cancellationToken = default);

        /// <summary>
        ///     Gets payments by transaction ID.
        /// </summary>
        /// <param name="transactionId">The transaction ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The payment with the transaction ID.</returns>
        Task<Payment?> GetByTransactionIdAsync(string transactionId, CancellationToken cancellationToken = default);

        /// <summary>
        ///     Gets payments by status.
        /// </summary>
        /// <param name="status">The payment status.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The payments with the specified status.</returns>
        Task<IEnumerable<Payment>>
            GetByStatusAsync(PaymentStatus status, CancellationToken cancellationToken = default);

        /// <summary>
        ///     Updates the payment status.
        /// </summary>
        /// <param name="paymentId">The payment ID.</param>
        /// <param name="status">The new status.</param>
        /// <param name="processorResponse">The processor response.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>True if the update was successful.</returns>
        Task<bool> UpdatePaymentStatusAsync(string paymentId, PaymentStatus status, string? processorResponse = null,
            CancellationToken cancellationToken = default);
    }
}
