using Microsoft.AspNetCore.Mvc;
using VaccineTracker.Application.Exceptions;

public sealed class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            // create error response
            var (statusCode, title, detail) = exception switch
            {
                ValidationException =>
                    (StatusCodes.Status400BadRequest, "Validation Error", exception.Message),

                ForbiddenException =>
                    (StatusCodes.Status403Forbidden, "Forbidden", exception.Message),

                NotFoundException =>
                    (StatusCodes.Status404NotFound, "Resource Not Found", exception.Message),

                ConflictException =>
                    (StatusCodes.Status409Conflict, "Conflict", exception.Message),

                BusinessRuleException =>
                    (StatusCodes.Status422UnprocessableEntity, "Business Rule Violation", exception.Message),

                _ =>
                    (StatusCodes.Status500InternalServerError,
                     "An unexpected error occurred",
                     "Please try again later.")
            };

            var problem = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail
            };

            if (exception is ValidationException
                or ForbiddenException
                or NotFoundException
                or ConflictException
                or BusinessRuleException)
            {
                _logger.LogWarning(exception, "Request failed: {Message}", exception.Message);
            }
            else
            {
                _logger.LogError(exception, "Unhandled exception occurred.");
            }
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsJsonAsync(problem);
        }
    }
}