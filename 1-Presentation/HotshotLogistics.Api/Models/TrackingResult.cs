// <copyright file="TrackingResult.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace HotshotLogistics.Api.Models
{
    /// <summary>
    ///     Result model for tracking operations.
    /// </summary>
    public sealed record TrackingResult
    {
        /// <summary>
        ///     Gets a value indicating whether the operation was successful.
        /// </summary>
        public bool Success { get; init; }

        /// <summary>
        ///     Gets the job ID.
        /// </summary>
        public string JobId { get; init; } = string.Empty;

        /// <summary>
        ///     Gets the driver ID.
        /// </summary>
        public int? DriverId { get; init; }

        /// <summary>
        ///     Gets the result message.
        /// </summary>
        public string Message { get; init; } = string.Empty;

        /// <summary>
        ///     Gets the operation timestamp.
        /// </summary>
        public DateTime Timestamp { get; init; }
    }
}
