// <copyright file="StartTrackingRequest.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace HotshotLogistics.Api.Models
{
    /// <summary>
    ///     Request model for starting tracking.
    /// </summary>
    public sealed record StartTrackingRequest
    {
        /// <summary>
        ///     Gets the job ID.
        /// </summary>
        public string JobId { get; init; } = string.Empty;

        /// <summary>
        ///     Gets the driver ID.
        /// </summary>
        public int DriverId { get; init; }
    }
}
