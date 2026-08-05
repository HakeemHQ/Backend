using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hakeem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMedicalRecordFieldEditRelatedDocument : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FieldReviews_MedicalRecords_MedicalRecordId",
                table: "FieldReviews");

            migrationBuilder.DropIndex(
                name: "IX_FieldReviews_MedicalRecordId",
                table: "FieldReviews");

            migrationBuilder.DropColumn(
                name: "MedicalRecordId",
                table: "FieldReviews");

            migrationBuilder.AddColumn<Guid>(
                name: "SourceExtractedItemId",
                table: "MedicalRecords",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "ReviewedAt",
                table: "FieldReviews",
                type: "datetimeoffset",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<int>(
                name: "Decision",
                table: "FieldReviews",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "CorrectedValue",
                table: "FieldReviews",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "ReviewStatus",
                table: "ExtractedItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReviewedAt",
                table: "ExtractedItems",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MedicalRecordFields",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MedicalRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceExtractedFieldId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FieldName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalRecordFields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicalRecordFields_ExtractedFields_SourceExtractedFieldId",
                        column: x => x.SourceExtractedFieldId,
                        principalTable: "ExtractedFields",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MedicalRecordFields_MedicalRecords_MedicalRecordId",
                        column: x => x.MedicalRecordId,
                        principalTable: "MedicalRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MedicalRecords_SourceExtractedItemId",
                table: "MedicalRecords",
                column: "SourceExtractedItemId",
                unique: true,
                filter: "[SourceExtractedItemId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalRecordFields_MedicalRecordId_FieldName",
                table: "MedicalRecordFields",
                columns: new[] { "MedicalRecordId", "FieldName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MedicalRecordFields_SourceExtractedFieldId",
                table: "MedicalRecordFields",
                column: "SourceExtractedFieldId");

            migrationBuilder.AddForeignKey(
                name: "FK_MedicalRecords_ExtractedItems_SourceExtractedItemId",
                table: "MedicalRecords",
                column: "SourceExtractedItemId",
                principalTable: "ExtractedItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MedicalRecords_ExtractedItems_SourceExtractedItemId",
                table: "MedicalRecords");

            migrationBuilder.DropTable(
                name: "MedicalRecordFields");

            migrationBuilder.DropIndex(
                name: "IX_MedicalRecords_SourceExtractedItemId",
                table: "MedicalRecords");

            migrationBuilder.DropColumn(
                name: "SourceExtractedItemId",
                table: "MedicalRecords");

            migrationBuilder.DropColumn(
                name: "ReviewStatus",
                table: "ExtractedItems");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "ExtractedItems");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ReviewedAt",
                table: "FieldReviews",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset");

            migrationBuilder.AlterColumn<string>(
                name: "Decision",
                table: "FieldReviews",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "CorrectedValue",
                table: "FieldReviews",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MedicalRecordId",
                table: "FieldReviews",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_FieldReviews_MedicalRecordId",
                table: "FieldReviews",
                column: "MedicalRecordId");

            migrationBuilder.AddForeignKey(
                name: "FK_FieldReviews_MedicalRecords_MedicalRecordId",
                table: "FieldReviews",
                column: "MedicalRecordId",
                principalTable: "MedicalRecords",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
