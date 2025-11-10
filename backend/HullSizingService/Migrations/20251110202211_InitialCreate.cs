using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HullSizingService.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "sizing");

            migrationBuilder.CreateTable(
                name: "hull_family_presets",
                schema: "sizing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    family = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    display_name = table.Column<string>(type: "text", nullable: true),
                    l_over_b_min = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    l_over_b_max = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    b_over_t_min = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    b_over_t_max = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    d_over_t_min = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    d_over_t_max = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    cb_min = table.Column<decimal>(type: "numeric(5,3)", nullable: false),
                    cb_max = table.Column<decimal>(type: "numeric(5,3)", nullable: false),
                    cp_min = table.Column<decimal>(type: "numeric(5,3)", nullable: true),
                    cp_max = table.Column<decimal>(type: "numeric(5,3)", nullable: true),
                    cwp_min = table.Column<decimal>(type: "numeric(5,3)", nullable: true),
                    cwp_max = table.Column<decimal>(type: "numeric(5,3)", nullable: true),
                    fn_min = table.Column<decimal>(type: "numeric(5,3)", nullable: true),
                    fn_max = table.Column<decimal>(type: "numeric(5,3)", nullable: true),
                    generator_type = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_hull_family_presets", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "iso_containers",
                schema: "sizing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    container_type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    length_mm = table.Column<int>(type: "integer", nullable: false),
                    width_mm = table.Column<int>(type: "integer", nullable: false),
                    height_mm = table.Column<int>(type: "integer", nullable: false),
                    max_gross_kg = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_iso_containers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "kpi_weights",
                schema: "sizing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    metric = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    weight = table.Column<decimal>(type: "numeric(5,3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_kpi_weights", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "mission_cases",
                schema: "sizing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    mission_category = table.Column<string>(type: "text", nullable: true),
                    mission_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    cargo_basis = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    cargo_value = table.Column<decimal>(type: "numeric(12,2)", nullable: true),
                    cargo_volume_m3 = table.Column<decimal>(type: "numeric(12,2)", nullable: true),
                    cargo_density_t_per_m3 = table.Column<decimal>(type: "numeric(6,3)", nullable: true),
                    teu_count = table.Column<int>(type: "integer", nullable: true),
                    service_speed_kn = table.Column<decimal>(type: "numeric(6,2)", nullable: false),
                    sea_margin_pct = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    service_margin_pct = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    env_hs_m = table.Column<decimal>(type: "numeric(6,2)", nullable: true),
                    env_tz_s = table.Column<decimal>(type: "numeric(6,2)", nullable: true),
                    cap_loa_m = table.Column<decimal>(type: "numeric(8,2)", nullable: true),
                    cap_beam_m = table.Column<decimal>(type: "numeric(8,2)", nullable: true),
                    cap_draft_m = table.Column<decimal>(type: "numeric(6,2)", nullable: true),
                    cap_airdraft_m = table.Column<decimal>(type: "numeric(6,2)", nullable: true),
                    endurance_nm = table.Column<decimal>(type: "numeric(8,2)", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mission_cases", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vessel_catalog",
                schema: "sizing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    provenance = table.Column<string>(type: "text", nullable: true),
                    vessel_type = table.Column<string>(type: "text", nullable: true),
                    lpp_m = table.Column<decimal>(type: "numeric(12,4)", nullable: true),
                    lwl_m = table.Column<decimal>(type: "numeric(12,4)", nullable: true),
                    bm = table.Column<decimal>(type: "numeric(12,4)", nullable: true),
                    tm = table.Column<decimal>(type: "numeric(12,4)", nullable: true),
                    dm = table.Column<decimal>(type: "numeric(12,4)", nullable: true),
                    cb = table.Column<decimal>(type: "numeric(6,4)", nullable: true),
                    cp = table.Column<decimal>(type: "numeric(6,4)", nullable: true),
                    cwp = table.Column<decimal>(type: "numeric(6,4)", nullable: true),
                    cm = table.Column<decimal>(type: "numeric(6,4)", nullable: true),
                    dwt_t = table.Column<decimal>(type: "numeric(12,2)", nullable: true),
                    service_speed_kn = table.Column<decimal>(type: "numeric(6,2)", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    source_url = table.Column<string>(type: "text", nullable: true),
                    license_info = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vessel_catalog", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sizing_runs",
                schema: "sizing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    mission_case_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    locks_json = table.Column<string>(type: "jsonb", nullable: true),
                    options_json = table.Column<string>(type: "jsonb", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    compute_time_ms = table.Column<int>(type: "integer", nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    diagnostics_json = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sizing_runs", x => x.id);
                    table.ForeignKey(
                        name: "fk_sizing_runs_mission_cases_mission_case_id",
                        column: x => x.mission_case_id,
                        principalSchema: "sizing",
                        principalTable: "mission_cases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "candidate_designs",
                schema: "sizing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sizing_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    hull_family = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    rank = table.Column<int>(type: "integer", nullable: false),
                    is_selected = table.Column<bool>(type: "boolean", nullable: false),
                    lpp_m = table.Column<decimal>(type: "numeric(12,4)", nullable: false),
                    lwl_m = table.Column<decimal>(type: "numeric(12,4)", nullable: false),
                    loa_m = table.Column<decimal>(type: "numeric(12,4)", nullable: false),
                    bm = table.Column<decimal>(type: "numeric(12,4)", nullable: false),
                    tm = table.Column<decimal>(type: "numeric(12,4)", nullable: false),
                    dm = table.Column<decimal>(type: "numeric(12,4)", nullable: false),
                    cb = table.Column<decimal>(type: "numeric(6,4)", nullable: false),
                    cp = table.Column<decimal>(type: "numeric(6,4)", nullable: false),
                    cwp = table.Column<decimal>(type: "numeric(6,4)", nullable: false),
                    cm = table.Column<decimal>(type: "numeric(6,4)", nullable: true),
                    displacement_t = table.Column<decimal>(type: "numeric(12,3)", nullable: false),
                    fn = table.Column<decimal>(type: "numeric(6,4)", nullable: false),
                    lwl_over_lambda = table.Column<decimal>(type: "numeric(6,3)", nullable: true),
                    ehp_kw = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    shp_kw = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    gm_est_m = table.Column<decimal>(type: "numeric(8,3)", nullable: true),
                    kb_m = table.Column<decimal>(type: "numeric(8,3)", nullable: true),
                    lcb_pct_lpp = table.Column<decimal>(type: "numeric(6,3)", nullable: true),
                    scores_json = table.Column<string>(type: "jsonb", nullable: true),
                    flags_json = table.Column<string>(type: "jsonb", nullable: true),
                    score = table.Column<decimal>(type: "numeric(8,4)", nullable: false),
                    geometry_json = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reference_vessel_id = table.Column<string>(type: "text", nullable: true),
                    reference_vessel_name = table.Column<string>(type: "text", nullable: true),
                    similarity_score = table.Column<decimal>(type: "numeric", nullable: true),
                    solver_mode = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_candidate_designs", x => x.id);
                    table.ForeignKey(
                        name: "fk_candidate_designs_sizing_runs_sizing_run_id",
                        column: x => x.sizing_run_id,
                        principalSchema: "sizing",
                        principalTable: "sizing_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "push_operations",
                schema: "sizing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    candidate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vessel_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_push_operations", x => x.id);
                    table.ForeignKey(
                        name: "fk_push_operations_candidate_designs_candidate_id",
                        column: x => x.candidate_id,
                        principalSchema: "sizing",
                        principalTable: "candidate_designs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_candidate_designs_hull_family",
                schema: "sizing",
                table: "candidate_designs",
                column: "hull_family");

            migrationBuilder.CreateIndex(
                name: "ix_candidate_designs_rank",
                schema: "sizing",
                table: "candidate_designs",
                column: "rank");

            migrationBuilder.CreateIndex(
                name: "ix_candidate_designs_score",
                schema: "sizing",
                table: "candidate_designs",
                column: "score",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "ix_candidate_designs_sizing_run_id",
                schema: "sizing",
                table: "candidate_designs",
                column: "sizing_run_id");

            migrationBuilder.CreateIndex(
                name: "ix_hull_family_presets_family",
                schema: "sizing",
                table: "hull_family_presets",
                column: "family",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_hull_family_presets_is_active",
                schema: "sizing",
                table: "hull_family_presets",
                column: "is_active",
                filter: "is_active = true");

            migrationBuilder.CreateIndex(
                name: "ix_kpi_weights_user_id_metric",
                schema: "sizing",
                table: "kpi_weights",
                columns: new[] { "user_id", "metric" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_mission_cases_mission_type",
                schema: "sizing",
                table: "mission_cases",
                column: "mission_type",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_mission_cases_name_tenant_id",
                schema: "sizing",
                table: "mission_cases",
                columns: new[] { "name", "tenant_id" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_mission_cases_tenant_id",
                schema: "sizing",
                table: "mission_cases",
                column: "tenant_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_mission_cases_user_id",
                schema: "sizing",
                table: "mission_cases",
                column: "user_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_push_operations_candidate_id",
                schema: "sizing",
                table: "push_operations",
                column: "candidate_id");

            migrationBuilder.CreateIndex(
                name: "ix_push_operations_idempotency_key",
                schema: "sizing",
                table: "push_operations",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sizing_runs_created_at",
                schema: "sizing",
                table: "sizing_runs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_sizing_runs_mission_case_id",
                schema: "sizing",
                table: "sizing_runs",
                column: "mission_case_id");

            migrationBuilder.CreateIndex(
                name: "ix_sizing_runs_status",
                schema: "sizing",
                table: "sizing_runs",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_vessel_catalog_provenance",
                schema: "sizing",
                table: "vessel_catalog",
                column: "provenance");

            migrationBuilder.CreateIndex(
                name: "ix_vessel_catalog_vessel_type",
                schema: "sizing",
                table: "vessel_catalog",
                column: "vessel_type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "hull_family_presets",
                schema: "sizing");

            migrationBuilder.DropTable(
                name: "iso_containers",
                schema: "sizing");

            migrationBuilder.DropTable(
                name: "kpi_weights",
                schema: "sizing");

            migrationBuilder.DropTable(
                name: "push_operations",
                schema: "sizing");

            migrationBuilder.DropTable(
                name: "vessel_catalog",
                schema: "sizing");

            migrationBuilder.DropTable(
                name: "candidate_designs",
                schema: "sizing");

            migrationBuilder.DropTable(
                name: "sizing_runs",
                schema: "sizing");

            migrationBuilder.DropTable(
                name: "mission_cases",
                schema: "sizing");
        }
    }
}
