using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataService.Migrations
{
    /// <inheritdoc />
    public partial class AddComparisonSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "comparison_snapshots",
                schema: "data",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vessel_id = table.Column<Guid>(type: "uuid", nullable: false),
                    loadcase_id = table.Column<Guid>(type: "uuid", nullable: true),
                    run_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_baseline = table.Column<bool>(type: "boolean", nullable: false),
                    vessel_lpp = table.Column<decimal>(type: "numeric(10,3)", nullable: false),
                    vessel_beam = table.Column<decimal>(type: "numeric(10,3)", nullable: false),
                    vessel_design_draft = table.Column<decimal>(type: "numeric(10,3)", nullable: false),
                    loadcase_rho = table.Column<decimal>(type: "numeric(10,3)", nullable: true),
                    loadcase_kg = table.Column<decimal>(type: "numeric(10,3)", nullable: true),
                    min_draft = table.Column<decimal>(type: "numeric(10,3)", nullable: false),
                    max_draft = table.Column<decimal>(type: "numeric(10,3)", nullable: false),
                    draft_step = table.Column<decimal>(type: "numeric(10,3)", nullable: false),
                    results_json = table.Column<string>(type: "text", nullable: false),
                    computation_time_ms = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    tags = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_comparison_snapshots", x => x.id);
                    table.ForeignKey(
                        name: "fk_comparison_snapshots_loadcases_loadcase_id",
                        column: x => x.loadcase_id,
                        principalSchema: "data",
                        principalTable: "loadcases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_comparison_snapshots_vessels_vessel_id",
                        column: x => x.vessel_id,
                        principalSchema: "data",
                        principalTable: "vessels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_comparison_snapshots_loadcase_id",
                schema: "data",
                table: "comparison_snapshots",
                column: "loadcase_id");

            migrationBuilder.CreateIndex(
                name: "ix_comparison_snapshots_user_id",
                schema: "data",
                table: "comparison_snapshots",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_comparison_snapshots_vessel_id",
                schema: "data",
                table: "comparison_snapshots",
                column: "vessel_id");

            migrationBuilder.CreateIndex(
                name: "ix_comparison_snapshots_vessel_id_is_baseline",
                schema: "data",
                table: "comparison_snapshots",
                columns: new[] { "vessel_id", "is_baseline" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "comparison_snapshots",
                schema: "data");
        }
    }
}
