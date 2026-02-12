using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using invoice_v1.src.Application.DTOs;

namespace invoice_v1.src.Services
{
    // Google Drive service implementation using service account authentication.
    public class GoogleDriveService : IGoogleDriveService
    {
        private DriveService? _serviceAccountDrive;
        private DriveService? _userDrive;
        private readonly ILogger<GoogleDriveService> _logger;
        private readonly IConfiguration _configuration;


        public GoogleDriveService(IConfiguration configuration, ILogger<GoogleDriveService> logger)
        {
            _logger = logger;
            _configuration = configuration;

            var keyPath = configuration["GoogleDrive:ServiceAccountKeyPath"];

            if (!string.IsNullOrEmpty(keyPath) && File.Exists(keyPath))
            {
                try
                {
                    var credential = GoogleCredential
                        .FromFile(keyPath)
                        .CreateScoped(DriveService.ScopeConstants.Drive);

                    _serviceAccountDrive = new DriveService(new BaseClientService.Initializer()
                    {
                        HttpClientInitializer = credential,
                        ApplicationName = "Invoice Processing System V2"
                    });

                    _logger.LogInformation("Google Drive service initialized successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to initialize Google Drive service");
                    _serviceAccountDrive = null;
                }
            }
            else
            {
                _logger.LogWarning(
                    "Service account key not found at: {Path}. Drive monitoring disabled.",
                    keyPath);
            }
        }

        private async Task<DriveService> GetUserDriveAsync()
        {
            if (_userDrive != null)
                return _userDrive;

            var secrets = new ClientSecrets
            {
                ClientId = _configuration["GoogleDrive:ClientId"],
                ClientSecret = _configuration["GoogleDrive:ClientSecret"]
            };

            var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                secrets,
                new[] { DriveService.ScopeConstants.Drive },
                "personal-user",
                CancellationToken.None,
                new FileDataStore("Drive.Personal.Auth", true));

            _userDrive = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "Invoice Processing System V2"
            });

            _logger.LogInformation("Personal OAuth Drive initialized");

            return _userDrive;
        }


        public async Task<List<Google.Apis.Drive.v3.Data.File>> ListFilesInFolderAsync(
            string folderId,
            CancellationToken cancellationToken = default)
        {
            if (_serviceAccountDrive == null)
            {
                _logger.LogWarning("Drive service not initialized");
                return new List<Google.Apis.Drive.v3.Data.File>();
            }

            try
            {
                var request = _serviceAccountDrive.Files.List();
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

        private async Task<string> GetOrCreateVendorFolderAsync(
    DriveService drive,
    string parentFolderId,
    string email)
        {
            // Sanitize folder name
            var folderName = email.ToLowerInvariant().Trim();

            // Search for existing folder
            var listRequest = drive.Files.List();
            listRequest.Q =
                $"mimeType='application/vnd.google-apps.folder' " +
                $"and name='{folderName}' " +
                $"and '{parentFolderId}' in parents " +
                $"and trashed=false";

            listRequest.Fields = "files(id, name)";

            var response = await listRequest.ExecuteAsync();

            var existing = response.Files?.FirstOrDefault();

            if (existing != null)
            {
                _logger.LogInformation(
                    "Vendor folder already exists for {Email}: {FolderId}",
                    email,
                    existing.Id);

                return existing.Id;
            }

            // Create folder
            var fileMetadata = new Google.Apis.Drive.v3.Data.File
            {
                Name = folderName,
                MimeType = "application/vnd.google-apps.folder",
                Parents = new[] { parentFolderId }
            };

            var createRequest = drive.Files.Create(fileMetadata);
            createRequest.Fields = "id";

            var folder = await createRequest.ExecuteAsync();

            _logger.LogInformation(
                "Created new vendor folder for {Email}: {FolderId}",
                email,
                folder.Id);

            return folder.Id;
        }


        public async Task<DriveFileResult> UploadFileAsync(
        string fileName,
        string mimeType,
        Stream stream,
        string email)
        {
            var drive = await GetUserDriveAsync();

            var rootFolderId = _configuration["GoogleDrive:SharedFolderId"];

            var folderId = await GetOrCreateVendorFolderAsync(
                drive,
                rootFolderId!,
                email);


            if (string.IsNullOrWhiteSpace(folderId))
                throw new Exception("GoogleDrive:SharedFolderId is missing in configuration");

            if (stream.CanSeek)
                stream.Position = 0;

            try
            {
                var fileMetadata = new Google.Apis.Drive.v3.Data.File
                {
                    Name = fileName,
                    Parents = new[] { folderId }
                };

                var request = drive.Files.Create(
                    fileMetadata,
                    stream,
                    mimeType);

                request.Fields = "id, name, webViewLink";

                var uploadResult = await request.UploadAsync();

                if (uploadResult.Status != Google.Apis.Upload.UploadStatus.Completed)
                {
                    var error = uploadResult.Exception?.Message ?? "Unknown Google Drive error";
                    throw new Exception($"Drive upload failed: {error}");
                }

                var uploadedFile = request.ResponseBody;

                if (uploadedFile == null)
                    throw new Exception("Drive upload failed: ResponseBody is null");

                _logger.LogInformation(
                    "Uploaded file {FileName} to Google Drive with ID {DriveId}",
                    fileName,
                    uploadedFile.Id);

                return new DriveFileResult
                {
                    Id = uploadedFile.Id,
                    Name = uploadedFile.Name,
                    WebViewLink = uploadedFile.WebViewLink
                };
            }
            catch (Google.GoogleApiException gex)
            {
                _logger.LogError(gex,
                    "Google API error while uploading file. Reason: {Reason}",
                    gex.Error?.Message);

                throw new Exception($"Google Drive API Error: {gex.Error?.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file {FileName} to Drive", fileName);
                throw;
            }
        }




    }
}
