// <copyright file="RateLimitPolicies.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace HotshotLogistics.Api.RateLimiting
{
    /// <summary>
    ///     Names of the rate limiting policies that actions opt into with
    ///     <c>[EnableRateLimiting]</c>.
    /// </summary>
    public static class RateLimitPolicies
    {
        /// <summary>
        ///     The stricter policy for unauthenticated endpoints.
        /// </summary>
        public const string Public = "public";
    }
}
