using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hakeem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExtractedItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExtractedFields_MedicalDocuments_DocumentId",
                table: "ExtractedFields");

            migrationBuilder.AddColumn<string>(
                name: "ExtractionStatus",
                table: "MedicalDocuments",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Queued");

            migrationBuilder.AddColumn<string>(
                name: "FailureCode",
                table: "MedicalDocuments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EvidenceText",
                table: "ExtractedFields",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "ExtractedItemId",
                table: "ExtractedFields",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Issues",
                table: "ExtractedFields",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ExtractedItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MedicalDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SequenceNumber = table.Column<int>(type: "int", nullable: false),
                    PageNumber = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExtractedItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExtractedItems_MedicalDocuments_MedicalDocumentId",
                        column: x => x.MedicalDocumentId,
                        principalTable: "MedicalDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(
                """
                DECLARE @ItemMap TABLE
                (
                    Id uniqueidentifier NOT NULL,
                    MedicalDocumentId uniqueidentifier NOT NULL,
                    FieldGroup nvarchar(max) NOT NULL,
                    SequenceNumber int NOT NULL
                );

                INSERT INTO @ItemMap (Id, MedicalDocumentId, FieldGroup, SequenceNumber)
                SELECT
                    NEWID(),
                    grouped.DocumentId,
                    grouped.FieldGroup,
                    CONVERT(int, ROW_NUMBER() OVER (
                        PARTITION BY grouped.DocumentId
                        ORDER BY grouped.FieldGroup))
                FROM
                (
                    SELECT DISTINCT DocumentId, FieldGroup
                    FROM ExtractedFields
                ) AS grouped;

                INSERT INTO ExtractedItems
                    (Id, MedicalDocumentId, ItemType, SequenceNumber, PageNumber, CreatedAt, UpdatedAt)
                SELECT
                    Id,
                    MedicalDocumentId,
                    CASE WHEN FieldGroup = '' THEN 'Unknown' ELSE FieldGroup END,
                    SequenceNumber,
                    0,
                    SYSUTCDATETIME(),
                    SYSUTCDATETIME()
                FROM @ItemMap;

                UPDATE fields
                SET fields.ExtractedItemId = itemMap.Id
                FROM ExtractedFields AS fields
                INNER JOIN @ItemMap AS itemMap
                    ON itemMap.MedicalDocumentId = fields.DocumentId
                    AND itemMap.FieldGroup = fields.FieldGroup;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "ExtractedItemId",
                table: "ExtractedFields",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.DropIndex(
                name: "IX_ExtractedFields_DocumentId",
                table: "ExtractedFields");

            migrationBuilder.DropColumn(
                name: "DocumentId",
                table: "ExtractedFields");

            migrationBuilder.DropColumn(
                name: "FieldGroup",
                table: "ExtractedFields");

            migrationBuilder.CreateIndex(
                name: "IX_ExtractedFields_ExtractedItemId",
                table: "ExtractedFields",
                column: "ExtractedItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ExtractedItems_MedicalDocumentId_SequenceNumber",
                table: "ExtractedItems",
                columns: new[] { "MedicalDocumentId", "SequenceNumber" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ExtractedFields_ExtractedItems_ExtractedItemId",
                table: "ExtractedFields",
                column: "ExtractedItemId",
                principalTable: "ExtractedItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExtractedFields_ExtractedItems_ExtractedItemId",
                table: "ExtractedFields");

            migrationBuilder.AddColumn<Guid>(
                name: "DocumentId",
                table: "ExtractedFields",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FieldGroup",
                table: "ExtractedFields",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                """
                UPDATE fields
                SET
                    fields.DocumentId = items.MedicalDocumentId,
                    fields.FieldGroup = items.ItemType
                FROM ExtractedFields AS fields
                INNER JOIN ExtractedItems AS items
                    ON items.Id = fields.ExtractedItemId;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "DocumentId",
                table: "ExtractedFields",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.DropIndex(
                name: "IX_ExtractedFields_ExtractedItemId",
                table: "ExtractedFields");

            migrationBuilder.DropColumn(
                name: "ExtractedItemId",
                table: "ExtractedFields");

            migrationBuilder.DropTable(
                name: "ExtractedItems");

            migrationBuilder.DropColumn(
                name: "ExtractionStatus",
                table: "MedicalDocuments");

            migrationBuilder.DropColumn(
                name: "FailureCode",
                table: "MedicalDocuments");

            migrationBuilder.DropColumn(
                name: "EvidenceText",
                table: "ExtractedFields");

            migrationBuilder.DropColumn(
                name: "Issues",
                table: "ExtractedFields");

            migrationBuilder.CreateIndex(
                name: "IX_ExtractedFields_DocumentId",
                table: "ExtractedFields",
                column: "DocumentId");

            migrationBuilder.AddForeignKey(
                name: "FK_ExtractedFields_MedicalDocuments_DocumentId",
                table: "ExtractedFields",
                column: "DocumentId",
                principalTable: "MedicalDocuments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
