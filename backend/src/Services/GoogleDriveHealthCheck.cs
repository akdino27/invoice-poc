using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace invoice_v1.src.Services
{
    /// <summary>
    /// Health check for Google Drive service connectivity.
    /// Tests if the service account can access the configured shared folder.
    /// </summary>
    public class GoogleDriveHealthCheck : IHealthCheck
    {
        private readonly IGoogleDriveService _driveService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GoogleDriveHealthCheck> _logger;

        public GoogleDriveHealthCheck(
            IGoogleDriveService driveService,
            IConfiguration configuration,
            ILogger<GoogleDriveHealthCheck> logger)
        {
            _driveService = driveService;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Get the configured shared folder ID
                var sharedFolderId = _configuration["GoogleDrive:SharedFolderId"];

                if (string.IsNullOrWhiteSpace(sharedFolderId))
                {
                    return HealthCheckResult.Degraded(
                        "Google Drive SharedFolderId not configured in appsettings.json");
                }

                // CHANGED: Updated to use the new recursive method name
                // This is a lightweight check just to ensure connectivity and permissions work
                var files = await _driveService.ListAllFilesRecursivelyAsync(
                    sharedFolderId,
                    cancellationToken);

                // If we got here, the service account has access
                return HealthCheckResult.Healthy(
                    $"Google Drive accessible. Found {files.Count} files in root hierarchy.");
            }
            catch (Google.GoogleApiException gex)
            {
                _logger.LogError(
                    gex,
                    "Google Drive API error during health check. Error: {Error}",
                    gex.Error?.Message);

                return HealthCheckResult.Unhealthy(
                    $"Google Drive API error: {gex.Error?.Message ?? "Unknown error"}",
                    gex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Google Drive health check failed");

                return HealthCheckResult.Unhealthy(
                    "Google Drive service is not accessible. Check service account configuration.",
                    ex);
            }
        }
    }
}