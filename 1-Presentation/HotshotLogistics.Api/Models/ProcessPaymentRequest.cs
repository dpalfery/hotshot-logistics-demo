// <copyright file="ProcessPaymentRequest.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace HotshotLogistics.Api.Models
{
    /// <summary>
    ///     Request model for processing payments.
    /// </summary>
    public sealed record ProcessPaymentRequest
    {
        /// <summary>
        ///     Gets the payment amount.
        /// </summary>
        public decimal Amount { get; init; }

        /// <summary>
        ///     Gets the payment method.
        /// </summary>
        public string PaymentMethod { get; init; } = string.Empty;

        /// <summary>
        ///     Gets optional payment reference.
        /// </summary>
        public string? Reference { get; init; }
    }
}
