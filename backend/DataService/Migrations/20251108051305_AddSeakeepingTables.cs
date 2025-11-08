using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataService.Migrations
{
    /// <inheritdoc />
    public partial class AddSeakeepingTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "rao_results",
                schema: "data",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    vessel_id = table.Column<Guid>(type: "uuid", nullable: false),
                    loadcase_id = table.Column<Guid>(type: "uuid", nullable: false),
                    frequency = table.Column<double[]>(type: "double precision[]", nullable: false),
                    heave_rao = table.Column<double[]>(type: "double precision[]", nullable: false),
                    pitch_rao = table.Column<double[]>(type: "double precision[]", nullable: false),
                    roll_rao = table.Column<double[]>(type: "double precision[]", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rao_results", x => x.id);
                    table.ForeignKey(
                        name: "fk_rao_results_loadcases_loadcase_id",
                        column: x => x.loadcase_id,
                        principalSchema: "data",
                        principalTable: "loadcases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_rao_results_vessels_vessel_id",
                        column: x => x.vessel_id,
                        principalSchema: "data",
                        principalTable: "vessels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "motion_responses",
                schema: "data",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    rao_result_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sea_state_hs = table.Column<double>(type: "numeric(8,3)", nullable: false),
                    sea_state_tp = table.Column<double>(type: "numeric(8,3)", nullable: false),
                    sea_state_heading = table.Column<double>(type: "numeric(6,2)", nullable: false),
                    sea_state_spectrum = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    sea_state_gamma = table.Column<double>(type: "numeric(6,3)", nullable: false),
                    significant_heave = table.Column<double>(type: "numeric(10,4)", nullable: false),
                    significant_pitch = table.Column<double>(type: "numeric(10,4)", nullable: false),
                    significant_roll = table.Column<double>(type: "numeric(10,4)", nullable: false),
                    heave_mean_period = table.Column<double>(type: "numeric(10,4)", nullable: false),
                    pitch_mean_period = table.Column<double>(type: "numeric(10,4)", nullable: false),
                    roll_mean_period = table.Column<double>(type: "numeric(10,4)", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_motion_responses", x => x.id);
                    table.ForeignKey(
                        name: "fk_motion_responses_rao_results_rao_result_id",
                        column: x => x.rao_result_id,
                        principalSchema: "data",
                        principalTable: "rao_results",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_vessels_real_lpp_m_beam_m_cb",
                schema: "catalog_user",
                table: "vessels_real",
                columns: new[] { "lpp_m", "beam_m", "cb" });

            migrationBuilder.CreateIndex(
                name: "ix_vessels_real_vessel_id",
                schema: "catalog_user",
                table: "vessels_real",
                column: "vessel_id");

            migrationBuilder.CreateIndex(
                name: "ix_vessels_real_vessel_type",
                schema: "catalog_user",
                table: "vessels_real",
                column: "vessel_type");

            migrationBuilder.CreateIndex(
                name: "ix_parametric_hulls_dataset_source",
                schema: "catalog_ml",
                table: "parametric_hulls",
                column: "dataset_source");

            migrationBuilder.CreateIndex(
                name: "ix_parametric_hulls_hull_id",
                schema: "catalog_ml",
                table: "parametric_hulls",
                column: "hull_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_parametric_hulls_is_active",
                schema: "catalog_ml",
                table: "parametric_hulls",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_parametric_hulls_volume_norm_lcb_norm",
                schema: "catalog_ml",
                table: "parametric_hulls",
                columns: new[] { "volume_norm", "lcb_norm" });

            migrationBuilder.CreateIndex(
                name: "ix_benchmark_test_conditions_hull_name",
                schema: "catalog_real",
                table: "benchmark_test_conditions",
                column: "hull_name");

            migrationBuilder.CreateIndex(
                name: "ix_benchmark_test_conditions_test_type",
                schema: "catalog_real",
                table: "benchmark_test_conditions",
                column: "test_type");

            migrationBuilder.CreateIndex(
                name: "ix_motion_responses_rao_result_id",
                schema: "data",
                table: "motion_responses",
                column: "rao_result_id");

            migrationBuilder.CreateIndex(
                name: "ix_rao_results_loadcase_id",
                schema: "data",
                table: "rao_results",
                column: "loadcase_id");

            migrationBuilder.CreateIndex(
                name: "ix_rao_results_vessel_id",
                schema: "data",
                table: "rao_results",
                column: "vessel_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "motion_responses",
                schema: "data");

            migrationBuilder.DropTable(
                name: "rao_results",
                schema: "data");

            migrationBuilder.DropIndex(
                name: "ix_vessels_real_lpp_m_beam_m_cb",
                schema: "catalog_user",
                table: "vessels_real");

            migrationBuilder.DropIndex(
                name: "ix_vessels_real_vessel_id",
                schema: "catalog_user",
                table: "vessels_real");

            migrationBuilder.DropIndex(
                name: "ix_vessels_real_vessel_type",
                schema: "catalog_user",
                table: "vessels_real");

            migrationBuilder.DropIndex(
                name: "ix_parametric_hulls_dataset_source",
                schema: "catalog_ml",
                table: "parametric_hulls");

            migrationBuilder.DropIndex(
                name: "ix_parametric_hulls_hull_id",
                schema: "catalog_ml",
                table: "parametric_hulls");

            migrationBuilder.DropIndex(
                name: "ix_parametric_hulls_is_active",
                schema: "catalog_ml",
                table: "parametric_hulls");

            migrationBuilder.DropIndex(
                name: "ix_parametric_hulls_volume_norm_lcb_norm",
                schema: "catalog_ml",
                table: "parametric_hulls");

            migrationBuilder.DropIndex(
                name: "ix_benchmark_test_conditions_hull_name",
                schema: "catalog_real",
                table: "benchmark_test_conditions");

            migrationBuilder.DropIndex(
                name: "ix_benchmark_test_conditions_test_type",
                schema: "catalog_real",
                table: "benchmark_test_conditions");
        }
    }
}
