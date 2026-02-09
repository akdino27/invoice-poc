using invoice_v1.src.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace invoice_v1.src.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<FileChangeLog> FileChangeLogs => Set<FileChangeLog>();
        public DbSet<JobQueue> JobQueues => Set<JobQueue>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<Invoice> Invoices => Set<Invoice>();
        public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();
        public DbSet<InvalidInvoice> InvalidInvoices => Set<InvalidInvoice>();

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

        // -------------------------
        // FileChangeLog
        // -------------------------
        private static void ConfigureFileChangeLog(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FileChangeLog>(entity =>
            {
                entity.ToTable("file_change_logs");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.DetectedAt).HasColumnType("timestamptz");
                entity.Property(e => e.GoogleDriveModifiedTime).HasColumnType("timestamptz");
                entity.Property(e => e.ProcessedAt).HasColumnType("timestamptz");

                entity.HasIndex(e => new { e.Processed, e.DetectedAt })
                      .HasDatabaseName("ix_file_change_logs_processed_detected_at");

                entity.HasIndex(e => e.FileId)
                      .HasDatabaseName("ix_file_change_logs_file_id");

                entity.HasIndex(e => e.ChangeType)
                      .HasDatabaseName("ix_file_change_logs_change_type");
            });
        }

        // -------------------------
        // JobQueue
        // -------------------------
        private static void ConfigureJobQueue(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<JobQueue>(entity =>
            {
                entity.ToTable("job_queues");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.PayloadJson).HasColumnType("jsonb");
                entity.Property(e => e.ErrorMessage).HasColumnType("jsonb");

                entity.Property(e => e.CreatedAt).HasColumnType("timestamptz");
                entity.Property(e => e.UpdatedAt).HasColumnType("timestamptz");
                entity.Property(e => e.NextRetryAt).HasColumnType("timestamptz");
                entity.Property(e => e.LockedAt).HasColumnType("timestamptz");

                entity.HasIndex(e => new { e.Status, e.NextRetryAt })
                      .HasDatabaseName("ix_job_queues_status_next_retry_at");

                entity.HasIndex(e => new { e.Status, e.LockedAt })
                      .HasDatabaseName("ix_job_queues_status_locked_at");

                entity.HasIndex(e => e.CreatedAt)
                      .HasDatabaseName("ix_job_queues_created_at");

                // GIN index added via migration SQL
                entity.HasIndex(e => e.PayloadJson)
                      .HasDatabaseName("ix_job_queues_payload_json");
            });
        }

        // -------------------------
        // Product
        // -------------------------
        private static void ConfigureProduct(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Product>(entity =>
            {
                entity.ToTable("products");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.DefaultUnitRate).HasColumnType("numeric(18,2)");
                entity.Property(e => e.TotalQuantitySold).HasColumnType("numeric(18,4)");
                entity.Property(e => e.TotalRevenue).HasColumnType("numeric(18,2)");

                entity.Property(e => e.LastSoldDate).HasColumnType("timestamptz");
                entity.Property(e => e.CreatedAt).HasColumnType("timestamptz");
                entity.Property(e => e.UpdatedAt).HasColumnType("timestamptz");

                entity.HasIndex(e => e.ProductId)
                      .IsUnique()
                      .HasDatabaseName("ix_products_product_id_unique");

                entity.HasIndex(e => e.PrimaryCategory)
                      .HasDatabaseName("ix_products_primary_category");

                entity.HasIndex(e => e.Category)
                      .HasDatabaseName("ix_products_category");

                entity.HasIndex(e => e.TotalRevenue)
                      .HasDatabaseName("ix_products_total_revenue");

                entity.HasIndex(e => e.LastSoldDate)
                      .HasDatabaseName("ix_products_last_sold_date");
            });
        }

        // -------------------------
        // Invoice
        // -------------------------
        private static void ConfigureInvoice(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Invoice>(entity =>
            {
                entity.ToTable("invoices");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.InvoiceDate).HasColumnType("timestamptz");
                entity.Property(e => e.CreatedAt).HasColumnType("timestamptz");
                entity.Property(e => e.UpdatedAt).HasColumnType("timestamptz");

                entity.Property(e => e.Subtotal).HasColumnType("numeric(18,2)");
                entity.Property(e => e.DiscountPercentage).HasColumnType("numeric(18,2)");
                entity.Property(e => e.DiscountAmount).HasColumnType("numeric(18,2)");
                entity.Property(e => e.ShippingCost).HasColumnType("numeric(18,2)");
                entity.Property(e => e.TotalAmount).HasColumnType("numeric(18,2)");
                entity.Property(e => e.BalanceDue).HasColumnType("numeric(18,2)");

                entity.Property(e => e.ExtractedDataJson).HasColumnType("jsonb");

                entity.HasIndex(e => e.DriveFileId)
                      .IsUnique()
                      .HasDatabaseName("ix_invoices_drive_file_id_unique");

                entity.HasIndex(e => e.InvoiceDate)
                      .HasDatabaseName("ix_invoices_invoice_date");

                entity.HasIndex(e => new { e.InvoiceDate, e.TotalAmount })
                      .HasDatabaseName("ix_invoices_date_amount");
            });
        }

        // -------------------------
        // InvoiceLine
        // -------------------------
        private static void ConfigureInvoiceLine(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<InvoiceLine>(entity =>
            {
                entity.ToTable("invoice_lines");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Quantity).HasColumnType("numeric(18,4)");
                entity.Property(e => e.UnitRate).HasColumnType("numeric(18,2)");
                entity.Property(e => e.Amount).HasColumnType("numeric(18,2)");
                entity.Property(e => e.CreatedAt).HasColumnType("timestamptz");

                entity.HasOne(e => e.Invoice)
                      .WithMany(i => i.LineItems)
                      .HasForeignKey(e => e.InvoiceId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Product)
                      .WithMany(p => p.InvoiceLines)
                      .HasForeignKey(e => e.ProductGuid)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.ProductGuid)
                      .HasDatabaseName("ix_invoice_lines_product_guid");

                entity.HasIndex(e => new { e.ProductGuid, e.InvoiceId })
                      .HasDatabaseName("ix_invoice_lines_product_invoice");
            });
        }

        // -------------------------
        // InvalidInvoice
        // -------------------------
        private static void ConfigureInvalidInvoice(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<InvalidInvoice>(entity =>
            {
                entity.ToTable("invalid_invoices");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                      .HasDefaultValueSql("gen_random_uuid()");

                entity.Property(e => e.Reason).HasColumnType("jsonb");
                entity.Property(e => e.CreatedAt).HasColumnType("timestamptz");

                entity.HasIndex(e => e.FileId)
                      .HasDatabaseName("ix_invalid_invoices_file_id");

                // GIN index via migration SQL
                entity.HasIndex(e => e.Reason)
                      .HasDatabaseName("ix_invalid_invoices_reason");
            });
        }
    }
}
