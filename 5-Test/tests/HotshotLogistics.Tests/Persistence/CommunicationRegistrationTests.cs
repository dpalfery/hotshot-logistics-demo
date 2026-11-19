// <copyright file="CommunicationRegistrationTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using FluentAssertions;
using HotshotLogistics.Application;
using HotshotLogistics.Application.Services;
using HotshotLogistics.Data;
using Microsoft.Extensions.DependencyInjection;

namespace HotshotLogistics.Tests.Persistence
{
    /// <summary>
    ///     Tests that communication providers and their factory are registered by the layers that own them.
    /// </summary>
    public class CommunicationRegistrationTests
    {
        /// <summary>
        ///     Tests that the persistence layer registers the providers but not the application's factory.
        /// </summary>
        [Fact]
        public void AddCommunicationServices_RegistersProvidersAndNoFactory()
        {
            ServiceCollection services = new();

            services.AddCommunicationServices();

            services.Count(d => d.ServiceType == typeof(ICommunicationService)).Should().Be(3);
            services.Should().NotContain(d => d.ServiceType == typeof(ICommunicationServiceFactory));
        }

        /// <summary>
        ///     Tests that the application layer registers the communication factory as a transient service.
        /// </summary>
        [Fact]
        public void AddApplicationServices_RegistersCommunicationServiceFactory()
        {
            ServiceCollection services = new();

            services.AddApplicationServices();

            services.Should().ContainSingle(d => d.ServiceType == typeof(ICommunicationServiceFactory))
                .Which.Should().Match<ServiceDescriptor>(d =>
                    d.ImplementationType == typeof(CommunicationServiceFactory) &&
                    d.Lifetime == ServiceLifetime.Transient);
        }
    }
}
