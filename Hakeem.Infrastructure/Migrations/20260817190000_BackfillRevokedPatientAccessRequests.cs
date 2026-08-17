using Hakeem.Infrastructure.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hakeem.Infrastructure.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260817190000_BackfillRevokedPatientAccessRequests")]
public partial class BackfillRevokedPatientAccessRequests : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE accessRequest
            SET accessRequest.Status = 'Revoked',
                accessRequest.UpdatedAt = COALESCE(access.RevokedAt, SYSUTCDATETIME())
            FROM PatientAccessRequests AS accessRequest
            INNER JOIN DoctorPatientAccesses AS access
                ON access.PatientAccessRequestId = accessRequest.Id
            WHERE accessRequest.Status = 'Redeemed'
              AND access.Status = 'Revoked';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // The backfill is intentionally irreversible because reverting every
        // revoked request could overwrite legitimate revocations created later.
    }
}
