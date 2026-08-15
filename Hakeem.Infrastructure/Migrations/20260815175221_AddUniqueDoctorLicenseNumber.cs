using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hakeem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueDoctorLicenseNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE [DoctorProfiles]
                SET [LicenseNumber] = 'HKM-DR-' +
                    UPPER(REPLACE(CONVERT(varchar(36), [Id]), '-', ''));
                """);

            migrationBuilder.CreateIndex(
                name: "IX_DoctorProfiles_LicenseNumber",
                table: "DoctorProfiles",
                column: "LicenseNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DoctorProfiles_LicenseNumber",
                table: "DoctorProfiles");
        }
    }
}
