using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace invoice_v1.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "file_change_logs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FileId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ChangeType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    DetectedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    MimeType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    FileSize = table.Column<long>(type: "bigint", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    GoogleDriveModifiedTime = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    Processed = table.Column<bool>(type: "boolean", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_file_change_logs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "invalid_invoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    FileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FileId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Reason = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invalid_invoices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "invoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    InvoiceDate = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    OrderId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    VendorName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    BillToName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ShipToCity = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ShipToState = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ShipToCountry = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ShipMode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Subtotal = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    DiscountPercentage = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    DiscountAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    ShippingCost = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    BalanceDue = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    Terms = table.Column<string>(type: "text", nullable: true),
                    DriveFileId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ExtractedDataJson = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "job_queues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PayloadJson = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    LockedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    LockedAt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    NextRetryAt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    ErrorMessage = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_queues", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ProductName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Category = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PrimaryCategory = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SecondaryCategory = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DefaultUnitRate = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    TotalQuantitySold = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    TotalRevenue = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    InvoiceCount = table.Column<int>(type: "integer", nullable: false),
                    LastSoldDate = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_products", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "invoice_lines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ProductName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Category = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    UnitRate = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoice_lines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_invoice_lines_invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_invoice_lines_products_ProductGuid",
                        column: x => x.ProductGuid,
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_file_change_logs_change_type",
                table: "file_change_logs",
                column: "ChangeType");

            migrationBuilder.CreateIndex(
                name: "ix_file_change_logs_file_id",
                table: "file_change_logs",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "ix_file_change_logs_processed_detected_at",
                table: "file_change_logs",
                columns: new[] { "Processed", "DetectedAt" });

            migrationBuilder.CreateIndex(
                name: "ix_invalid_invoices_file_id",
                table: "invalid_invoices",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "ix_invalid_invoices_reason",
                table: "invalid_invoices",
                column: "Reason");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_lines_InvoiceId",
                table: "invoice_lines",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "ix_invoice_lines_product_guid",
                table: "invoice_lines",
                column: "ProductGuid");

            migrationBuilder.CreateIndex(
                name: "ix_invoice_lines_product_invoice",
                table: "invoice_lines",
                columns: new[] { "ProductGuid", "InvoiceId" });

            migrationBuilder.CreateIndex(
                name: "ix_invoices_date_amount",
                table: "invoices",
                columns: new[] { "InvoiceDate", "TotalAmount" });

            migrationBuilder.CreateIndex(
                name: "ix_invoices_drive_file_id_unique",
                table: "invoices",
                column: "DriveFileId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_invoices_invoice_date",
                table: "invoices",
                column: "InvoiceDate");

            migrationBuilder.CreateIndex(
                name: "ix_job_queues_created_at",
                table: "job_queues",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "ix_job_queues_payload_json",
                table: "job_queues",
                column: "PayloadJson");

            migrationBuilder.CreateIndex(
                name: "ix_job_queues_status_locked_at",
                table: "job_queues",
                columns: new[] { "Status", "LockedAt" });

            migrationBuilder.CreateIndex(
                name: "ix_job_queues_status_next_retry_at",
                table: "job_queues",
                columns: new[] { "Status", "NextRetryAt" });

            migrationBuilder.CreateIndex(
                name: "ix_products_category",
                table: "products",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "ix_products_last_sold_date",
                table: "products",
                column: "LastSoldDate");

            migrationBuilder.CreateIndex(
                name: "ix_products_primary_category",
                table: "products",
                column: "PrimaryCategory");

            migrationBuilder.CreateIndex(
                name: "ix_products_product_id_unique",
                table: "products",
                column: "ProductId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_products_total_revenue",
                table: "products",
                column: "TotalRevenue");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "file_change_logs");

            migrationBuilder.DropTable(
                name: "invalid_invoices");

            migrationBuilder.DropTable(
                name: "invoice_lines");

            migrationBuilder.DropTable(
                name: "job_queues");

            migrationBuilder.DropTable(
                name: "invoices");

            migrationBuilder.DropTable(
                name: "products");
        }
    }
}
