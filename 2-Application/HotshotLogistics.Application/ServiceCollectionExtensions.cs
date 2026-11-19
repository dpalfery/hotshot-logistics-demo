using FluentValidation;
using HotshotLogistics.Application.Services;
using HotshotLogistics.Application.Validators;
using HotshotLogistics.Contracts.Factories;
using HotshotLogistics.Contracts.Hubs;
using HotshotLogistics.Contracts.Repositories;
using HotshotLogistics.Contracts.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HotshotLogistics.Application
{
    /// <summary>
    ///     Extension methods for setting up services in the application.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        ///     Adds application services to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Register services
            services.AddScoped<IConnectionManagerService, ConnectionManagerService>();
            services.AddScoped<IDriverService, DriverService>();
            services.AddScoped<IJobService>(sp => new JobService(
                sp.GetRequiredService<IJobRepository>(),
                sp.GetRequiredService<ICustomerRepository>(),
                sp.GetRequiredService<IDriverRepository>(),
                sp.GetRequiredService<INotificationService>(),
                sp.GetRequiredService<IMappingServiceFactory>().CreateMappingService(),
                sp.GetRequiredService<ILogger<JobService>>()));
            services.AddScoped<IJobAssignmentService, JobAssignmentService>();
            services.AddScoped<IRealtimeService, RealtimeService>();
            services.AddScoped<ISignalRClientWrapper, SignalRClientWrapper>();
            services.AddScoped<UserProfileService, UserProfileService>();

            // Register billing and payment services
            services.AddScoped<IBillingService, BillingService>();
            services.AddScoped<ICustomerService, CustomerService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<ITrackingService, TrackingService>();

            /* Register payment processors */
            services.AddScoped<StripePaymentProcessor>();
            services.AddScoped<PayPalPaymentProcessor>();
            services.AddScoped<IPaymentProcessorFactory, PaymentProcessorFactory>();

            // The ICommunicationService implementations are registered by the persistence layer's
            // AddCommunicationServices(); the factory that selects among them lives here.
            services.AddTransient<ICommunicationServiceFactory, CommunicationServiceFactory>();

            // Do not also register IMappingService as a factory callback. MappingServiceFactory
            // enumerates GetServices<IMappingService>(), and a callback here would recurse.
            services.AddSingleton<IMappingServiceFactory, MappingServiceFactory>();

            // Register validators
            services.AddValidatorsFromAssemblyContaining<CreateJobValidator>();

            return services;
        }
    }
}
