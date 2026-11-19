// <copyright file="UpdateCreditLimitRequest.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace HotshotLogistics.Api.Models
{
    /// <summary>
    ///     Request model for updating customer credit limit.
    /// </summary>
    public sealed record UpdateCreditLimitRequest
    {
        /// <summary>
        ///     Gets the new credit limit.
        /// </summary>
        public decimal NewLimit { get; init; }
    }
}
