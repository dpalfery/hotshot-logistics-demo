// <copyright file="ExceptionHandlingMiddlewareTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Net;
using System.Text.Json;
using FluentAssertions;
using HotshotLogistics.Api.Middleware;
using HotshotLogistics.Core.Exceptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

namespace HotshotLogistics.Tests.Utils.Infrastructure
{
    /// <summary>
    ///     Integration tests for the ExceptionHandlingMiddleware.
    /// </summary>
    public class ExceptionHandlingMiddlewareTests
    {
        private readonly Mock<ILogger<ExceptionHandlingMiddleware>> _mockLogger;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ExceptionHandlingMiddlewareTests" /> class.
        /// </summary>
        public ExceptionHandlingMiddlewareTests()
        {
            _mockLogger = new Mock<ILogger<ExceptionHandlingMiddleware>>();
        }

        /// <summary>
        ///     Tests that ValidationException returns BadRequest with field errors.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task InvokeAsync_ValidationException_ReturnsBadRequestWithFieldErrors()
        {
            // Arrange
            Dictionary<string, string[]> fieldErrors = new()
            {
                { "Title", ["Title is required"] },
                { "Amount", ["Amount must be greater than 0"] }
            };

            ValidationException exception = new("Validation failed", fieldErrors);

            HttpResponseMessage response = await ExecuteMiddlewareTestAsync(exception);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            ErrorResponse errorResponse = await DeserializeErrorResponseAsync(response);
            errorResponse.Message.Should().Be("Validation failed");
            errorResponse.ErrorCode.Should().Be("VALIDATION_ERROR");
            errorResponse.FieldErrors.Should().BeEquivalentTo(fieldErrors);
            errorResponse.CorrelationId.Should().NotBeNullOrEmpty();
            errorResponse.Path.Should().Be("/test");
        }

        /// <summary>
        ///     Tests that BusinessRuleException returns UnprocessableEntity.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task InvokeAsync_BusinessRuleException_ReturnsUnprocessableEntity()
        {
            // Arrange
            BusinessRuleException exception = new("Business rule violated");

            HttpResponseMessage response = await ExecuteMiddlewareTestAsync(exception);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

            ErrorResponse errorResponse = await DeserializeErrorResponseAsync(response);
            errorResponse.Message.Should().Be("Business rule violated");
            errorResponse.ErrorCode.Should().Be("BUSINESS_RULE_VIOLATION");
            errorResponse.CorrelationId.Should().NotBeNullOrEmpty();
        }

        /// <summary>
        ///     Tests that PaymentProcessingException returns PaymentRequired.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task InvokeAsync_PaymentProcessingException_ReturnsPaymentRequired()
        {
            // Arrange
            PaymentProcessingException exception = new("Payment failed", "txn123", "CARD_DECLINED");

            HttpResponseMessage response = await ExecuteMiddlewareTestAsync(exception);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.PaymentRequired);

            ErrorResponse errorResponse = await DeserializeErrorResponseAsync(response);
            errorResponse.Message.Should().Be("Payment failed");
            errorResponse.ErrorCode.Should().Be("PAYMENT_PROCESSING_ERROR");
            errorResponse.Details.Should().NotBeNull();

            string? detailsJson = errorResponse.Details.ToString();
            JsonElement detailsElement = JsonSerializer.Deserialize<JsonElement>(detailsJson!);

            detailsElement.GetProperty("transactionId").GetString().Should().Be("txn123");
            detailsElement.GetProperty("providerErrorCode").GetString().Should().Be("CARD_DECLINED");
        }

        /// <summary>
        ///     Tests that ExternalServiceException returns BadGateway.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task InvokeAsync_ExternalServiceException_ReturnsBadGateway()
        {
            // Arrange
            ExternalServiceException exception = new("MapsAPI", "Service unavailable", 503, "/geocode");

            HttpResponseMessage response = await ExecuteMiddlewareTestAsync(exception);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadGateway);

