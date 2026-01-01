using DataDictionary.AspNetCore.Core.Exceptions;
using DataDictionary.AspNetCore.Core.Helpers;
using System.Text.Json;

namespace NetSqlDataDicV2.Web.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = Guid.NewGuid().ToString("N")[..8];

        _logger.LogError(exception,
            "Unhandled exception. CorrelationId: {CorrelationId}, Path: {Path}",
            correlationId, context.Request.Path);

        // Determine response based on exception type
        var (statusCode, message) = exception switch
        {
            DllLoadException dle => (400, GetDllLoadErrorMessage(dle)),
            DbContextCreationException dce => (400, dce.Message),
            DependencyResolutionException dre => (400, dre.Message),
            EfModelSourceException ese => (400, ese.Message),
            ArgumentException ae => (400, ae.Message),
            InvalidOperationException ioe => (400, ioe.Message),
            _ => (500, ErrorMessages.UnexpectedError())
        };

        // For API requests, return JSON
        if (context.Request.Path.StartsWithSegments("/api") ||
            context.Request.Headers.Accept.ToString().Contains("application/json"))
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;

            var response = new
            {
                error = message,
                correlationId,
                // Don't include stack trace in production
                details = _environment.IsDevelopment() ? exception.ToString() : null
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
        else
        {
            // For page requests, redirect to error page
            context.Response.Redirect($"/Home/Error?message={Uri.EscapeDataString(message)}&correlationId={correlationId}");
        }
    }

    private static string GetDllLoadErrorMessage(DllLoadException ex)
    {
        return ex.ErrorType switch
        {
            DllLoadErrorType.FileNotFound => ErrorMessages.DllNotFound(ex.DllPath),
            DllLoadErrorType.InvalidAssembly => ErrorMessages.InvalidDll(ex.DllPath),
            DllLoadErrorType.SecurityViolation => ErrorMessages.DllSecurityViolation(),
            _ => ex.Message
        };
    }
}
