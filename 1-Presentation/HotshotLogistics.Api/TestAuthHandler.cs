// <copyright file="TestAuthHandler.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Globalization;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace HotshotLogistics.Api
{
    /// <summary>
    ///     Test authentication handler for development and integration testing.
    /// </summary>
    public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private const string s_developmentToken = "test-token";
        private const string s_driverIdHeader = "X-Test-Driver-Id";
        private const string s_hubBearerToken = "Bearer " + s_developmentToken;
        private static readonly PathString s_hubPath = new("/realtime");

        private readonly string _defaultRole;

        /// <summary>
        ///     Initializes a new instance of the <see cref="TestAuthHandler" /> class.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="encoder">The URL encoder.</param>
        /// <param name="defaultRole">The default role.</param>
        public TestAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            string defaultRole = "Admin")
            : base(options, logger, encoder)
        {
            _defaultRole = defaultRole;
        }

        /// <inheritdoc />
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // Require the 'Test' scheme for dev/testing. The /realtime hub is the one place a browser cannot
            // set the Authorization header (a WebSocket), so there the development token may also arrive as
            // the access_token query value, or as the Bearer token SignalR sends on negotiate.
            string authHeader = Request.Headers["Authorization"].ToString();
            bool hubRequest = Request.Path.StartsWithSegments(s_hubPath);
            bool testScheme = authHeader.StartsWith("Test", StringComparison.OrdinalIgnoreCase) ||
                              (hubRequest && (authHeader.Equals(s_hubBearerToken, StringComparison.Ordinal) ||
                                              (authHeader.Length == 0 &&
                                               Request.Query["access_token"] == s_developmentToken)));
            if (!testScheme)
            {
                return Task.FromResult(AuthenticateResult.Fail(authHeader.Length == 0
                    ? "Missing Authorization Header"
                    : "Invalid Authorization Scheme"));
            }

            // Allow dynamic role per request via X-Test-Role header; default to configured _defaultRole
            string requestedRole = _defaultRole;
            if (Request.Headers.TryGetValue("X-Test-Role", out StringValues roleHeader) &&
                !string.IsNullOrWhiteSpace(roleHeader.ToString()))
            {
                requestedRole = roleHeader.ToString().Trim();
            }

            List<Claim> claims =
            [
                new(ClaimTypes.NameIdentifier, "test-user-id"),
                new(ClaimTypes.Name, "Test User"),
                new(ClaimTypes.Email, "test@example.com"),
                new(ClaimTypes.Role, requestedRole),
                new("roles", requestedRole)
            ];

            if (Request.Headers.TryGetValue(s_driverIdHeader, out StringValues driverHeader)
                && int.TryParse(driverHeader.ToString(), NumberStyles.None, CultureInfo.InvariantCulture, out int driverId)
                && driverId > 0)
            {
                claims.Add(new Claim("driver_id", driverId.ToString(CultureInfo.InvariantCulture)));
            }

            ClaimsIdentity identity = new(claims, "Test");
            ClaimsPrincipal principal = new(identity);
            AuthenticationTicket ticket = new(principal, "Test");

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
