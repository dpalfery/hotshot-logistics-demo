// <copyright file="PaymentResult.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace HotshotLogistics.Api.Models
{
    /// <summary>
    ///     Result model for payment processing.
    /// </summary>
    public sealed record PaymentResult
    {
        /// <summary>
        ///     Gets a value indicating whether the payment was successful.
        /// </summary>
        public bool Success { get; init; }

        /// <summary>
        ///     Gets the invoice ID.
        /// </summary>
        public string InvoiceId { get; init; } = string.Empty;

        /// <summary>
        ///     Gets the payment amount.
        /// </summary>
        public decimal Amount { get; init; }

        /// <summary>
        ///     Gets the payment method.
        /// </summary>
        public string PaymentMethod { get; init; } = string.Empty;

        /// <summary>
        ///     Gets the processing timestamp.
        /// </summary>
        public DateTime ProcessedAt { get; init; }
    }
}
