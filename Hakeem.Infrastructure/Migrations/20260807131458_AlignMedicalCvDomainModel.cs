using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hakeem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AlignMedicalCvDomainModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MedicalCvs_PatientProfiles_PatientProfileId",
                table: "MedicalCvs");

            migrationBuilder.DropIndex(
                name: "IX_MedicalCvVersions_MedicalCvId",
                table: "MedicalCvVersions");

            migrationBuilder.RenameColumn(
                name: "PatientProfileId",
                table: "MedicalCvs",
                newName: "PatientId");

            migrationBuilder.RenameIndex(
                name: "IX_MedicalCvs_PatientProfileId",
                table: "MedicalCvs",
                newName: "IX_MedicalCvs_PatientId");

            migrationBuilder.Sql(
                "UPDATE [MedicalCvVersions] SET [Status] = 'Draft' " +
                "WHERE [Status] NOT IN ('Draft', 'Approved')");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "MedicalCvVersions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "MedicalCvVersions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Focus",
                table: "MedicalCvVersions",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PdfFileKey",
                table: "MedicalCvVersions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ScopeType",
                table: "MedicalCvVersions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Full");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "MedicalCvs",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalCvVersions_MedicalCvId_VersionNumber",
                table: "MedicalCvVersions",
                columns: new[] { "MedicalCvId", "VersionNumber" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_MedicalCvs_PatientProfiles_PatientId",
                table: "MedicalCvs",
                column: "PatientId",
                principalTable: "PatientProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MedicalCvs_PatientProfiles_PatientId",
                table: "MedicalCvs");

            migrationBuilder.DropIndex(
                name: "IX_MedicalCvVersions_MedicalCvId_VersionNumber",
                table: "MedicalCvVersions");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "MedicalCvVersions");

            migrationBuilder.DropColumn(
                name: "Focus",
                table: "MedicalCvVersions");

            migrationBuilder.DropColumn(
                name: "PdfFileKey",
                table: "MedicalCvVersions");

            migrationBuilder.DropColumn(
                name: "ScopeType",
                table: "MedicalCvVersions");

            migrationBuilder.RenameColumn(
                name: "PatientId",
                table: "MedicalCvs",
                newName: "PatientProfileId");

            migrationBuilder.RenameIndex(
                name: "IX_MedicalCvs_PatientId",
                table: "MedicalCvs",
                newName: "IX_MedicalCvs_PatientProfileId");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "MedicalCvVersions",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "MedicalCvs",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.CreateIndex(
                name: "IX_MedicalCvVersions_MedicalCvId",
                table: "MedicalCvVersions",
                column: "MedicalCvId");

            migrationBuilder.AddForeignKey(
                name: "FK_MedicalCvs_PatientProfiles_PatientProfileId",
                table: "MedicalCvs",
                column: "PatientProfileId",
                principalTable: "PatientProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
