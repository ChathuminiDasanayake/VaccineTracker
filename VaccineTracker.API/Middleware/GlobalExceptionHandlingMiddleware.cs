using Microsoft.AspNetCore.Mvc;

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
            // log exception
            _logger.LogError(exception, "Unhandled exception occurred.");
            // create error response
            var (statusCode, title, detail) = exception switch
            {
                ArgumentException =>
                    (StatusCodes.Status400BadRequest,
                     "Bad Request",
                     exception.Message),

                KeyNotFoundException =>
                    (StatusCodes.Status404NotFound,
                     "Resource Not Found",
                     exception.Message),

                _ =>
                    (StatusCodes.Status500InternalServerError,
                     "An unexpected error occurred.",
                     "Please try again later.")
            };

            var problem = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail
            };

            _logger.LogError(exception, "Unhandled exception occurred.");

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsJsonAsync(problem);
        }
    }
}