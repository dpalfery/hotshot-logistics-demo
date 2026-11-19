// <copyright file="TaxCalculationResult.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace HotshotLogistics.Api.Models
{
    /// <summary>
    ///     Result model for tax calculations.
    /// </summary>
    public sealed record TaxCalculationResult
    {
        /// <summary>
        ///     Gets the original amount.
        /// </summary>
        public decimal Amount { get; init; }

        /// <summary>
        ///     Gets the state.
        /// </summary>
        public string State { get; init; } = string.Empty;

        /// <summary>
        ///     Gets the calculated tax amount.
        /// </summary>
        public decimal TaxAmount { get; init; }

        /// <summary>
        ///     Gets the total amount including tax.
        /// </summary>
        public decimal TotalAmount { get; init; }

        /// <summary>
        ///     Gets the tax rate applied.
        /// </summary>
        public decimal TaxRate { get; init; }
    }
}
