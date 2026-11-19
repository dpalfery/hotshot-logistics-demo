// <copyright file="InvoiceAgingBucketsResponse.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace HotshotLogistics.Api.Models
{
    /// <summary>
    ///     Dashboard invoice aging buckets.
    /// </summary>
    public sealed record InvoiceAgingBucketsResponse
    {
        public decimal Current { get; init; }

        public decimal Days30 { get; init; }

        public decimal Days60 { get; init; }

        public decimal Days90 { get; init; }

        public decimal Over90 { get; init; }

        public decimal Total { get; init; }
    }
}
