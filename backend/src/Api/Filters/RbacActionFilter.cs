using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace invoice_v1.src.Api.Filters
{
    /// <summary>
    /// Action filter that extracts and validates user context (email and role) from request headers.
    /// Sets HttpContext.Items["UserEmail"] and HttpContext.Items["IsAdmin"] for use in controllers.
    /// </summary>
    public class RbacActionFilter : IActionFilter
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<RbacActionFilter> _logger;

        public RbacActionFilter(IConfiguration configuration, ILogger<RbacActionFilter> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            // Extract user email from header
            if (!context.HttpContext.Request.Headers.TryGetValue("X-User-Email", out var userEmailHeader))
            {
                _logger.LogWarning("Missing X-User-Email header");
                context.Result = new UnauthorizedObjectResult(new
                {
                    error = "User email not found in request. Authentication required."
                });
                return;
            }

            var userEmail = userEmailHeader.ToString();

            if (string.IsNullOrWhiteSpace(userEmail))
            {
                _logger.LogWarning("Empty X-User-Email header");
                context.Result = new UnauthorizedObjectResult(new
                {
                    error = "User email is empty. Authentication required."
                });
                return;
            }

            // Check if user is admin
            var isAdmin = false;
            if (context.HttpContext.Request.Headers.TryGetValue("X-User-Role", out var roleHeader))
            {
                isAdmin = roleHeader.ToString().Equals("admin", StringComparison.OrdinalIgnoreCase);
            }

            // Alternative: Check against admin emails from configuration
            var adminEmails = _configuration.GetSection("Security:AdminEmails").Get<List<string>>() ?? new List<string>();
            if (adminEmails.Contains(userEmail, StringComparer.OrdinalIgnoreCase))
            {
                isAdmin = true;
            }

            // Store in HttpContext for controller access
            context.HttpContext.Items["UserEmail"] = userEmail;
            context.HttpContext.Items["IsAdmin"] = isAdmin;

            _logger.LogDebug("RBAC filter: Email={UserEmail}, IsAdmin={IsAdmin}", userEmail, isAdmin);
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // No action needed after execution
        }
    }

    /// <summary>
    /// Extension methods for easy access to user context from controllers.
    /// </summary>
    public static class RbacExtensions
    {
        public static (string userEmail, bool isAdmin) GetUserContext(this ControllerBase controller)
        {
            var userEmail = controller.HttpContext.Items["UserEmail"] as string
                ?? throw new UnauthorizedAccessException("User context not found");
            var isAdmin = controller.HttpContext.Items["IsAdmin"] as bool? ?? false;

            return (userEmail, isAdmin);
        }
    }
}
