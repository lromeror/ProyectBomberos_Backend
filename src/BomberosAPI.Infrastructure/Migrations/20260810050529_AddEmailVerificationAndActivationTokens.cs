using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BomberosAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailVerificationAndActivationTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "email_verified",
                table: "User",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "AccountActivationToken",
                columns: table => new
                {
                    account_activation_token_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    token_hash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    expires_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    used_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountActivationToken", x => x.account_activation_token_id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountActivationToken");

            migrationBuilder.DropColumn(
                name: "email_verified",
                table: "User");
        }
    }
}
