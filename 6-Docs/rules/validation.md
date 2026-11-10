---
id: rules/validation
title: Validation Rule
doc-type: rule
status: current
owner: unassigned
last-reviewed: 2026-11-09
---

# Validation Rule

This rule enforces input validation practices for ASP.NET Core applications in the Hotshot Logistics project, ensuring secure and consistent error handling.

## When to Apply
Apply these practices whenever implementing input validation in ASP.NET Core APIs, controllers, or services in .NET 10 projects (every project in this solution targets `net10.0`).

## Input Validation
- Validate all user inputs on both client and server sides.
- Use built-in ASP.NET Core validation attributes or FluentValidation.
- Never trust client data; always validate and sanitize inputs.
- Implement comprehensive validation for all endpoints accepting user input.

## Error Handling
- Return **ProblemDetails** consistently for all error responses.
- Use appropriate HTTP status codes (400 for validation errors, 500 for server errors).
- Include meaningful error messages without exposing sensitive information.
- Standardize error response format across all APIs.

## Validation Implementation
- Use Data Annotations for simple validation rules.
- Implement custom validation attributes for complex business rules.
- Validate inputs in controller actions or application services.
- Provide clear validation feedback to clients.

## Security Considerations
- Prevent injection attacks through proper validation.
- Validate file uploads and size limits.
- Log validation failures for monitoring.
