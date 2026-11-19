// <copyright file="OverdueInvoiceSummary.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace HotshotLogistics.Api.Models
{
    /// <summary>
    ///     Summary model for overdue invoices.
    /// </summary>
    public sealed record OverdueInvoiceSummary
    {
        /// <summary>
        ///     Gets the invoice ID.
        /// </summary>
        public string InvoiceId { get; init; } = string.Empty;

        /// <summary>
        ///     Gets the invoice number.
        /// </summary>
        public string InvoiceNumber { get; init; } = string.Empty;

        /// <summary>
        ///     Gets the customer ID.
        /// </summary>
        public string CustomerId { get; init; } = string.Empty;

        /// <summary>
        ///     Gets the invoice amount.
        /// </summary>
        public decimal Amount { get; init; }

        /// <summary>
        ///     Gets the balance due.
        /// </summary>
        public decimal BalanceDue { get; init; }

        /// <summary>
        ///     Gets the due date.
        /// </summary>
        public DateTime DueDate { get; init; }

        /// <summary>
        ///     Gets the number of days overdue.
        /// </summary>
        public int DaysOverdue { get; init; }
    }
}
