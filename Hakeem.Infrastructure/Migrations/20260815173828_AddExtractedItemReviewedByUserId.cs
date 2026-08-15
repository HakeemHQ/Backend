using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hakeem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExtractedItemReviewedByUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ReviewedByUserId",
                table: "ExtractedItems",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReviewedByUserId",
                table: "ExtractedItems");
        }
    }
}
