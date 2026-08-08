using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BomberosAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHealthPersonnelStatusResearchMarkersAndAuditResource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "HealthPersonnel",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<decimal>(
                name: "lactate_post_mmol",
                table: "BioimpedanceMeasurement",
                type: "decimal(4,2)",
                precision: 4,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "lactate_pre_mmol",
                table: "BioimpedanceMeasurement",
                type: "decimal(4,2)",
                precision: 4,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "metabolic_age_years",
                table: "BioimpedanceMeasurement",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "stroop_errors",
                table: "BioimpedanceMeasurement",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "stroop_time_seconds",
                table: "BioimpedanceMeasurement",
                type: "decimal(6,2)",
                precision: 6,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "resource_id",
                table: "AccessAudit",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "resource_type",
                table: "AccessAudit",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            // NOTA: EF generó aquí también un CreateIndex/AddForeignKey para
            // HealthPersonnel.user_id y TraineeFirefighter.user_id, detectando que el
            // modelo (navigation property `User`) no tenía FK/índice registrado en el
            // snapshot. La base local (creada desde schema.sql) YA tiene ambos, con
            // nombres distintos (FK_HealthPersonnel_User / FK_TraineeFirefighter_User
            // e IX_HealthPersonnel_user_id / IX_TraineeFirefighter_user_id) — aplicar
            // esas sentencias fallaba con "ya existe". Se removieron de este Up()/Down()
            // por ser drift preexistente ajeno a este cambio, no columnas nuevas.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_active",
                table: "HealthPersonnel");

            migrationBuilder.DropColumn(
                name: "lactate_post_mmol",
                table: "BioimpedanceMeasurement");

            migrationBuilder.DropColumn(
                name: "lactate_pre_mmol",
                table: "BioimpedanceMeasurement");

            migrationBuilder.DropColumn(
                name: "metabolic_age_years",
                table: "BioimpedanceMeasurement");

            migrationBuilder.DropColumn(
                name: "stroop_errors",
                table: "BioimpedanceMeasurement");

            migrationBuilder.DropColumn(
                name: "stroop_time_seconds",
                table: "BioimpedanceMeasurement");

            migrationBuilder.DropColumn(
                name: "resource_id",
                table: "AccessAudit");

            migrationBuilder.DropColumn(
                name: "resource_type",
                table: "AccessAudit");
        }
    }
}
