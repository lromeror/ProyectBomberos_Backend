using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BomberosAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVitalSignsPracticeContextFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "exposed_to_smoke_48h",
                table: "VitalSignsMeasurement",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_smoker",
                table: "VitalSignsMeasurement",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "practice_role",
                table: "VitalSignsMeasurement",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "exposed_to_smoke_48h",
                table: "VitalSignsMeasurement");

            migrationBuilder.DropColumn(
                name: "is_smoker",
                table: "VitalSignsMeasurement");

            migrationBuilder.DropColumn(
                name: "practice_role",
                table: "VitalSignsMeasurement");
        }
    }
}
