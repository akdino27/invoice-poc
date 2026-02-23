using invoice_v1.src.Application.DTOs;

namespace invoice_v1.src.Services
{
    public interface IGoogleDriveService
    {

        Task<List<Google.Apis.Drive.v3.Data.File>> ListAllFilesRecursivelyAsync(
            string rootFolderId,
            CancellationToken cancellationToken = default);

        Task<DriveFileResult> UploadFileAsync(
            string fileName,
            string mimeType,
            Stream stream,
            string email);

        Task DeleteFileAsync(string fileId);
    }
}
