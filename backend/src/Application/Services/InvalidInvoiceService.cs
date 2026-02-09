using invoice_v1.src.Application.DTOs;
using invoice_v1.src.Application.Interfaces;
using invoice_v1.src.Domain.Entities;
using invoice_v1.src.Infrastructure.Repositories;
using System.Text.Json;

namespace invoice_v1.src.Application.Services
{
    public class InvalidInvoiceService : IInvalidInvoiceService
    {
        private readonly IInvalidInvoiceRepository invalidInvoiceRepository;
        private readonly IJobRepository jobRepository;
        private readonly ILogger<InvalidInvoiceService> logger;

        public InvalidInvoiceService(
            IInvalidInvoiceRepository invalidInvoiceRepository,
            IJobRepository jobRepository,
            ILogger<InvalidInvoiceService> logger)
        {
            this.invalidInvoiceRepository = invalidInvoiceRepository;
            this.jobRepository = jobRepository;
            this.logger = logger;
        }

        public async Task CreateInvalidInvoiceFromJobAsync(Guid jobId, string reason)
        {
            var job = await jobRepository.GetByIdAsync(jobId);
            if (job == null)
            {
                logger.LogWarning("Job {JobId} not found when creating invalid invoice", jobId);
                return;
            }

            string? fileId = null;
            string? fileName = null;

            // FIX: Use PayloadJson instead of Payload
            if (!string.IsNullOrWhiteSpace(job.PayloadJson))
            {
                try
                {
                    var payloadObj = JsonSerializer.Deserialize<JsonElement>(job.PayloadJson);

                    fileId = payloadObj.TryGetProperty("fileId", out var fid) ? fid.GetString() : null;
                    fileName = payloadObj.TryGetProperty("originalName", out var fn) ? fn.GetString() : null;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error parsing job payload for invalid invoice");
                }
            }

            var invalidInvoice = new InvalidInvoice
            {
                Id = Guid.NewGuid(),
                FileId = fileId,
                FileName = fileName ?? "Unknown",
                Reason = reason,
                CreatedAt = DateTime.UtcNow
            };

            await invalidInvoiceRepository.CreateAsync(invalidInvoice);

            logger.LogInformation("Created invalid invoice entry for job {JobId}, file {FileId}",
                jobId, fileId);
        }

        public async Task<List<InvalidInvoiceDto>> GetAllAsync(int skip, int take)
        {
            var invalidInvoices = await invalidInvoiceRepository.GetAllAsync(skip, take);

            return invalidInvoices.Select(i => new InvalidInvoiceDto
            {
                Id = i.Id,
                FileId = i.FileId,
                FileName = i.FileName,
                Reason = i.Reason,
                CreatedAt = i.CreatedAt
            }).ToList();
        }

        public async Task<int> GetCountAsync()
        {
            return await invalidInvoiceRepository.GetCountAsync();
        }
    }
}
