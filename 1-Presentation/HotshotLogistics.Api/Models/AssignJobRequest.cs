// <copyright file="AssignJobRequest.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace HotshotLogistics.Api.Models
{
    /// <summary>
    ///     Request model for assigning a job to a driver.
    /// </summary>
    public sealed record AssignJobRequest
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
