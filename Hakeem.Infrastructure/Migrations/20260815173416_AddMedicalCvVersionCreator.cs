using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hakeem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMedicalCvVersionCreator : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedByRole",
                table: "MedicalCvVersions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Unknown");

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "MedicalCvVersions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MedicalCvVersions_CreatedByUserId",
                table: "MedicalCvVersions",
                column: "CreatedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_MedicalCvVersions_Users_CreatedByUserId",
                table: "MedicalCvVersions",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MedicalCvVersions_Users_CreatedByUserId",
                table: "MedicalCvVersions");

            migrationBuilder.DropIndex(
                name: "IX_MedicalCvVersions_CreatedByUserId",
                table: "MedicalCvVersions");

            migrationBuilder.DropColumn(
                name: "CreatedByRole",
                table: "MedicalCvVersions");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "MedicalCvVersions");
        }
    }
}
