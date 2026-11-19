// <copyright file="UpdateJobStatusRequest.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using HotshotLogistics.Core.Enums;

namespace HotshotLogistics.Api.Models
{
    /// <summary>
    ///     Request model for updating job status.
    /// </summary>
    public sealed record UpdateJobStatusRequest
    {
        /// <summary>
        ///     Gets the new job status.
        /// </summary>
        public JobStatus Status { get; init; }
    }
}
