using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hakeem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDoctorPatientAccessDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PatientAccessRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DoctorProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CodeHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CodeExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RedeemedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientAccessRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientAccessRequests_DoctorProfiles_DoctorProfileId",
                        column: x => x.DoctorProfileId,
                        principalTable: "DoctorProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PatientAccessRequests_PatientProfiles_PatientProfileId",
                        column: x => x.PatientProfileId,
                        principalTable: "PatientProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DoctorPatientAccesses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientAccessRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DoctorProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Active"),
                    GrantedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorPatientAccesses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DoctorPatientAccesses_DoctorProfiles_DoctorProfileId",
                        column: x => x.DoctorProfileId,
                        principalTable: "DoctorProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DoctorPatientAccesses_PatientAccessRequests_PatientAccessRequestId",
                        column: x => x.PatientAccessRequestId,
                        principalTable: "PatientAccessRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DoctorPatientAccesses_PatientProfiles_PatientProfileId",
                        column: x => x.PatientProfileId,
                        principalTable: "PatientProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DoctorPatientAccesses_DoctorProfileId_PatientProfileId",
                table: "DoctorPatientAccesses",
                columns: new[] { "DoctorProfileId", "PatientProfileId" },
                unique: true,
                filter: "[Status] = 'Active'");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorPatientAccesses_DoctorProfileId_PatientProfileId_Status",
                table: "DoctorPatientAccesses",
                columns: new[] { "DoctorProfileId", "PatientProfileId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_DoctorPatientAccesses_PatientAccessRequestId",
                table: "DoctorPatientAccesses",
                column: "PatientAccessRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DoctorPatientAccesses_PatientProfileId",
                table: "DoctorPatientAccesses",
                column: "PatientProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientAccessRequests_CodeHash",
                table: "PatientAccessRequests",
                column: "CodeHash",
                unique: true,
                filter: "[CodeHash] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PatientAccessRequests_DoctorProfileId_Status_RequestedAt",
                table: "PatientAccessRequests",
                columns: new[] { "DoctorProfileId", "Status", "RequestedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PatientAccessRequests_PatientProfileId_Status_RequestedAt",
                table: "PatientAccessRequests",
                columns: new[] { "PatientProfileId", "Status", "RequestedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DoctorPatientAccesses");

            migrationBuilder.DropTable(
                name: "PatientAccessRequests");
        }
    }
}
