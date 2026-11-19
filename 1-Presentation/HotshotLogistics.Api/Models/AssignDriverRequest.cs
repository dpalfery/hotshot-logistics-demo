// <copyright file="AssignDriverRequest.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace HotshotLogistics.Api.Models
{
    /// <summary>
    ///     Request model for assigning a driver to a job.
    /// </summary>
    public sealed record AssignDriverRequest
    {
        /// <summary>
        ///     Gets the driver ID.
        /// </summary>
        public int DriverId { get; init; }
    }
}
