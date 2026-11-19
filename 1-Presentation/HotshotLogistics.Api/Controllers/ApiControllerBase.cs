// <copyright file="ApiControllerBase.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebUtilities;

namespace HotshotLogistics.Api.Controllers
{
    /// <summary>
    ///     Base class for API controllers. Its helpers return the same
    ///     <see cref="ProblemDetails" /> body the exception handler writes, so every error path
    ///     answers with one contract while the result types stay the familiar MVC ones.
    /// </summary>
    public abstract class ApiControllerBase : ControllerBase
    {
        /// <summary>
        ///     Creates a 400 response with a problem body.
        /// </summary>
        /// <param name="detail">What was wrong with the request.</param>
        /// <returns>The result.</returns>
        protected BadRequestObjectResult BadRequestProblem(string detail)
        {
            return BadRequest(CreateProblem(StatusCodes.Status400BadRequest, detail));
        }

        /// <summary>
        ///     Creates a 401 response with a problem body.
        /// </summary>
        /// <param name="detail">Why the caller was not authenticated.</param>
        /// <returns>The result.</returns>
        protected UnauthorizedObjectResult UnauthorizedProblem(string detail)
        {
            return Unauthorized(CreateProblem(StatusCodes.Status401Unauthorized, detail));
        }

        /// <summary>
        ///     Creates a 404 response with a problem body.
        /// </summary>
        /// <param name="detail">What could not be found.</param>
        /// <returns>The result.</returns>
        protected NotFoundObjectResult NotFoundProblem(string detail)
        {
            return NotFound(CreateProblem(StatusCodes.Status404NotFound, detail));
        }

        /// <summary>
        ///     Creates a 409 response with a problem body.
        /// </summary>
        /// <param name="detail">Why the request conflicts with the current state.</param>
        /// <returns>The result.</returns>
        protected ConflictObjectResult ConflictProblem(string detail)
        {
            return Conflict(CreateProblem(StatusCodes.Status409Conflict, detail));
        }

        /// <summary>
        ///     Creates a 400 response with a validation problem body listing each field's errors.
        /// </summary>
        /// <param name="errors">The error messages by field name.</param>
        /// <returns>The result.</returns>
        protected BadRequestObjectResult ValidationFailed(IReadOnlyDictionary<string, string[]> errors)
        {
            ModelStateDictionary modelState = new();
            foreach ((string field, string[] messages) in errors)
            {
                foreach (string message in messages)
                {
                    modelState.AddModelError(field, message);
                }
            }

            ProblemDetailsFactory? factory = HttpContext?.RequestServices.GetService<ProblemDetailsFactory>();
            ValidationProblemDetails details = factory is not null
                ? factory.CreateValidationProblemDetails(HttpContext!, modelState, StatusCodes.Status400BadRequest)
                : new ValidationProblemDetails(modelState)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "One or more validation errors occurred."
                };
            return BadRequest(details);
        }

        /// <summary>
        ///     Creates a 400 response with a validation problem body from a failed FluentValidation result.
        /// </summary>
        /// <param name="result">The failed validation result.</param>
        /// <returns>The result.</returns>
        protected BadRequestObjectResult ValidationFailed(ValidationResult result)
        {
            return ValidationFailed(result.Errors
                .GroupBy(failure => failure.PropertyName)
                .ToDictionary(group => group.Key, group => group.Select(failure => failure.ErrorMessage).ToArray()));
        }

        private ProblemDetails CreateProblem(int status, string detail)
        {
            ProblemDetailsFactory? factory = HttpContext?.RequestServices.GetService<ProblemDetailsFactory>();
            return factory is not null
                ? factory.CreateProblemDetails(HttpContext!, status, detail: detail)
                : new ProblemDetails
                {
                    Status = status,
                    Title = ReasonPhrases.GetReasonPhrase(status),
                    Detail = detail
                };
        }
    }
}
