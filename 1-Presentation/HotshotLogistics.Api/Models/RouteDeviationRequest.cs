// <copyright file="RouteDeviationRequest.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using HotshotLogistics.Domain.Entities;

namespace HotshotLogistics.Api.Models
{
    /// <summary>
    ///     Request model for checking route deviation.
    /// </summary>
    public sealed record RouteDeviationRequest
    {
        /// <summary>
        ///     Gets the job ID.
        /// </summary>
        public string JobId { get; init; } = string.Empty;

        /// <summary>
        ///     Gets the current location.
        /// </summary>
        public LocationUpdate? CurrentLocation { get; init; }
    }
}
