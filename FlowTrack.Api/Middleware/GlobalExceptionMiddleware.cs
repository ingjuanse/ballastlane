using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json;
using FlowTrack.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FlowTrack.Api.Middleware;

public sealed class GlobalExceptionMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title, detail) = exception switch
        {
            ValidationException validationException =>
                ((int)HttpStatusCode.BadRequest, "Validation failed.", validationException.Message),
            DomainRuleViolationException domainException =>
                ((int)HttpStatusCode.BadRequest, "Validation failed.", domainException.Message),
            UnauthorizedAccessException =>
                ((int)HttpStatusCode.Unauthorized, "Unauthorized.", "Authentication is required to access this resource."),
            _ => ((int)HttpStatusCode.InternalServerError, "An unexpected error occurred.", "An unexpected error occurred while processing the request.")
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception while processing {Method} {Path}.", context.Request.Method, context.Request.Path);
        }
        else
        {
            logger.LogWarning(exception, "Request failed with status code {StatusCode} for {Method} {Path}.", statusCode, context.Request.Method, context.Request.Path);
        }

        if (context.Response.HasStarted)
        {
            logger.LogWarning("The response has already started; the exception response cannot be written.");
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };
        problemDetails.Extensions["traceId"] = context.TraceIdentifier;

        await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails));
    }
}
