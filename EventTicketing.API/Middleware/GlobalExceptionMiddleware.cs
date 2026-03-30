using System.Net;
using System.Text.Json;
using EventTicketing.Infrastructure.Exceptions;

namespace EventTicketing.API.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message) = exception switch
        {
            EventNotFoundException ex => (HttpStatusCode.NotFound, ex.Message),
            InsufficientTicketsException ex => (HttpStatusCode.BadRequest, ex.Message),
            EventDeletionNotAllowedException ex => (HttpStatusCode.BadRequest, ex.Message),
            EarlyBirdExpiredException ex => (HttpStatusCode.BadRequest, ex.Message),
            KeyNotFoundException ex => (HttpStatusCode.NotFound, ex.Message),
            InvalidEventOperationException ex => (HttpStatusCode.BadRequest, ex.Message),
            EventNotActiveException ex => (HttpStatusCode.BadRequest, ex.Message),
            TicketOperationException ex => (HttpStatusCode.BadRequest, ex.Message),
            InvalidOperationException ex => (HttpStatusCode.BadRequest, ex.Message),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred.")
        };

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var body = JsonSerializer.Serialize(new
        {
            error = message,
            statusCode = (int)statusCode,
            timestamp = DateTime.UtcNow
        });

        return context.Response.WriteAsync(body);
    }
}