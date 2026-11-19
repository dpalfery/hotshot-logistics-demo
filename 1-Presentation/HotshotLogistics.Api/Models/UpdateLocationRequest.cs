// <copyright file="UpdateLocationRequest.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using HotshotLogistics.Domain.Entities;

namespace HotshotLogistics.Api.Models
{
    /// <summary>
    ///     Request model for updating location.
    /// </summary>
    public sealed record UpdateLocationRequest
    {
        /// <summary>
        ///     Gets the job ID.
        /// </summary>
        public string JobId { get; init; } = string.Empty;

        /// <summary>
        ///     Gets the driver ID.
        /// </summary>
        public int DriverId { get; init; }

        /// <summary>
        ///     Gets the location update data.
        /// </summary>
        public LocationUpdate? LocationUpdate { get; init; }
    }
}
