namespace HotshotLogistics.Domain.DTOs
{
    /// <summary>
    ///     Represents invoice summary statistics.
    /// </summary>
    public class InvoiceSummary
    {
        /// <summary>
        ///     Gets or sets the total number of invoices.
        /// </summary>
        public int TotalInvoices { get; set; }

        /// <summary>
        ///     Gets or sets the total invoice amount.
        /// </summary>
        public decimal TotalAmount { get; set; }

        /// <summary>
        ///     Gets or sets the total paid amount.
        /// </summary>
        public decimal TotalPaid { get; set; }

        /// <summary>
        ///     Gets or sets the total outstanding amount.
        /// </summary>
        public decimal TotalOutstanding { get; set; }

        /// <summary>
        ///     Gets or sets the number of overdue invoices.
        /// </summary>
        public int OverdueCount { get; set; }

        /// <summary>
        ///     Gets or sets the total overdue amount.
        /// </summary>
        public decimal OverdueAmount { get; set; }

        /// <summary>
        ///     Gets or sets the average days to payment.
        /// </summary>
        public double AverageDaysToPayment { get; set; }
    }
}
