using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuoteManager.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "TaxMasters",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ServiceMasters",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Quotes",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Invoices",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            // Performance Indexes
            // Quotes - Composite index for filtered queries
            migrationBuilder.CreateIndex(
                name: "IX_Quotes_ClientId_Status_CreatedDate",
                table: "Quotes",
                columns: new[] { "ClientId", "Status", "CreatedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_CreatedById_CreatedDate",
                table: "Quotes",
                columns: new[] { "CreatedById", "CreatedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_Status_CreatedDate",
                table: "Quotes",
                columns: new[] { "Status", "CreatedDate" });

            // Invoices - Composite index for client queries
            migrationBuilder.CreateIndex(
                name: "IX_Invoices_ClientId_Status_InvoiceDate",
                table: "Invoices",
                columns: new[] { "ClientId", "Status", "InvoiceDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_QuoteId",
                table: "Invoices",
                column: "QuoteId");

            // QuoteItems - Foreign key indexes
            migrationBuilder.CreateIndex(
                name: "IX_QuoteItems_QuoteId",
                table: "QuoteItems",
                column: "QuoteId");

            migrationBuilder.CreateIndex(
                name: "IX_QuoteItems_ServiceId",
                table: "QuoteItems",
                column: "ServiceId");

            // QuoteItemTaxes - Foreign key indexes
            migrationBuilder.CreateIndex(
                name: "IX_QuoteItemTaxes_QuoteItemId",
                table: "QuoteItemTaxes",
                column: "QuoteItemId");

            migrationBuilder.CreateIndex(
                name: "IX_QuoteItemTaxes_TaxId",
                table: "QuoteItemTaxes",
                column: "TaxId");

            // ServiceMasters - Active services filter
            migrationBuilder.CreateIndex(
                name: "IX_ServiceMasters_IsActive",
                table: "ServiceMasters",
                column: "IsActive");

            // TaxMasters - Active taxes filter
            migrationBuilder.CreateIndex(
                name: "IX_TaxMasters_IsActive",
                table: "TaxMasters",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop indexes first
            migrationBuilder.DropIndex(
                name: "IX_TaxMasters_IsActive",
                table: "TaxMasters");

            migrationBuilder.DropIndex(
                name: "IX_ServiceMasters_IsActive",
                table: "ServiceMasters");

            migrationBuilder.DropIndex(
                name: "IX_QuoteItemTaxes_TaxId",
                table: "QuoteItemTaxes");

            migrationBuilder.DropIndex(
                name: "IX_QuoteItemTaxes_QuoteItemId",
                table: "QuoteItemTaxes");

            migrationBuilder.DropIndex(
                name: "IX_QuoteItems_ServiceId",
                table: "QuoteItems");

            migrationBuilder.DropIndex(
                name: "IX_QuoteItems_QuoteId",
                table: "QuoteItems");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_QuoteId",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_ClientId_Status_InvoiceDate",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Quotes_Status_CreatedDate",
                table: "Quotes");

            migrationBuilder.DropIndex(
                name: "IX_Quotes_CreatedById_CreatedDate",
                table: "Quotes");

            migrationBuilder.DropIndex(
                name: "IX_Quotes_ClientId_Status_CreatedDate",
                table: "Quotes");

            // Drop RowVersion columns
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "TaxMasters");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ServiceMasters");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Invoices");
        }
    }
}
