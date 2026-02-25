using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuoteManager.Migrations
{
    /// <inheritdoc />
    public partial class RemoveQuoteRequestTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuoteRequests");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QuoteRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssignedToId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ClientId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    QuoteId = table.Column<int>(type: "int", nullable: true),
                    BudgetRange = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PreferredDeadline = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Requirements = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ServiceType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StaffNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuoteRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuoteRequests_AspNetUsers_AssignedToId",
                        column: x => x.AssignedToId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_QuoteRequests_AspNetUsers_ClientId",
                        column: x => x.ClientId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuoteRequests_Quotes_QuoteId",
                        column: x => x.QuoteId,
                        principalTable: "Quotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuoteRequests_AssignedToId",
                table: "QuoteRequests",
                column: "AssignedToId");

            migrationBuilder.CreateIndex(
                name: "IX_QuoteRequests_ClientId",
                table: "QuoteRequests",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_QuoteRequests_QuoteId",
                table: "QuoteRequests",
                column: "QuoteId");
        }
    }
}
