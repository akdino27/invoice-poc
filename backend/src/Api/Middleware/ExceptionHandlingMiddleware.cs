using System.Net;
using System.Text.Json;
using invoice_v1.src.Application.Exceptions;

namespace invoice_v1.src.Api.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger)
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
            catch (RateLimitExceededException ex)
            {
                _logger.LogWarning(ex, "Rate limit exceeded");
                await HandleExceptionAsync(context, ex, HttpStatusCode.TooManyRequests, ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt");
                await HandleExceptionAsync(context, ex, HttpStatusCode.Unauthorized, ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation");
                await HandleExceptionAsync(context, ex, HttpStatusCode.BadRequest, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception occurred");
                await HandleExceptionAsync(
                    context,
                    ex,
                    HttpStatusCode.InternalServerError,
                    "An internal error occurred. Please contact support.");
            }
        }

        private static Task HandleExceptionAsync(
            HttpContext context,
            Exception exception,
            HttpStatusCode statusCode,
            string message)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            var response = new
            {
                error = message,
                statusCode = (int)statusCode,
                timestamp = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(response);
            return context.Response.WriteAsync(json);
        }
    }
}
