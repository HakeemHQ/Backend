using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hakeem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MoveMedicalCvScopeToLogicalCv : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MedicalCvs_PatientId",
                table: "MedicalCvs");

            migrationBuilder.AddColumn<string>(
                name: "Focus",
                table: "MedicalCvs",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScopeType",
                table: "MedicalCvs",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.Sql(
                """
                SET NOCOUNT ON;

                CREATE TABLE #MedicalCvLogicalGroups
                (
                    OriginalMedicalCvId uniqueidentifier NOT NULL,
                    ScopeType nvarchar(20) NOT NULL,
                    Focus nvarchar(200) NULL,
                    TargetMedicalCvId uniqueidentifier NOT NULL,
                    GroupOrder int NOT NULL
                );

                ;WITH NormalizedVersionGroups AS
                (
                    SELECT DISTINCT
                        [MedicalCvId],
                        CASE
                            WHEN [ScopeType] = 'Focused'
                                 AND NULLIF(LTRIM(RTRIM([Focus])), '') IS NOT NULL
                                THEN 'Focused'
                            ELSE 'Full'
                        END AS [ScopeType],
                        CASE
                            WHEN [ScopeType] = 'Focused'
                                 AND NULLIF(LTRIM(RTRIM([Focus])), '') IS NOT NULL
                                THEN LTRIM(RTRIM([Focus]))
                            ELSE NULL
                        END AS [Focus]
                    FROM [MedicalCvVersions]
                ),
                LogicalGroups AS
                (
                    SELECT [MedicalCvId], [ScopeType], [Focus]
                    FROM NormalizedVersionGroups

                    UNION ALL

                    SELECT [Id], 'Full', NULL
                    FROM [MedicalCvs] AS cv
                    WHERE NOT EXISTS
                    (
                        SELECT 1
                        FROM NormalizedVersionGroups AS versionGroup
                        WHERE versionGroup.[MedicalCvId] = cv.[Id]
                    )
                ),
                RankedGroups AS
                (
                    SELECT
                        [MedicalCvId],
                        [ScopeType],
                        [Focus],
                        ROW_NUMBER() OVER
                        (
                            PARTITION BY [MedicalCvId]
                            ORDER BY
                                CASE WHEN [ScopeType] = 'Full' THEN 0 ELSE 1 END,
                                [Focus]
                        ) AS [GroupOrder]
                    FROM LogicalGroups
                )
                INSERT INTO #MedicalCvLogicalGroups
                (
                    [OriginalMedicalCvId],
                    [ScopeType],
                    [Focus],
                    [TargetMedicalCvId],
                    [GroupOrder]
                )
                SELECT
                    [MedicalCvId],
                    [ScopeType],
                    [Focus],
                    CASE WHEN [GroupOrder] = 1 THEN [MedicalCvId] ELSE NEWID() END,
                    [GroupOrder]
                FROM RankedGroups;

                UPDATE cv
                SET
                    cv.[ScopeType] = logicalGroup.[ScopeType],
                    cv.[Focus] = logicalGroup.[Focus]
                FROM [MedicalCvs] AS cv
                INNER JOIN #MedicalCvLogicalGroups AS logicalGroup
                    ON logicalGroup.[OriginalMedicalCvId] = cv.[Id]
                   AND logicalGroup.[GroupOrder] = 1;

                INSERT INTO [MedicalCvs]
                (
                    [Id],
                    [PatientId],
                    [Title],
                    [ScopeType],
                    [Focus],
                    [CreatedAt],
                    [UpdatedAt]
                )
                SELECT
                    logicalGroup.[TargetMedicalCvId],
                    cv.[PatientId],
                    cv.[Title],
                    logicalGroup.[ScopeType],
                    logicalGroup.[Focus],
                    cv.[CreatedAt],
                    cv.[UpdatedAt]
                FROM #MedicalCvLogicalGroups AS logicalGroup
                INNER JOIN [MedicalCvs] AS cv
                    ON cv.[Id] = logicalGroup.[OriginalMedicalCvId]
                WHERE logicalGroup.[GroupOrder] > 1;

                UPDATE version
                SET version.[MedicalCvId] = logicalGroup.[TargetMedicalCvId]
                FROM [MedicalCvVersions] AS version
                INNER JOIN #MedicalCvLogicalGroups AS logicalGroup
                    ON logicalGroup.[OriginalMedicalCvId] = version.[MedicalCvId]
                   AND logicalGroup.[ScopeType] =
                       CASE
                           WHEN version.[ScopeType] = 'Focused'
                                AND NULLIF(LTRIM(RTRIM(version.[Focus])), '') IS NOT NULL
                               THEN 'Focused'
                           ELSE 'Full'
                       END
                   AND
                       (
                           logicalGroup.[Focus] =
                               CASE
                                   WHEN version.[ScopeType] = 'Focused'
                                        AND NULLIF(LTRIM(RTRIM(version.[Focus])), '') IS NOT NULL
                                       THEN LTRIM(RTRIM(version.[Focus]))
                                   ELSE NULL
                               END
                           OR
                           (
                               logicalGroup.[Focus] IS NULL
                               AND
                               CASE
                                   WHEN version.[ScopeType] = 'Focused'
                                        AND NULLIF(LTRIM(RTRIM(version.[Focus])), '') IS NOT NULL
                                       THEN LTRIM(RTRIM(version.[Focus]))
                                   ELSE NULL
                               END IS NULL
                           )
                       );

                DROP TABLE #MedicalCvLogicalGroups;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "ScopeType",
                table: "MedicalCvs",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "Focus",
                table: "MedicalCvVersions");

            migrationBuilder.DropColumn(
                name: "ScopeType",
                table: "MedicalCvVersions");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalCvs_PatientId_ScopeType_Focus",
                table: "MedicalCvs",
                columns: new[] { "PatientId", "ScopeType", "Focus" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_MedicalCvs_ScopeType_Focus",
                table: "MedicalCvs",
                sql: "([ScopeType] = 'Full' AND [Focus] IS NULL) OR ([ScopeType] = 'Focused' AND [Focus] IS NOT NULL AND LEN(LTRIM(RTRIM([Focus]))) > 0)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MedicalCvs_PatientId_ScopeType_Focus",
                table: "MedicalCvs");

            migrationBuilder.DropCheckConstraint(
                name: "CK_MedicalCvs_ScopeType_Focus",
                table: "MedicalCvs");

            migrationBuilder.AddColumn<string>(
                name: "Focus",
                table: "MedicalCvVersions",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScopeType",
                table: "MedicalCvVersions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Full");

            migrationBuilder.Sql(
                """
                UPDATE version
                SET
                    version.[ScopeType] = cv.[ScopeType],
                    version.[Focus] = cv.[Focus]
                FROM [MedicalCvVersions] AS version
                INNER JOIN [MedicalCvs] AS cv
                    ON cv.[Id] = version.[MedicalCvId];
                """);

            migrationBuilder.DropColumn(
                name: "Focus",
                table: "MedicalCvs");

            migrationBuilder.DropColumn(
                name: "ScopeType",
                table: "MedicalCvs");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalCvs_PatientId",
                table: "MedicalCvs",
                column: "PatientId");
        }
    }
}
