// <copyright file="TaxCalculationRequest.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace HotshotLogistics.Api.Models
{
    /// <summary>
    ///     Request model for tax calculations.
    /// </summary>
    public sealed record TaxCalculationRequest
    {
        /// <summary>
        ///     Gets the amount to calculate tax for.
        /// </summary>
        public decimal Amount { get; init; }

        /// <summary>
        ///     Gets the state for tax calculation.
        /// </summary>
        public string State { get; init; } = string.Empty;
    }
}
