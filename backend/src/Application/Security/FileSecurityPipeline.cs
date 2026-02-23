using invoice_v1.src.Application.Interfaces;
using invoice_v1.src.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace invoice_v1.src.Application.Security
{
    public class FileSecurityPipeline : IFileSecurityPipeline
    {
        private readonly FileTypeValidator _fileTypeValidator;
        private readonly MagicBytesValidator _magicBytesValidator;
        private readonly TokenCountValidator _tokenCountValidator;
        private readonly VirusTotalScanner _virusTotalScanner;
        private readonly ILogger<FileSecurityPipeline> _logger;

        public FileSecurityPipeline(
        FileTypeValidator fileTypeValidator,
        MagicBytesValidator magicBytesValidator,
        TokenCountValidator tokenCountValidator,
        VirusTotalScanner virusTotalScanner,
        ILogger<FileSecurityPipeline> logger)
        {
            _fileTypeValidator = fileTypeValidator;
            _magicBytesValidator = magicBytesValidator;
            _tokenCountValidator = tokenCountValidator;
            _virusTotalScanner = virusTotalScanner;
            _logger = logger;
        }

        public async Task<SecurityPipelineResult> RunAsync(IFormFile file, Guid vendorId)
        {
            // ── Layer 1: File type validation ──
            try
            {
                _fileTypeValidator.Validate(file);
            }
            catch (SecurityValidationException ex)
            {
                _logger.LogWarning(
                "SECURITY [Layer 1 - FileType]: Rejected | Vendor: {VendorId} | File: {FileName} | Reason: {Reason}",
                vendorId, file.FileName, ex.Message);
                return Fail(ex.Message, ex.FailCode);
            }

            // ── Layer 2: Magic bytes validation ──
            try
            {
                _magicBytesValidator.Validate(file);
            }
            catch (SecurityValidationException ex)
            {
                _logger.LogWarning(
                "SECURITY [Layer 2 - MagicBytes]: Rejected | Vendor: {VendorId} | File: {FileName} | Reason: {Reason}",
                vendorId, file.FileName, ex.Message);
                return Fail(ex.Message, ex.FailCode);
            }

            // ── Layer 3: Token count validation ──
            try
            {
                await _tokenCountValidator.ValidateAsync(file);
            }
            catch (SecurityValidationException ex)
            {
                _logger.LogWarning(
                "SECURITY [Layer 3 - TokenCount]: Rejected | Vendor: {VendorId} | File: {FileName} | Reason: {Reason}",
                vendorId, file.FileName, ex.Message);
                return Fail(ex.Message, ex.FailCode);
            }

            // ── Layer 4: VirusTotal hash-only scan ──
            try
            {
                using var scanStream = file.OpenReadStream();
                var vtResult = await _virusTotalScanner.ScanAsync(scanStream, file.FileName);

                if (!vtResult.IsClean)
                {
                    _logger.LogWarning(
                    "SECURITY [Layer 4 - VirusTotal]: Malicious file detected | Vendor: {VendorId} | File: {FileName} | Result: {Result}",
                    vendorId, file.FileName, vtResult.Message);
                    return Fail(
                    $"File failed antivirus scan. {vtResult.Message}",
                    SecurityFailReason.MalwareDetected);
                }
            }
            catch (SecurityValidationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // Fail open on VirusTotal API errors — do not block legitimate uploads
                _logger.LogWarning(ex,
                "SECURITY [Layer 4 - VirusTotal]: API error for {FileName}. Failing open",
                file.FileName);
            }

            // ── All layers passed ──
            _logger.LogInformation(
            "SECURITY: File {FileName} from vendor {VendorId} passed all security layers",
            file.FileName, vendorId);

            return new SecurityPipelineResult { IsHealthy = true };
        }

        private static SecurityPipelineResult Fail(string reason, SecurityFailReason failCode)
        {
            return new SecurityPipelineResult
            {
                IsHealthy = false,
                FailReason = reason,
                FailCode = failCode
            };
        }
    }
}