// <copyright file="ResourceOwnerAuthorizationHandler.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace HotshotLogistics.Application.Authorization
{
    /// <summary>
    ///     Authorization handler for resource ownership requirements.
    /// </summary>
    public class ResourceOwnerAuthorizationHandler : AuthorizationHandler<ResourceOwnerRequirement>
    {
        private readonly ILogger<ResourceOwnerAuthorizationHandler> _logger;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ResourceOwnerAuthorizationHandler" /> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public ResourceOwnerAuthorizationHandler(ILogger<ResourceOwnerAuthorizationHandler> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            ResourceOwnerRequirement requirement)
        {
            if (context.User?.Identity is not { IsAuthenticated: true })
            {
                _logger.LogWarning("Authorization failed: User is null or unauthenticated");
                return Task.CompletedTask;
            }

            // Check if user has Admin role - admins can access all resources
            if (context.User.HasClaim(c => c is { Type: "roles", Value: "Admin" }))
            {
                _logger.LogInformation("Authorization granted: User has Admin role for resource type {ResourceType}",
                    requirement.ResourceType);
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            // Check if user has Manager role - managers can access most resources
            if (context.User.HasClaim(c => c is { Type: "roles", Value: "Manager" }) &&
                requirement.ResourceType != "System")
            {
                _logger.LogInformation("Authorization granted: User has Manager role for resource type {ResourceType}",
                    requirement.ResourceType);
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            // For resource-specific access, check ownership
            string? resourceId = context.Resource as string;
            if (!string.IsNullOrEmpty(resourceId))
            {
                string? userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                                 context.User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")
                                     ?.Value ??
                                 context.User.FindFirst("oid")?.Value;

                if (!string.IsNullOrEmpty(userId))
                {
                    // For customer resources, check if the user is associated with the customer
                    if (requirement.ResourceType == "Customer" && IsCustomerResourceOwner(userId, resourceId))
                    {
                        _logger.LogInformation(
                            "Authorization granted: User {UserId} owns customer resource {ResourceId}", userId,
                            resourceId);
                        context.Succeed(requirement);
                        return Task.CompletedTask;
                    }

                    // For job resources, check if the user is assigned to the job
                    if (requirement.ResourceType == "Job" && IsJobResourceOwner(userId, resourceId))
                    {
                        _logger.LogInformation("Authorization granted: User {UserId} owns job resource {ResourceId}",
                            userId, resourceId);
                        context.Succeed(requirement);
                        return Task.CompletedTask;
                    }
                }
            }

            _logger.LogWarning("Authorization denied: User does not have access to resource type {ResourceType}",
                requirement.ResourceType);
            return Task.CompletedTask;
        }

        /// <summary>
        ///     Checks if the user owns the specified customer resource.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="resourceId">The resource ID.</param>
        /// <returns>True if the user owns the resource; otherwise, false.</returns>
        private static bool IsCustomerResourceOwner(string userId, string resourceId)
        {
            // TODO: Implement actual ownership check against database
            // For now, return false - this would need to be implemented based on business rules
            // e.g., check if user is associated with customer as contact or owner
            _ = userId;
            _ = resourceId;
            return false;
        }

        /// <summary>
        ///     Checks if the user owns the specified job resource.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="resourceId">The resource ID.</param>
        /// <returns>True if the user owns the resource; otherwise, false.</returns>
        private static bool IsJobResourceOwner(string userId, string resourceId)
        {
            // TODO: Implement actual ownership check against database
            // For now, return false - this would need to be implemented based on business rules
            // e.g., check if user is the driver assigned to the job
            _ = userId;
            _ = resourceId;
            return false;
        }
    }
}
