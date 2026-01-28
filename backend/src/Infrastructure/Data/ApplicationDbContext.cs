using invoice_v1.src.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace invoice_v1.src.Infrastructure.Data
{
    // Manages all entities and relationships
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSets
        public DbSet<FileChangeLog> FileChangeLogs { get; set; } = null!;
        public DbSet<JobQueue> JobQueues { get; set; } = null!;
        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<Invoice> Invoices { get; set; } = null!;
        public DbSet<InvoiceLine> InvoiceLines { get; set; } = null!;
        public DbSet<InvalidInvoice> InvalidInvoices { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ConfigureFileChangeLog(modelBuilder);
            ConfigureJobQueue(modelBuilder);
            ConfigureProduct(modelBuilder);
            ConfigureInvoice(modelBuilder);
            ConfigureInvoiceLine(modelBuilder);
            ConfigureInvalidInvoice(modelBuilder);
        }

        private static void ConfigureFileChangeLog(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FileChangeLog>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.ChangeType)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.DetectedAt)
                    .IsRequired();

                // Index for polling unprocessed logs
                entity.HasIndex(e => new { e.Processed, e.DetectedAt })
                    .HasDatabaseName("IX_FileChangeLogs_Processed_DetectedAt");

                // Index for FileId lookups
                entity.HasIndex(e => e.FileId)
                    .HasDatabaseName("IX_FileChangeLogs_FileId");

                entity.HasIndex(e => e.ChangeType)
                    .HasDatabaseName("IX_FileChangeLogs_ChangeType");
            });
        }

        private static void ConfigureJobQueue(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<JobQueue>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.JobType)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.PayloadJson)
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                // Critical index for worker polling
                entity.HasIndex(e => new { e.Status, e.NextRetryAt })
                    .HasDatabaseName("IX_JobQueues_Status_NextRetryAt");

                // Index for job lookup by status
                entity.HasIndex(e => e.Status)
                    .HasDatabaseName("IX_JobQueues_Status");

                // Index for locked jobs monitoring
                entity.HasIndex(e => new { e.LockedBy, e.LockedAt })
                    .HasDatabaseName("IX_JobQueues_LockedBy_LockedAt");

                // Index for job history queries
                entity.HasIndex(e => e.CreatedAt)
                    .HasDatabaseName("IX_JobQueues_CreatedAt");
            });
        }

        private static void ConfigureProduct(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.ProductId)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.ProductName)
                    .IsRequired()
                    .HasMaxLength(500);

                // Unique constraint on business ProductId
                entity.HasIndex(e => e.ProductId)
                    .IsUnique()
                    .HasDatabaseName("IX_Products_ProductId_Unique");

                // Index for category-based queries
                entity.HasIndex(e => e.PrimaryCategory)
                    .HasDatabaseName("IX_Products_PrimaryCategory");

                entity.HasIndex(e => e.Category)
                    .HasDatabaseName("IX_Products_Category");

                // Index for trending products (sorted by total quantity sold)
                entity.HasIndex(e => e.TotalQuantitySold)
                    .HasDatabaseName("IX_Products_TotalQuantitySold");

                // Index for revenue analysis
                entity.HasIndex(e => e.TotalRevenue)
                    .HasDatabaseName("IX_Products_TotalRevenue");

                // Index for last sold date (find stale products)
                entity.HasIndex(e => e.LastSoldDate)
                    .HasDatabaseName("IX_Products_LastSoldDate");
            });
        }

        private static void ConfigureInvoice(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Invoice>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.DriveFileId)
                    .IsRequired()
                    .HasMaxLength(100);

                // Unique constraint on DriveFileId (one invoice per file)
                entity.HasIndex(e => e.DriveFileId)
                    .IsUnique()
                    .HasDatabaseName("IX_Invoices_DriveFileId_Unique");

                entity.HasIndex(e => e.InvoiceNumber)
                    .HasDatabaseName("IX_Invoices_InvoiceNumber");

                entity.HasIndex(e => e.OrderId)
                    .HasDatabaseName("IX_Invoices_OrderId");

                // Critical for time-series analytics
                entity.HasIndex(e => e.InvoiceDate)
                    .HasDatabaseName("IX_Invoices_InvoiceDate");

                // For vendor analysis
                entity.HasIndex(e => e.VendorName)
                    .HasDatabaseName("IX_Invoices_VendorName");

                // For customer analysis
                entity.HasIndex(e => e.BillToName)
                    .HasDatabaseName("IX_Invoices_BillToName");

                // Composite index for date range queries with amount
                entity.HasIndex(e => new { e.InvoiceDate, e.TotalAmount })
                    .HasDatabaseName("IX_Invoices_Date_Amount");

                // Index for created date queries
                entity.HasIndex(e => e.CreatedAt)
                    .HasDatabaseName("IX_Invoices_CreatedAt");
            });
        }

        private static void ConfigureInvoiceLine(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<InvoiceLine>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.ProductId)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.ProductName)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.Quantity)
                    .IsRequired()
                    .HasColumnType("decimal(18,4)");

                entity.Property(e => e.UnitRate)
                    .IsRequired()
                    .HasColumnType("decimal(18,2)");

                entity.Property(e => e.Amount)
                    .IsRequired()
                    .HasColumnType("decimal(18,2)");

                // Foreign key to Invoice (cascade delete)
                entity.HasOne(e => e.Invoice)
                    .WithMany(i => i.LineItems)
                    .HasForeignKey(e => e.InvoiceId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Foreign key to Product (restrict delete)
                entity.HasOne(e => e.Product)
                    .WithMany(p => p.InvoiceLines)
                    .HasForeignKey(e => e.ProductGuid)
                    .OnDelete(DeleteBehavior.Restrict);

                // Index for product-based queries
                entity.HasIndex(e => e.ProductGuid)
                    .HasDatabaseName("IX_InvoiceLines_ProductGuid");

                // Index for ProductId lookup (denormalized)
                entity.HasIndex(e => e.ProductId)
                    .HasDatabaseName("IX_InvoiceLines_ProductId");

                // Index for category analytics
                entity.HasIndex(e => e.Category)
                    .HasDatabaseName("IX_InvoiceLines_Category");

                // Composite index for product sales over time
                entity.HasIndex(e => new { e.ProductGuid, e.InvoiceId })
                    .HasDatabaseName("IX_InvoiceLines_Product_Invoice");
            });
        }

        private static void ConfigureInvalidInvoice(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<InvalidInvoice>(entity =>
            {
                entity.ToTable("InvalidInvoices");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .HasDefaultValueSql("NEWID()");

                entity.HasIndex(e => e.FileId)
                    .HasDatabaseName("IX_InvalidInvoices_FileId");

                entity.Property(e => e.FileName)
                    .HasMaxLength(500);

                entity.Property(e => e.FileId)
                    .HasMaxLength(100);

                entity.Property(e => e.Reason)
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                entity.Property(e => e.CreatedAt)
                    .IsRequired();
            });
        }
    }
}
