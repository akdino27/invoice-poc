using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using invoice_v1.src.Services;

namespace invoice_v1.src.Services
{
    // Google Drive service implementation using service account authentication.
    public class GoogleDriveService : IGoogleDriveService
    {
        private readonly DriveService? _driveService;
        private readonly ILogger<GoogleDriveService> _logger;

        public GoogleDriveService(IConfiguration configuration, ILogger<GoogleDriveService> logger)
        {
            _logger = logger;

            var keyPath = configuration["GoogleDrive:ServiceAccountKeyPath"];

            if (!string.IsNullOrEmpty(keyPath) && File.Exists(keyPath))
            {
                try
                {
                    var credential = GoogleCredential
                        .FromFile(keyPath)
                        .CreateScoped(DriveService.ScopeConstants.DriveReadonly);

                    _driveService = new DriveService(new BaseClientService.Initializer()
                    {
                        HttpClientInitializer = credential,
                        ApplicationName = "Invoice Processing System V2"
                    });

                    _logger.LogInformation("Google Drive service initialized successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to initialize Google Drive service");
                    _driveService = null;
                }
            }
            else
            {
                _logger.LogWarning(
                    "Service account key not found at: {Path}. Drive monitoring disabled.",
                    keyPath);
            }
        }

        public async Task<List<Google.Apis.Drive.v3.Data.File>> ListFilesInFolderAsync(
            string folderId,
            CancellationToken cancellationToken = default)
        {
            if (_driveService == null)
            {
                _logger.LogWarning("Drive service not initialized");
                return new List<Google.Apis.Drive.v3.Data.File>();
            }

            try
            {
                var request = _driveService.Files.List();
                request.Q = $"'{folderId}' in parents and trashed=false";
                request.Fields = "files(id, name, mimeType, size, modifiedTime, createdTime, owners)";
                request.PageSize = 1000;

                var result = await request.ExecuteAsync(cancellationToken);
                _logger.LogInformation(
                    "Listed {Count} files from folder {FolderId}",
                    result.Files.Count,
                    folderId);

                return result.Files.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing files from folder {FolderId}", folderId);
                return new List<Google.Apis.Drive.v3.Data.File>();
            }
        }
    }
}
