namespace invoice_v1.src.Services
{
    // Interface for Google Drive operations.
    public interface IGoogleDriveService
    {
        Task<List<Google.Apis.Drive.v3.Data.File>> ListFilesInFolderAsync(
            string folderId,
            CancellationToken cancellationToken = default);
    }
}
