using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hakeem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentReviewStatuses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReviewStatus",
                table: "MedicalDocuments",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "NotReviewed");

            migrationBuilder.Sql(
                """
                UPDATE ExtractedItems
                SET ReviewStatus = 1
                WHERE ReviewStatus NOT IN (1, 2);

                UPDATE documents
                SET ReviewStatus =
                    CASE
                        WHEN NOT EXISTS (
                            SELECT 1
                            FROM ExtractedItems AS items
                            WHERE items.MedicalDocumentId = documents.Id)
                            THEN 'NotReviewed'
                        WHEN NOT EXISTS (
                            SELECT 1
                            FROM ExtractedItems AS items
                            WHERE items.MedicalDocumentId = documents.Id
                              AND items.ReviewStatus <> 2)
                            THEN 'FullyReviewed'
                        WHEN EXISTS (
                            SELECT 1
                            FROM ExtractedItems AS items
                            WHERE items.MedicalDocumentId = documents.Id
                              AND items.ReviewStatus = 2)
                            THEN 'PartiallyReviewed'
                        ELSE 'NotReviewed'
                    END
                FROM MedicalDocuments AS documents;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReviewStatus",
                table: "MedicalDocuments");
        }
    }
}
