using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hakeem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnforceUniquePendingAccessRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_PatientAccessRequests_DoctorProfileId_PatientProfileId",
                table: "PatientAccessRequests",
                columns: new[] { "DoctorProfileId", "PatientProfileId" },
                unique: true,
                filter: "[Status] = 'Pending'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PatientAccessRequests_DoctorProfileId_PatientProfileId",
                table: "PatientAccessRequests");
        }
    }
}
