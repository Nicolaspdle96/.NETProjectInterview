using System.Diagnostics;
using System.Text.Json;
using JournalApp.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace JournalApp.Api.Middleware;

/// <summary>
/// Translates exceptions thrown by the application into <see cref="ProblemDetails"/> responses.
/// </summary>
public class ExceptionHandlingMiddleware(
    RequestDelegate next,
    IOptions<JsonOptions> jsonOptions,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception) when (!context.Response.HasStarted)
        {
            var problem = MapToProblem(exception);
            if (problem.Status == StatusCodes.Status500InternalServerError)
            {
                logger.LogError(exception, "Unhandled exception while processing {Method} {Path}",
                    context.Request.Method, context.Request.Path);
            }

            problem.Instance = context.Request.Path;
            problem.Extensions["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier;

            context.Response.StatusCode = problem.Status!.Value;
            context.Response.ContentType = "application/problem+json";

            // Serialize with the runtime type so validation errors are included.
            await JsonSerializer.SerializeAsync(
                context.Response.Body, problem, problem.GetType(), jsonOptions.Value.JsonSerializerOptions,
                context.RequestAborted);
        }
    }

    private static ProblemDetails MapToProblem(Exception exception) => exception switch
    {
        DomainValidationException validation => new ValidationProblemDetails(
            validation.Errors.ToDictionary(e => JsonNamingPolicy.CamelCase.ConvertName(e.Key), e => e.Value))
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "One or more validation errors occurred."
        },
        NotFoundException => Problem(StatusCodes.Status404NotFound, "Resource not found.", exception.Message),
        ConflictException => Problem(StatusCodes.Status409Conflict, "Conflict.", exception.Message),
        UnauthorizedException => Problem(StatusCodes.Status401Unauthorized, "Unauthorized.", exception.Message),
        _ => Problem(StatusCodes.Status500InternalServerError, "An unexpected error occurred.", null)
    };

    private static ProblemDetails Problem(int status, string title, string? detail) =>
        new() { Status = status, Title = title, Detail = detail };
}
