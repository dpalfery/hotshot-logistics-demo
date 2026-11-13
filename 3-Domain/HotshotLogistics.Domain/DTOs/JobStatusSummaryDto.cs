// <copyright file="JobStatusSummaryDto.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace HotshotLogistics.Domain.DTOs
{
    /// <summary>
    ///     Represents a summary of jobs grouped by status.
    /// </summary>
    public class JobStatusSummaryDto
    {
        /// <summary>
        ///     Gets or sets the number of jobs currently pending.
        /// </summary>
        public int Pending { get; set; }

        /// <summary>
        ///     Gets or sets the number of jobs assigned to drivers.
        /// </summary>
        public int Assigned { get; set; }

        /// <summary>
        ///     Gets or sets the number of jobs currently en route.
        /// </summary>
        public int EnRoute { get; set; }

        /// <summary>
        ///     Gets or sets the number of jobs received by customers.
        /// </summary>
        public int Received { get; set; }
    }
}
