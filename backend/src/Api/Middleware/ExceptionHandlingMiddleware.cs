using System.Net;
using System.Text.Json;

namespace invoice_v1.src.Api.Middleware
{
    /// <summary>
    /// Global exception handling middleware.
    /// Catches unhandled exceptions and returns structured error responses.
    /// </summary>
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
                _logger.LogError(ex, "Unhandled exception occurred: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var response = new ErrorResponse
            {
                Message = "An error occurred processing your request",
                TraceId = context.TraceIdentifier
            };

            switch (exception)
            {
                case ArgumentNullException:
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.Message = exception.Message;
                    break;

                case ArgumentException:
                case InvalidOperationException:
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.Message = exception.Message;
                    break;

                case UnauthorizedAccessException:
                    // FIXED: Changed from 401 to 403
                    // 401 = Not authenticated (missing/invalid credentials)
                    // 403 = Authenticated but not authorized (insufficient permissions)
                    context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                    response.Message = exception.Message;
                    break;

                default:
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    if (_environment.IsDevelopment())
                    {
                        response.Message = exception.Message;
                        response.Details = exception.StackTrace;
                    }
                    break;
            }

            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(json);
        }

        private class ErrorResponse
        {
            public string Message { get; set; } = string.Empty;
            public string TraceId { get; set; } = string.Empty;
            public string? Details { get; set; }
        }
    }
}
