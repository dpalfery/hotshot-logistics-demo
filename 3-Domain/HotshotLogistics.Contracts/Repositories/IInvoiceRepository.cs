using HotshotLogistics.Core.Enums;
using HotshotLogistics.Domain.DTOs;
using HotshotLogistics.Domain.Entities;

namespace HotshotLogistics.Contracts.Repositories
{
    /// <summary>
    ///     Repository interface for invoice operations. Getting and adding an invoice through the generic contract
    ///     loads and saves its line items.
    /// </summary>
    public interface IInvoiceRepository : IRepository<Invoice>
    {
        /// <summary>
        ///     Gets invoices by customer identifier.
        /// </summary>
        /// <param name="customerId">The customer identifier.</param>
        /// <returns>A list of invoices for the customer.</returns>
        Task<IEnumerable<Invoice>> GetByCustomerIdAsync(string customerId);

        /// <summary>
        ///     Gets invoices by job identifier.
        /// </summary>
        /// <param name="jobId">The job identifier.</param>
        /// <returns>A list of invoices for the job.</returns>
        Task<IEnumerable<Invoice>> GetByJobIdAsync(string jobId);

        /// <summary>
        ///     Gets invoices by status.
        /// </summary>
        /// <param name="status">The invoice status.</param>
        /// <returns>A list of invoices with the specified status.</returns>
        Task<IEnumerable<Invoice>> GetByStatusAsync(InvoiceStatus status);

        /// <summary>
        ///     Gets overdue invoices.
        /// </summary>
        /// <returns>A list of overdue invoices.</returns>
        Task<IEnumerable<Invoice>> GetOverdueInvoicesAsync();

        /// <summary>
        ///     Gets invoices due within a specified number of days.
        /// </summary>
        /// <param name="days">The number of days to look ahead.</param>
        /// <returns>A list of invoices due within the specified days.</returns>
        Task<IEnumerable<Invoice>> GetInvoicesDueWithinDaysAsync(int days);

        /// <summary>
        ///     Gets invoices by date range.
        /// </summary>
        /// <param name="startDate">The start date.</param>
        /// <param name="endDate">The end date.</param>
        /// <returns>A list of invoices within the date range.</returns>
        Task<IEnumerable<Invoice>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        ///     Gets invoices with pagination and filtering.
        /// </summary>
        /// <param name="filter">The invoice filter criteria.</param>
        /// <returns>A paged result of invoices.</returns>
        Task<PagedResult<Invoice>> GetPagedAsync(InvoiceFilterDto filter);

        /// <summary>
        ///     Gets the next invoice number.
        /// </summary>
        /// <returns>The next available invoice number.</returns>
        Task<string> GetNextInvoiceNumberAsync();

        /// <summary>
        ///     Gets aging report data for accounts receivable.
        /// </summary>
        /// <returns>A list of aging report entries.</returns>
        Task<IEnumerable<AgingReportEntryDto>> GetAgingReportAsync();

        /// <summary>
        ///     Gets total outstanding balance for a customer.
        /// </summary>
        /// <param name="customerId">The customer identifier.</param>
        /// <returns>The total outstanding balance.</returns>
        Task<decimal> GetOutstandingBalanceAsync(string customerId);

        /// <summary>
        ///     Gets invoice summary statistics.
        /// </summary>
        /// <returns>Invoice summary statistics.</returns>
        Task<InvoiceSummaryDto> GetInvoiceSummaryAsync();

        /// <summary>
        ///     Gets invoices by invoice number (partial match).
        /// </summary>
        /// <param name="invoiceNumber">The invoice number to search for.</param>
        /// <returns>A list of matching invoices.</returns>
        Task<IEnumerable<Invoice>> SearchByInvoiceNumberAsync(string invoiceNumber);

        /// <summary>
        ///     Updates the paid amount for an invoice.
        /// </summary>
        /// <param name="invoiceId">The invoice identifier.</param>
        /// <param name="paidAmount">The new paid amount.</param>
        /// <returns>True if the update was successful, false otherwise.</returns>
        Task<bool> UpdatePaidAmountAsync(string invoiceId, decimal paidAmount);

        /// <summary>
        ///     Updates the invoice status.
        /// </summary>
        /// <param name="invoiceId">The invoice identifier.</param>
        /// <param name="status">The new status.</param>
        /// <returns>True if the update was successful, false otherwise.</returns>
        Task<bool> UpdateStatusAsync(string invoiceId, InvoiceStatus status);

        /// <summary>
        ///     Generates a new invoice from job data.
        /// </summary>
        /// <param name="jobId">The job identifier to generate invoice for.</param>
        /// <returns>The generated invoice.</returns>
        Task<Invoice> GenerateInvoiceAsync(string jobId);
    }
}
