using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hakeem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSharedIdentityRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "UserType",
                table: "Users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "IdentityVerificationStatus",
                table: "PatientProfiles",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Pending");

            migrationBuilder.AddColumn<string>(
                name: "NationalId",
                table: "PatientProfiles",
                type: "nvarchar(14)",
                maxLength: 14,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PatientCode",
                table: "PatientProfiles",
                type: "nvarchar(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VerifiedAt",
                table: "PatientProfiles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VerifiedByDoctorId",
                table: "PatientProfiles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerifiedNationalId",
                table: "PatientProfiles",
                type: "nvarchar(14)",
                maxLength: 14,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DoctorProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Specialty = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LicenseNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DoctorProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(
                """
                IF (SELECT COUNT_BIG(*) FROM [PatientProfiles]) > 999999
                    THROW 50000, 'Cannot backfill contract-shaped patient codes for more than 999999 existing patients.', 1;

                ;WITH [PatientCodes] AS
                (
                    SELECT
                        [Id],
                        RIGHT('000000' + CONVERT(varchar(6), ROW_NUMBER() OVER (ORDER BY [Id])), 6) AS [Code]
                    FROM [PatientProfiles]
                )
                UPDATE [PatientProfiles]
                SET [PatientCode] =
                    'H' + SUBSTRING([PatientCodes].[Code], 1, 3) +
                    '-' + SUBSTRING([PatientCodes].[Code], 4, 3)
                FROM [PatientProfiles]
                INNER JOIN [PatientCodes]
                    ON [PatientProfiles].[Id] = [PatientCodes].[Id];
                """);

            migrationBuilder.AlterColumn<string>(
                name: "PatientCode",
                table: "PatientProfiles",
                type: "nvarchar(8)",
                maxLength: 8,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(8)",
                oldMaxLength: 8,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PatientProfiles_PatientCode",
                table: "PatientProfiles",
                column: "PatientCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PatientProfiles_VerifiedByDoctorId",
                table: "PatientProfiles",
                column: "VerifiedByDoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientProfiles_VerifiedNationalId",
                table: "PatientProfiles",
                column: "VerifiedNationalId",
                unique: true,
                filter: "[VerifiedNationalId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorProfiles_UserId",
                table: "DoctorProfiles",
                column: "UserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PatientProfiles_DoctorProfiles_VerifiedByDoctorId",
                table: "PatientProfiles",
                column: "VerifiedByDoctorId",
                principalTable: "DoctorProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PatientProfiles_DoctorProfiles_VerifiedByDoctorId",
                table: "PatientProfiles");

            migrationBuilder.DropTable(
                name: "DoctorProfiles");

            migrationBuilder.DropIndex(
                name: "IX_PatientProfiles_PatientCode",
                table: "PatientProfiles");

            migrationBuilder.DropIndex(
                name: "IX_PatientProfiles_VerifiedByDoctorId",
                table: "PatientProfiles");

            migrationBuilder.DropIndex(
                name: "IX_PatientProfiles_VerifiedNationalId",
                table: "PatientProfiles");

            migrationBuilder.DropColumn(
                name: "IdentityVerificationStatus",
                table: "PatientProfiles");

            migrationBuilder.DropColumn(
                name: "NationalId",
                table: "PatientProfiles");

            migrationBuilder.DropColumn(
                name: "PatientCode",
                table: "PatientProfiles");

            migrationBuilder.DropColumn(
                name: "VerifiedAt",
                table: "PatientProfiles");

            migrationBuilder.DropColumn(
                name: "VerifiedByDoctorId",
                table: "PatientProfiles");

            migrationBuilder.DropColumn(
                name: "VerifiedNationalId",
                table: "PatientProfiles");

            migrationBuilder.AlterColumn<string>(
                name: "UserType",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);
        }
    }
}
