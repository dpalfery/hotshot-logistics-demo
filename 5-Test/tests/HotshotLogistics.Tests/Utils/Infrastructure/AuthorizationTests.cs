// <copyright file="AuthorizationTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Security.Claims;
using FluentAssertions;
using HotshotLogistics.Application.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Moq;

namespace HotshotLogistics.Tests.Utils.Infrastructure
{
    /// <summary>
    ///     Integration tests for authorization policies and handlers.
    /// </summary>
    public class AuthorizationTests
    {
        private readonly Mock<ILogger<ResourceOwnerAuthorizationHandler>> _loggerMock = new();

        /// <summary>
        ///     Tests that Admin role grants access to all resources.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task ResourceOwnerAuthorizationHandler_AdminRole_GrantsAccess()
        {
            // Arrange
            ResourceOwnerAuthorizationHandler handler = new(_loggerMock.Object);
            ClaimsPrincipal user = new(new ClaimsIdentity(
            [
                new Claim("roles", "Admin")
            ], "Test"));
            AuthorizationHandlerContext context = new(
                [new ResourceOwnerRequirement("Customer")],
                user,
                "resource123");

            // Act
            await handler.HandleAsync(context);

            // Assert
            context.HasSucceeded.Should().BeTrue();
        }

        /// <summary>
        ///     Tests that Manager role grants access to non-system resources.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task ResourceOwnerAuthorizationHandler_ManagerRole_GrantsAccessToNonSystemResources()
        {
            // Arrange
            ResourceOwnerAuthorizationHandler handler = new(_loggerMock.Object);
            ClaimsPrincipal user = new(new ClaimsIdentity(
            [
                new Claim("roles", "Manager")
            ], "Test"));
            AuthorizationHandlerContext context = new(
                [new ResourceOwnerRequirement("Customer")],
                user,
                "resource123");

            // Act
            await handler.HandleAsync(context);

            // Assert
            context.HasSucceeded.Should().BeTrue();
        }

        /// <summary>
        ///     Tests that Manager role denies access to system resources.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task ResourceOwnerAuthorizationHandler_ManagerRole_DeniesAccessToSystemResources()
        {
            // Arrange
            ResourceOwnerAuthorizationHandler handler = new(_loggerMock.Object);
            ClaimsPrincipal user = new(new ClaimsIdentity(
            [
                new Claim("roles", "Manager")
            ], "Test"));
            AuthorizationHandlerContext context = new(
                [new ResourceOwnerRequirement("System")],
                user,
                "resource123");

            // Act
            await handler.HandleAsync(context);

            // Assert
            context.HasSucceeded.Should().BeFalse();
        }

        /// <summary>
        ///     Tests that users without appropriate roles are denied access.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task ResourceOwnerAuthorizationHandler_NoAppropriateRole_DeniesAccess()
        {
            // Arrange
            ResourceOwnerAuthorizationHandler handler = new(_loggerMock.Object);
            ClaimsPrincipal user = new(new ClaimsIdentity(
            [
                new Claim("roles", "Driver")
            ], "Test"));
            AuthorizationHandlerContext context = new(
                [new ResourceOwnerRequirement("Customer")],
                user,
                "resource123");

            // Act
            await handler.HandleAsync(context);

            // Assert
            context.HasSucceeded.Should().BeFalse();
        }

        /// <summary>
        ///     Tests that an unauthenticated identity is denied even when it carries the Admin role.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task ResourceOwnerAuthorizationHandler_UnauthenticatedAdmin_DeniesAccess()
        {
            // Arrange
            ResourceOwnerAuthorizationHandler handler = new(_loggerMock.Object);
            ClaimsPrincipal user = new(new ClaimsIdentity(
            [
                new Claim("roles", "Admin")
            ]));
            AuthorizationHandlerContext context = new(
                [new ResourceOwnerRequirement("Customer")],
                user,
                "resource123");

            // Act
            await handler.HandleAsync(context);

            // Assert
            context.HasSucceeded.Should().BeFalse();
        }

        /// <summary>
        ///     Tests that null user denies access.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task ResourceOwnerAuthorizationHandler_NullUser_DeniesAccess()
        {
            // Arrange
            ResourceOwnerAuthorizationHandler handler = new(_loggerMock.Object);
            AuthorizationHandlerContext context = new(
                [new ResourceOwnerRequirement("Customer")],
                null!,
                "resource123");

            // Act
            await handler.HandleAsync(context);

            // Assert
            context.HasSucceeded.Should().BeFalse();
        }

        /// <summary>
        ///     Tests authorization policies configuration.
        /// </summary>
        [Fact]
        public void AuthorizationPolicies_Constants_AreDefined()
        {
            // Assert
            AuthorizationPolicies.Admin.Should().Be("Admin");
            AuthorizationPolicies.Manager.Should().Be("Manager");
            AuthorizationPolicies.Driver.Should().Be("Driver");
            AuthorizationPolicies.Customer.Should().Be("Customer");
            AuthorizationPolicies.ManagerOrAdmin.Should().Be("ManagerOrAdmin");
            AuthorizationPolicies.ManagerOrDriver.Should().Be("ManagerOrDriver");
            AuthorizationPolicies.OwnResource.Should().Be("OwnResource");
            AuthorizationPolicies.CustomerResource.Should().Be("CustomerResource");
        }
    }
}
