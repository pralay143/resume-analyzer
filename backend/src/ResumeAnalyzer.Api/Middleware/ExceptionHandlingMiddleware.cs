using Microsoft.AspNetCore.Mvc;
using ResumeAnalyzer.Api.Services;

namespace ResumeAnalyzer.Api.Middleware;

/// <summary>
/// Converts unhandled exceptions into ProblemDetails responses. Validation errors become 400 with their
/// message; anything unexpected becomes a generic 500 so stack traces and internals never reach the client.
/// </summary>
public class ExceptionHandlingMiddleware(
    RequestDelegate next,
    IProblemDetailsService problemDetailsService,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // The client went away; there's nobody to send a response to.
        }
        catch (Exception ex)
        {
            if (context.Response.HasStarted)
            {
                logger.LogError(ex, "Unhandled exception after the response started");
                throw;
            }

            var problem = ToProblemDetails(ex);
            if (problem.Status >= StatusCodes.Status500InternalServerError)
            {
                logger.LogError(ex, "Unhandled exception processing {Method} {Path}",
                    context.Request.Method, context.Request.Path);
            }
            else
            {
                logger.LogInformation("Request rejected with {StatusCode}: {Message}", problem.Status, ex.Message);
            }

            context.Response.Clear();
            context.Response.StatusCode = problem.Status!.Value;

            await problemDetailsService.WriteAsync(new ProblemDetailsContext
            {
                HttpContext = context,
                ProblemDetails = problem,
                Exception = ex
            });
        }
    }

    private static ProblemDetails ToProblemDetails(Exception ex) => ex switch
    {
        ResumeParseException => new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Invalid resume file",
            Detail = ex.Message
        },
        // Raised by Kestrel, e.g. when the request body exceeds the size limit (413).
        BadHttpRequestException badRequest => new ProblemDetails
        {
            Status = badRequest.StatusCode,
            Title = badRequest.StatusCode == StatusCodes.Status413PayloadTooLarge
                ? "Request too large"
                : "Bad request",
            Detail = badRequest.StatusCode == StatusCodes.Status413PayloadTooLarge
                ? "The upload is too large. Resumes can be at most 5 MB."
                : "The request could not be read."
        },
        _ => new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred",
            Detail = "Something went wrong on our side. Please try again later."
        }
    };
}
