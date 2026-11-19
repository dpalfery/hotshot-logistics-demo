// <copyright file="UserProfileController.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using HotshotLogistics.Contracts.Services;
using HotshotLogistics.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotshotLogistics.Api.Controllers
{
    /// <summary>
    ///     API controller for user profile management operations.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public sealed class UserProfileController : ApiControllerBase
    {
        private readonly ILogger<UserProfileController> _logger;
        private readonly IUserProfileService _userProfileService;

        /// <summary>
        ///     Initializes a new instance of the <see cref="UserProfileController" /> class.
        /// </summary>
        /// <param name="userProfileService">The user profile service.</param>
        /// <param name="logger">The _logger.</param>
        public UserProfileController(
            IUserProfileService userProfileService,
            ILogger<UserProfileController> logger)
        {
            _userProfileService = userProfileService ?? throw new ArgumentNullException(nameof(userProfileService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        ///     Gets the current user's profile information.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The user profile information.</returns>
        [HttpGet("me")]
        [ProducesResponseType(typeof(UserProfile), StatusCodes.Status200OK)]
        public async Task<ActionResult<UserProfile>> GetCurrentUserProfile(
            CancellationToken cancellationToken = default)
        {
            UserProfile profile = await _userProfileService.GetCurrentUserProfileAsync(cancellationToken);
            return Ok(profile);
        }

        /// <summary>
        ///     Updates the current user's profile information.
        /// </summary>
        /// <param name="profile">The updated profile information.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>No content if successful.</returns>
        [HttpPut("me")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateCurrentUserProfile(
            [FromBody] UserProfile? profile,
            CancellationToken cancellationToken = default)
        {
            if (profile == null)
            {
                return BadRequestProblem("Profile data is required");
            }

            await _userProfileService.UpdateCurrentUserProfileAsync(profile, cancellationToken);
            return NoContent();
        }

        /// <summary>
        ///     Synchronizes the current user's profile with the local database.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>No content if successful.</returns>
        [HttpPost("sync")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> SyncUserProfile(CancellationToken cancellationToken = default)
        {
            // Get the current user ID from claims
            string? userId = User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
                             ?? User.FindFirst("oid")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return BadRequestProblem("Unable to identify current user");
            }

            await _userProfileService.SyncUserProfileAsync(userId, cancellationToken);
            return NoContent();
        }
    }
}
