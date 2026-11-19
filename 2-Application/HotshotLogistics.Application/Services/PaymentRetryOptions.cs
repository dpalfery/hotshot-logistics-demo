// <copyright file="PaymentRetryOptions.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.ComponentModel.DataAnnotations;

namespace HotshotLogistics.Application.Services
{
    /// <summary>
    ///     Settings for retrying a payment attempt, bound from <c>Payments:Retry</c>. Retrying a charge
    ///     is not idempotent on its own, so only transport failures and the processor's explicitly
    ///     transient error codes are retried; a decline or an unknown failure never is.
    /// </summary>
    public sealed class PaymentRetryOptions
    {
        /// <summary>
        ///     The configuration section these options bind from.
        /// </summary>
        public const string SectionName = "Payments:Retry";

        /// <summary>
        ///     Gets or sets how many times a failed attempt is retried after the first one.
        /// </summary>
        [Range(0, 10)]
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        ///     Gets or sets the delay before the first retry; each later retry doubles it.
        /// </summary>
        public TimeSpan BaseDelay { get; set; } = TimeSpan.FromSeconds(2);

        /// <summary>
        ///     Gets or sets the processor error codes that mark a failure as safe to retry.
        /// </summary>
        public string[] TransientErrorCodes { get; set; } =
            ["TEMPORARY_ERROR", "TIMEOUT", "RATE_LIMITED", "SERVICE_UNAVAILABLE"];
    }
}
