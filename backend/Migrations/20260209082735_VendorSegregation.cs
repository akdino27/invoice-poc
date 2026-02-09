using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace invoice_v1.Migrations
{
    /// <inheritdoc />
    public partial class VendorSegregation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FileChangeLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FileId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ChangeType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DetectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MimeType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    FileSize = table.Column<long>(type: "bigint", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    GoogleDriveModifiedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Processed = table.Column<bool>(type: "boolean", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileChangeLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JobQueues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    LockedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    LockedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NextRetryAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobQueues", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Vendors",
                columns: table => new
                {
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    FirstSeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastActivityAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vendors", x => x.Email);
                });

            migrationBuilder.CreateTable(
                name: "InvalidInvoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorEmail = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    FileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FileId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvalidInvoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvalidInvoices_Vendors_VendorEmail",
                        column: x => x.VendorEmail,
                        principalTable: "Vendors",
                        principalColumn: "Email");
                });

            migrationBuilder.CreateTable(
                name: "Invoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorEmail = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DriveFileId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    InvoiceNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    InvoiceDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OrderId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    VendorName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    BillToName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ShipToCity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ShipToState = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ShipToCountry = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ShipMode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Subtotal = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    DiscountPercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    DiscountAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    ShippingCost = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    BalanceDue = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    Terms = table.Column<string>(type: "text", nullable: true),
                    ExtractedDataJson = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Invoices_Vendors_VendorEmail",
                        column: x => x.VendorEmail,
                        principalTable: "Vendors",
                        principalColumn: "Email",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VendorEmail = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ProductId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ProductName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Category = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PrimaryCategory = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SecondaryCategory = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DefaultUnitRate = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    TotalQuantitySold = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalRevenue = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    InvoiceCount = table.Column<int>(type: "integer", nullable: false),
                    LastSoldDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Products_Vendors_VendorEmail",
                        column: x => x.VendorEmail,
                        principalTable: "Vendors",
                        principalColumn: "Email",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceLines",
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
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceLines_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InvoiceLines_Products_ProductGuid",
                        column: x => x.ProductGuid,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FileChangeLogs_ChangeType",
                table: "FileChangeLogs",
                column: "ChangeType");

            migrationBuilder.CreateIndex(
                name: "IX_FileChangeLogs_FileId",
                table: "FileChangeLogs",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_FileChangeLogs_ModifiedBy",
                table: "FileChangeLogs",
                column: "ModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_FileChangeLogs_Processed_DetectedAt",
                table: "FileChangeLogs",
                columns: new[] { "Processed", "DetectedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_InvalidInvoices_FileId",
                table: "InvalidInvoices",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_InvalidInvoices_VendorEmail",
                table: "InvalidInvoices",
                column: "VendorEmail");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLines_Category",
                table: "InvoiceLines",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLines_InvoiceId",
                table: "InvoiceLines",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLines_Product_Invoice",
                table: "InvoiceLines",
                columns: new[] { "ProductGuid", "InvoiceId" });

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLines_ProductGuid",
                table: "InvoiceLines",
                column: "ProductGuid");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLines_ProductId",
                table: "InvoiceLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_BillToName",
                table: "Invoices",
                column: "BillToName");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_CreatedAt",
                table: "Invoices",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_Date_Amount",
                table: "Invoices",
                columns: new[] { "InvoiceDate", "TotalAmount" });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_DriveFileId_Unique",
                table: "Invoices",
                column: "DriveFileId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_ExtractedDataJson_Gin",
                table: "Invoices",
                column: "ExtractedDataJson")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_InvoiceDate",
                table: "Invoices",
                column: "InvoiceDate");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_InvoiceNumber",
                table: "Invoices",
                column: "InvoiceNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_OrderId",
                table: "Invoices",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_VendorEmail",
                table: "Invoices",
                column: "VendorEmail");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_VendorEmail_InvoiceNumber_Unique",
                table: "Invoices",
                columns: new[] { "VendorEmail", "InvoiceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_VendorName",
                table: "Invoices",
                column: "VendorName");

            migrationBuilder.CreateIndex(
                name: "IX_JobQueues_CreatedAt",
                table: "JobQueues",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_JobQueues_LockedBy_LockedAt",
                table: "JobQueues",
                columns: new[] { "LockedBy", "LockedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_JobQueues_PayloadJson_Gin",
                table: "JobQueues",
                column: "PayloadJson")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_JobQueues_Status",
                table: "JobQueues",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_JobQueues_Status_NextRetryAt",
                table: "JobQueues",
                columns: new[] { "Status", "NextRetryAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_Category",
                table: "Products",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Products_LastSoldDate",
                table: "Products",
                column: "LastSoldDate");

            migrationBuilder.CreateIndex(
                name: "IX_Products_PrimaryCategory",
                table: "Products",
                column: "PrimaryCategory");

            migrationBuilder.CreateIndex(
                name: "IX_Products_ProductId",
                table: "Products",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_TotalQuantitySold",
                table: "Products",
                column: "TotalQuantitySold");

            migrationBuilder.CreateIndex(
                name: "IX_Products_TotalRevenue",
                table: "Products",
                column: "TotalRevenue");

            migrationBuilder.CreateIndex(
                name: "IX_Products_VendorEmail",
                table: "Products",
                column: "VendorEmail");

            migrationBuilder.CreateIndex(
                name: "IX_Products_VendorEmail_ProductId_Unique",
                table: "Products",
                columns: new[] { "VendorEmail", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vendors_Email",
                table: "Vendors",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vendors_LastActivityAt",
                table: "Vendors",
                column: "LastActivityAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FileChangeLogs");

            migrationBuilder.DropTable(
                name: "InvalidInvoices");

            migrationBuilder.DropTable(
                name: "InvoiceLines");

            migrationBuilder.DropTable(
                name: "JobQueues");

            migrationBuilder.DropTable(
                name: "Invoices");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "Vendors");
        }
    }
}
