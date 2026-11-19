// <copyright file="DomainExceptionHandler.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using HotshotLogistics.Core.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace HotshotLogistics.Api.Errors
{
    /// <summary>
    ///     Translates the domain exception types into RFC 9457 problem responses. Exceptions it
    ///     does not name fall through to the framework's handler, which answers 500 without leaking
    ///     the message or stack trace.
    /// </summary>
    public sealed class DomainExceptionHandler : IExceptionHandler
    {
        private readonly IProblemDetailsService _problemDetailsService;

        /// <summary>
        ///     Initializes a new instance of the <see cref="DomainExceptionHandler" /> class.
        /// </summary>
        /// <param name="problemDetailsService">The service that writes the problem body.</param>
        public DomainExceptionHandler(IProblemDetailsService problemDetailsService)
        {
            _problemDetailsService = problemDetailsService ?? throw new ArgumentNullException(nameof(problemDetailsService));
        }

        /// <inheritdoc />
        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            ProblemDetails? problem = CreateProblem(exception);
            if (problem is null)
            {
                return false;
            }

            httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
            return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = problem,
                Exception = exception
            });
        }

        private static ProblemDetails? CreateProblem(Exception exception)
        {
            return exception switch
            {
                ValidationException validation => new ValidationProblemDetails(
                    validation.Errors.ToDictionary(pair => pair.Key, pair => pair.Value))
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "One or more validation errors occurred.",
                    Detail = validation.Message,
                    Type = TypeFor(StatusCodes.Status400BadRequest)
                },
                BusinessRuleException rule => Create(StatusCodes.Status422UnprocessableEntity, rule.Message),
                PaymentProcessingException payment => WithExtensions(
                    Create(StatusCodes.Status402PaymentRequired, payment.Message),
                    ("transactionId", payment.TransactionId),
                    ("providerErrorCode", payment.ProviderErrorCode)),
                ExternalServiceException external => WithExtensions(
                    Create(StatusCodes.Status502BadGateway, "A service this request depends on failed."),
                    ("serviceName", external.ServiceName)),
                _ => null
            };
        }

        private static ProblemDetails Create(int status, string detail)
        {
            return new ProblemDetails
            {
                Status = status,
                Title = ReasonPhrases.GetReasonPhrase(status),
                Detail = detail,
                Type = TypeFor(status)
            };
        }

        private static ProblemDetails WithExtensions(
            ProblemDetails problem,
            params (string Key, object? Value)[] extensions)
        {
            foreach ((string key, object? value) in extensions)
            {
                if (value is not null)
                {
                    problem.Extensions[key] = value;
                }
            }

            return problem;
        }

        private static string TypeFor(int status)
        {
            return status switch
            {
                StatusCodes.Status400BadRequest => "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                StatusCodes.Status402PaymentRequired => "https://tools.ietf.org/html/rfc9110#section-15.5.3",
                StatusCodes.Status422UnprocessableEntity => "https://tools.ietf.org/html/rfc9110#section-15.5.21",
                StatusCodes.Status502BadGateway => "https://tools.ietf.org/html/rfc9110#section-15.6.3",
                _ => "https://tools.ietf.org/html/rfc9110#section-15.6.1"
            };
        }
    }
}