            ErrorResponse errorResponse = await DeserializeErrorResponseAsync(response);
            errorResponse.Message.Should().Be("Service unavailable");
            errorResponse.ErrorCode.Should().Be("EXTERNAL_SERVICE_ERROR");
            errorResponse.Details.Should().NotBeNull();

            string? detailsJson = errorResponse.Details.ToString();
            JsonElement detailsElement = JsonSerializer.Deserialize<JsonElement>(detailsJson!);

            detailsElement.GetProperty("serviceName").GetString().Should().Be("MapsAPI");
            detailsElement.GetProperty("statusCode").GetInt32().Should().Be(503);
            detailsElement.GetProperty("endpoint").GetString().Should().Be("/geocode");
        }

        /// <summary>
        ///     Tests that generic Exception returns InternalServerError.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task InvokeAsync_GenericException_ReturnsInternalServerError()
        {
            // Arrange
            Exception exception = new("Unexpected error");

            HttpResponseMessage response = await ExecuteMiddlewareTestAsync(exception);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);

            ErrorResponse errorResponse = await DeserializeErrorResponseAsync(response);
            errorResponse.Message.Should().Be("An unexpected error occurred. Please try again later.");
            errorResponse.ErrorCode.Should().Be("INTERNAL_SERVER_ERROR");
            errorResponse.CorrelationId.Should().NotBeNullOrEmpty();
        }

        /// <summary>
        ///     Tests that correlation ID is preserved from request header.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task InvokeAsync_WithCorrelationIdInHeader_PreservesCorrelationId()
        {
            // Arrange
            string correlationId = "test-correlation-id";
            ValidationException exception = new("Validation failed");

            HttpResponseMessage response = await ExecuteMiddlewareTestAsync(exception, correlationId);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            ErrorResponse errorResponse = await DeserializeErrorResponseAsync(response);
            errorResponse.CorrelationId.Should().Be(correlationId);

            // Check response header
            response.Headers.Should().ContainKey("X-Correlation-ID");
            response.Headers.GetValues("X-Correlation-ID").FirstOrDefault().Should().Be(correlationId);
        }

        /// <summary>
        ///     Tests that correlation ID is generated when not provided.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task InvokeAsync_WithoutCorrelationId_GeneratesNewCorrelationId()
        {
            // Arrange
            BusinessRuleException exception = new("Rule violated");

            HttpResponseMessage response = await ExecuteMiddlewareTestAsync(exception);

            // Assert
            ErrorResponse errorResponse = await DeserializeErrorResponseAsync(response);
            errorResponse.CorrelationId.Should().NotBeNullOrEmpty();

            response.Headers.Should().ContainKey("X-Correlation-ID");
            response.Headers.GetValues("X-Correlation-ID").FirstOrDefault().Should().Be(errorResponse.CorrelationId);
        }

        private async Task<HttpResponseMessage> ExecuteMiddlewareTestAsync(Exception exception,
            string? correlationId = null)
        {
            using IHost host = new HostBuilder()
                .ConfigureWebHost(webBuilder => webBuilder
                    .UseTestServer()
                    .ConfigureServices(services => { services.AddSingleton(_mockLogger.Object); })
                    .Configure(app =>
                    {
                        app.UseExceptionHandling();
                        app.Run(_ => throw exception);
                    }))
                .Build();

            await host.StartAsync();

            TestServer server = host.GetTestServer();
            HttpClient client = server.CreateClient();

            HttpRequestMessage request = new(HttpMethod.Get, "/test");
            if (!string.IsNullOrEmpty(correlationId))
            {
                request.Headers.Add("X-Correlation-ID", correlationId);
            }

            return await client.SendAsync(request);
        }

        private async Task<ErrorResponse> DeserializeErrorResponseAsync(HttpResponseMessage response)
        {
            string content = await response.Content.ReadAsStringAsync();
            JsonSerializerOptions options = new()
            {
                PropertyNameCaseInsensitive = true
            };
            return JsonSerializer.Deserialize<ErrorResponse>(content, options)!;
        }
    }
}
