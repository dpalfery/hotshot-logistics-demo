// <copyright file="ServiceCollectionExtensions.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using HotshotLogistics.Contracts.Repositories;
using HotshotLogistics.Contracts.Services;
using HotshotLogistics.Data.Http;
using HotshotLogistics.Data.Repositories;
using HotshotLogistics.Data.Services;
using HotshotLogistics.Domain.DTOs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HotshotLogistics.Data
{
    /// <summary>
    ///     Extension methods for configuring services.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        ///     Adds Hotshot repositories to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddHotshotRepositories(this IServiceCollection services)
        {
            services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
            services.AddScoped<ICustomerRepository, CustomerRepository>();
            services.AddScoped<IDriverRepository, DriverRepository>();
            services.AddScoped<IJobRepository, JobRepository>();
            services.AddScoped<IJobAssignmentRepository, JobAssignmentRepository>();
            services.AddScoped<IInvoiceRepository, InvoiceRepository>();
            services.AddScoped<ILocationTrackingRepository, LocationTrackingRepository>();
            services.AddScoped<IPaymentRepository, PaymentRepository>();
            return services;
        }

        public static IServiceCollection AddMappingServices(this IServiceCollection services)
        {
            services.AddHttpClient("MappingService").AddOutboundResilience();
            services.AddTransient<IMappingService, MockMappingService>();

            // Register AzureMapsService with proper HttpClient factory and settings
            services.AddTransient<IMappingService>(provider =>
            {
                IHttpClientFactory httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
                HttpClient httpClient = httpClientFactory.CreateClient("MappingService");
                ILogger<AzureMapsService> logger = provider.GetRequiredService<ILogger<AzureMapsService>>();
                IOptions<AzureMapsSettings> settings = provider.GetRequiredService<IOptions<AzureMapsSettings>>();
                return new AzureMapsService(httpClient, logger, settings);
            });

            // Register GoogleMapsService with proper HttpClient factory
            services.AddTransient<IMappingService>(provider =>
            {
                IHttpClientFactory httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
                HttpClient httpClient = httpClientFactory.CreateClient("MappingService");
                ILogger<GoogleMapsService> logger = provider.GetRequiredService<ILogger<GoogleMapsService>>();
                IOptions<GoogleMapsSettings> settings = provider.GetRequiredService<IOptions<GoogleMapsSettings>>();
                return new GoogleMapsService(httpClient, logger, settings);
            });

            return services;
        }

        public static IServiceCollection AddCommunicationServices(this IServiceCollection services)
        {
            services.AddTransient<ICommunicationService, TwilioSmsService>();
            services.AddTransient<ICommunicationService, SendGridEmailService>();
            services.AddTransient<ICommunicationService, AzureNotificationHubService>();
            return services;
        }
    }
}
