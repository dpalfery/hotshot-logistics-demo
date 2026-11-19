// <copyright file="AccountsReceivableReport.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace HotshotLogistics.Api.Models
{
    /// <summary>
    ///     Report model for accounts receivable.
    /// </summary>
    public sealed record AccountsReceivableReport
    {
        /// <summary>
        ///     Gets the total overdue amount.
        /// </summary>
        public decimal TotalOverdueAmount { get; init; }

        /// <summary>
        ///     Gets the count of overdue invoices.
        /// </summary>
        public int OverdueInvoiceCount { get; init; }

        /// <summary>
        ///     Gets the report generation timestamp.
        /// </summary>
        public DateTime GeneratedAt { get; init; }

        /// <summary>
        ///     Gets the list of overdue invoices.
        /// </summary>
        public List<OverdueInvoiceSummary> OverdueInvoices { get; init; } = new();
    }
}
