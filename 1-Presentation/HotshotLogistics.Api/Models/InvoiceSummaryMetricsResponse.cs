// <copyright file="InvoiceSummaryMetricsResponse.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace HotshotLogistics.Api.Models
{
    /// <summary>
    ///     Dashboard invoice summary metrics.
    /// </summary>
    public sealed record InvoiceSummaryMetricsResponse
    {
        public decimal TotalInvoiced { get; init; }

        public decimal TotalPaid { get; init; }

        public decimal TotalOutstanding { get; init; }

        public decimal OverdueAmount { get; init; }
    }
}
