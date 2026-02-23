using invoice_v1.src.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace invoice_v1.src.Application.Security
{
    public class MagicBytesValidator
    {
        // PDF: %PDF
        private static readonly byte[] PdfMagic = { 0x25, 0x50, 0x44, 0x46 };

        // JPEG variants: FF D8 FF E0, FF D8 FF E1, FF D8 FF E8
        private static readonly byte[][] JpegMagics =
        {
            new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 },
            new byte[] { 0xFF, 0xD8, 0xFF, 0xE1 },
            new byte[] { 0xFF, 0xD8, 0xFF, 0xE8 }
        };

        // PNG: 89 50 4E 47 0D 0A 1A 0A
        private static readonly byte[] PngMagic = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        public void Validate(IFormFile file)
        {
            using var stream = file.OpenReadStream();
            using var reader = new BinaryReader(stream);

            var header = reader.ReadBytes(8);
            stream.Position = 0;

            var mimeType = file.ContentType?.Trim().ToLowerInvariant();

            switch (mimeType)
            {
                case "application/pdf":
                    if (!StartsWith(header, PdfMagic))
                        throw new SecurityValidationException(
                            "File content does not match PDF magic bytes (%PDF)",
                            SecurityFailReason.MagicBytesMismatch);
                    break;

                case "image/jpeg":
                    if (!JpegMagics.Any(magic => StartsWith(header, magic)))
                        throw new SecurityValidationException(
                            "File content does not match JPEG magic bytes",
                            SecurityFailReason.MagicBytesMismatch);
                    break;

                case "image/png":
                    if (!StartsWith(header, PngMagic))
                        throw new SecurityValidationException(
                            "File content does not match PNG magic bytes",
                            SecurityFailReason.MagicBytesMismatch);
                    break;

                default:
                    throw new SecurityValidationException(
                        $"Unsupported file type for magic bytes validation: {mimeType}",
                        SecurityFailReason.UnsupportedType);
            }
        }

        private static bool StartsWith(byte[] data, byte[] prefix)
        {
            if (data.Length < prefix.Length) return false;
            for (int i = 0; i < prefix.Length; i++)
            {
                if (data[i] != prefix[i]) return false;
            }
            return true;
        }
    }
}
