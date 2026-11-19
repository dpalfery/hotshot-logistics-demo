// <copyright file="Program.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Azure.Identity;
using HotshotLogistics.Api;
using HotshotLogistics.Api.Errors;
using HotshotLogistics.Api.Health;
using HotshotLogistics.Application;
using HotshotLogistics.Application.Hubs;
using HotshotLogistics.Core.Extensions;
using HotshotLogistics.Data;
using HotshotLogistics.Domain.DTOs;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Graph;
using Microsoft.Identity.Web;
using Microsoft.OpenApi;
using Newtonsoft.Json;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// User secrets (Development, or optional elsewhere) supply App Config connection details locally.
builder.Configuration.AddUserSecrets(typeof(Program).Assembly, true);
bool azureAppConfigurationEnabled = builder.Configuration.AddAzureAppConfigurationIfConfigured();

// Configure settings
builder.Services.Configure<GoogleMapsSettings>(builder.Configuration.GetSection("Mapping:GoogleMaps"));
builder.Services.Configure<AzureMapsSettings>(builder.Configuration.GetSection("Mapping:AzureMaps"));
builder.Services.Configure<SendGridSettings>(builder.Configuration.GetSection("Communication:SendGrid"));

// Add services to the container.
builder.Services.AddHotshotRepositories();
builder.Services.AddApplicationServices();
builder.Services.AddMappingServices();
builder.Services.AddCommunicationServices();

// Add services to the container
if (builder.Environment.IsDevelopment())
{
    // Use test authentication handler for local development
    builder.Services.AddAuthentication("Test")
        .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
}
else
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAdB2C"));
}

builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        // System.Text.Json 9+/10 requires PipeWriter.UnflushedBytes, which the ASP.NET Core 8
        // test host does not implement when tests roll forward to .NET 10.
        options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
        options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "HotshotLogistics.Api", Version = "v1" });
});

// One error contract: ProblemDetails plus the domain exception handler
builder.Services.AddApiErrorHandling();
builder.Services.AddApiHealthChecks(builder.Configuration);

// Add HTTP client factory for external API calls
builder.Services.AddHttpClient();

// Add distributed cache (using in-memory for development)
builder.Services.AddDistributedMemoryCache();

if (azureAppConfigurationEnabled)
{
    builder.Services.AddAzureAppConfiguration();
}

// Register GraphServiceClient
builder.Services.AddScoped(_ =>
{
    DefaultAzureCredentialOptions options = new()
    {
        ExcludeAzureCliCredential = true,
        ExcludeEnvironmentCredential = true,
        ExcludeManagedIdentityCredential = false,
        ExcludeVisualStudioCredential = true,
        ExcludeInteractiveBrowserCredential = true
    };
    DefaultAzureCredential credential = new(options);
    return new GraphServiceClient(credential);
});

// Add authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("Manager", policy => policy.RequireRole("Manager"));
    options.AddPolicy("Driver", policy => policy.RequireRole("Driver"));
    options.AddPolicy("Customer", policy => policy.RequireRole("Customer"));
    options.AddPolicy("ManagerOrAdmin", policy => policy.RequireRole("Admin", "Manager"));
    options.AddPolicy("ManagerOrDriver", policy => policy.RequireRole("Manager", "Driver"));
    options.AddPolicy("OwnResource",
        policy => policy.RequireAuthenticatedUser()); // Placeholder for resource-based auth
    options.AddPolicy("CustomerResource",
        policy => policy.RequireAuthenticatedUser()); // Placeholder for resource-based auth
});

// Configure SignalR
builder.Services.AddSignalR();

WebApplication app = builder.Build();

app.UseApiErrorHandling();

if (azureAppConfigurationEnabled)
{
    app.UseAzureAppConfiguration();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Configure CORS policy - must be after UseHttpsRedirection but before UseAuthentication
app.UseHttpsRedirection();

string[] allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

app.UseCors(policy =>
{
    policy.WithOrigins(allowedOrigins)
        .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
        .WithHeaders("Content-Type", "Authorization", "X-Requested-With", "x-signalr-user-agent")
        .AllowCredentials();
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapApiHealthChecks();
app.MapHub<RealtimeHub>("/realtime");

app.Run();
