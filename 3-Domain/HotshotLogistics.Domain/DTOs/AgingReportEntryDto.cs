namespace HotshotLogistics.Domain.DTOs
{
    /// <summary>
    ///     Represents an entry in the aging report.
    /// </summary>
    public class AgingReportEntryDto
    {
        /// <summary>
        ///     Gets or sets the customer identifier.
        /// </summary>
        public string CustomerId { get; set; } = string.Empty;

        /// <summary>
        ///     Gets or sets the customer name.
        /// </summary>
        public string CustomerName { get; set; } = string.Empty;

        /// <summary>
        ///     Gets or sets the balance that is not yet due.
        /// </summary>
        public decimal Current { get; set; }

        /// <summary>
        ///     Gets or sets the balance 1–30 days past due.
        /// </summary>
        public decimal Days1To30 { get; set; }

        /// <summary>
        ///     Gets or sets the balance 31–60 days past due.
        /// </summary>
        public decimal Days31To60 { get; set; }

        /// <summary>
        ///     Gets or sets the balance 61–90 days past due.
        /// </summary>
        public decimal Days61To90 { get; set; }

        /// <summary>
        ///     Gets or sets the over 90 days balance.
        /// </summary>
        public decimal Over90Days { get; set; }

        /// <summary>
        ///     Gets or sets the total balance.
        /// </summary>
        public decimal TotalBalance { get; set; }
    }
}
