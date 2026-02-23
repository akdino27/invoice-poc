using invoice_v1.src.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace invoice_v1.src.Application.Security
{
    public class FileTypeValidator
    {
        private static readonly Dictionary<string, HashSet<string>> AllowedMimeExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            { "application/pdf", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".pdf" } },
            { "image/jpeg", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg" } },
            { "image/png", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".png" } }
        };

        public void Validate(IFormFile file)
        {
            var mimeType = file.ContentType?.Trim();
            if (string.IsNullOrEmpty(mimeType) || !AllowedMimeExtensions.ContainsKey(mimeType))
            {
                throw new SecurityValidationException(
                    $"MIME type '{mimeType}' is not allowed. Allowed: application/pdf, image/jpeg, image/png",
                    SecurityFailReason.InvalidMimeType);
            }

            var extension = Path.GetExtension(file.FileName)?.Trim();
            if (string.IsNullOrEmpty(extension) || !AllowedMimeExtensions[mimeType].Contains(extension))
            {
                throw new SecurityValidationException(
                    $"File extension '{extension}' does not match MIME type '{mimeType}'",
                    SecurityFailReason.MimeExtensionMismatch);
            }
        }
    }
}
