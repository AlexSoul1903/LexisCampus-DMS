using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LexisCampusDMS.Infraestructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentRevocationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ResolutionNumber",
                table: "Documents",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RevocationObservations",
                table: "Documents",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RevocationReason",
                table: "Documents",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RevokedAtUtc",
                table: "Documents",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RevokedBy",
                table: "Documents",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Documents_ResolutionNumber",
                table: "Documents",
                column: "ResolutionNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Documents_ResolutionNumber",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "ResolutionNumber",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "RevocationObservations",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "RevocationReason",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "RevokedAtUtc",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "RevokedBy",
                table: "Documents");
        }
    }
}
