using HotshotLogistics.Core.Enums;

namespace HotshotLogistics.Domain.DTOs
{
    /// <summary>
    ///     Filter criteria for invoice queries.
    /// </summary>
    public class InvoiceFilterDto
    {
        /// <summary>
        ///     Gets or sets the customer identifier filter.
        /// </summary>
        public string? CustomerId { get; set; }

        /// <summary>
        ///     Gets or sets the job identifier filter.
        /// </summary>
        public string? JobId { get; set; }

        /// <summary>
        ///     Gets or sets the status filter.
        /// </summary>
        public InvoiceStatus? Status { get; set; }

        /// <summary>
        ///     Gets or sets the start date filter.
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        ///     Gets or sets the end date filter.
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        ///     Gets or sets the minimum amount filter.
        /// </summary>
        public decimal? MinAmount { get; set; }

        /// <summary>
        ///     Gets or sets the maximum amount filter.
        /// </summary>
        public decimal? MaxAmount { get; set; }

        /// <summary>
        ///     Gets or sets the search term for invoice number or customer name.
        /// </summary>
        public string? SearchTerm { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether to include only overdue invoices.
        /// </summary>
        public bool? IsOverdue { get; set; }

        /// <summary>
        ///     Gets or sets the page number for pagination.
        /// </summary>
        public int PageNumber { get; set; } = 1;

        /// <summary>
        ///     Gets or sets the page size for pagination.
        /// </summary>
        public int PageSize { get; set; } = 20;

        /// <summary>
        ///     Gets or sets the sort field.
        /// </summary>
        public string SortBy { get; set; } = "InvoiceDate";

        /// <summary>
        ///     Gets or sets the sort direction.
        /// </summary>
        public string SortDirection { get; set; } = "DESC";
    }
}
