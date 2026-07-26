using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hakeem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedPasswordResetEmailTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "EmailTemplates",
                columns: new[] { "Id", "Body", "CreatedAt", "IsActive", "RecipientKey", "Subject", "TemplateKey", "TemplateName", "UpdatedAt" },
                values: new object[] { 1, "<p>Hello {{UserName}},</p>\r\n\r\n<p>We received a request to reset your Hakeem password.</p>\r\n\r\n<p>\r\n    <a href=\"{{ResetLink}}\">Reset your password</a>\r\n</p>\r\n\r\n<p>\r\n    If you did not request a password reset,\r\n    you can safely ignore this email.\r\n</p>", new DateTime(2026, 7, 26, 0, 0, 0, 0, DateTimeKind.Utc), true, null, "Reset your Hakeem password", 2, "PasswordReset", null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "EmailTemplates",
                keyColumn: "Id",
                keyValue: 1);
        }
    }
}
