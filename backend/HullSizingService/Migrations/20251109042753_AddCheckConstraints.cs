using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HullSizingService.Migrations
{
    /// <inheritdoc />
    public partial class AddCheckConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Mission Cases - Validate mission requirements
            migrationBuilder.Sql(@"
                ALTER TABLE sizing.mission_cases
                ADD CONSTRAINT chk_mission_service_speed_positive
                    CHECK (service_speed_kn > 0 AND service_speed_kn <= 50),
                ADD CONSTRAINT chk_mission_sea_margin_range
                    CHECK (sea_margin_pct BETWEEN 0 AND 100),
                ADD CONSTRAINT chk_mission_service_margin_range
                    CHECK (service_margin_pct BETWEEN 0 AND 100),
                ADD CONSTRAINT chk_mission_cargo_value_positive
                    CHECK (cargo_value IS NULL OR cargo_value > 0),
                ADD CONSTRAINT chk_mission_cargo_volume_positive
                    CHECK (cargo_volume_m3 IS NULL OR cargo_volume_m3 > 0),
                ADD CONSTRAINT chk_mission_teu_positive
                    CHECK (teu_count IS NULL OR teu_count > 0),
                ADD CONSTRAINT chk_mission_dimensions_positive
                    CHECK (
                        (cap_loa_m IS NULL OR cap_loa_m > 0) AND
                        (cap_beam_m IS NULL OR cap_beam_m > 0) AND
                        (cap_draft_m IS NULL OR cap_draft_m > 0) AND
                        (cap_airdraft_m IS NULL OR cap_airdraft_m > 0)
                    );
            ");

            // Candidate Designs - Validate hull dimensions and coefficients
            // NOTE: Column names are bm, tm, dm (not beam_m, draft_m, depth_m) due to snake_case convention
            migrationBuilder.Sql(@"
                ALTER TABLE sizing.candidate_designs
                ADD CONSTRAINT chk_candidate_dimensions_positive
                    CHECK (lpp_m > 0 AND bm > 0 AND tm > 0 AND dm > 0),
                ADD CONSTRAINT chk_candidate_coefficients_range
                    CHECK (
                        cb BETWEEN 0.25 AND 0.95 AND
                        (cp IS NULL OR cp BETWEEN 0.5 AND 1.0) AND
                        (cwp IS NULL OR cwp BETWEEN 0.5 AND 1.0) AND
                        (cm IS NULL OR cm BETWEEN 0.7 AND 1.0)
                    ),
                ADD CONSTRAINT chk_candidate_disp_positive
                    CHECK (displacement_t > 0),
                ADD CONSTRAINT chk_candidate_speed_positive
                    CHECK (fn > 0 AND fn <= 1.0),
                ADD CONSTRAINT chk_candidate_resistance_non_negative
                    CHECK (ehp_kw IS NULL OR ehp_kw >= 0),
                ADD CONSTRAINT chk_candidate_gm_reasonable
                    CHECK (gm_est_m IS NULL OR (gm_est_m > -5.0 AND gm_est_m < 10.0)),
                ADD CONSTRAINT chk_candidate_score_range
                    CHECK (score BETWEEN 0 AND 1);
            ");

            // Sizing Runs - Validate solver options
            migrationBuilder.Sql(@"
                ALTER TABLE sizing.sizing_runs
                ADD CONSTRAINT chk_run_max_candidates_range
                    CHECK (max_candidates BETWEEN 1 AND 20),
                ADD CONSTRAINT chk_run_compute_time_non_negative
                    CHECK (compute_time_ms IS NULL OR compute_time_ms >= 0);
            ");

            // Hull Family Presets - Validate ratio bounds
            migrationBuilder.Sql(@"
                ALTER TABLE sizing.hull_family_presets
                ADD CONSTRAINT chk_family_ratios_positive
                    CHECK (
                        l_over_b_min > 0 AND l_over_b_max > l_over_b_min AND
                        b_over_t_min > 0 AND b_over_t_max > b_over_t_min AND
                        d_over_t_min > 0 AND d_over_t_max > d_over_t_min
                    ),
                ADD CONSTRAINT chk_family_coefficients_range
                    CHECK (
                        cb_min BETWEEN 0.25 AND 0.95 AND
                        cb_max BETWEEN cb_min AND 0.95 AND
                        (fn_min IS NULL OR fn_min BETWEEN 0.05 AND 0.70) AND
                        (fn_max IS NULL OR fn_max BETWEEN fn_min AND 0.70)
                    );
            ");

            // ISO Containers - Validate dimensions
            migrationBuilder.Sql(@"
                ALTER TABLE sizing.iso_containers
                ADD CONSTRAINT chk_container_dimensions_positive
                    CHECK (length_mm > 0 AND width_mm > 0 AND height_mm > 0),
                ADD CONSTRAINT chk_container_gross_positive
                    CHECK (max_gross_kg > 0);
            ");

            // KPI Weights - Validate weight values
            migrationBuilder.Sql(@"
                ALTER TABLE sizing.kpi_weights
                ADD CONSTRAINT chk_kpi_weight_range
                    CHECK (weight BETWEEN 0 AND 1);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove all CHECK constraints in reverse order
            migrationBuilder.Sql("ALTER TABLE sizing.kpi_weights DROP CONSTRAINT IF EXISTS chk_kpi_weight_range;");

            migrationBuilder.Sql("ALTER TABLE sizing.iso_containers DROP CONSTRAINT IF EXISTS chk_container_dimensions_positive;");
            migrationBuilder.Sql("ALTER TABLE sizing.iso_containers DROP CONSTRAINT IF EXISTS chk_container_gross_positive;");

            migrationBuilder.Sql("ALTER TABLE sizing.hull_family_presets DROP CONSTRAINT IF EXISTS chk_family_ratios_positive;");
            migrationBuilder.Sql("ALTER TABLE sizing.hull_family_presets DROP CONSTRAINT IF EXISTS chk_family_coefficients_range;");

            migrationBuilder.Sql("ALTER TABLE sizing.sizing_runs DROP CONSTRAINT IF EXISTS chk_run_max_candidates_range;");
            migrationBuilder.Sql("ALTER TABLE sizing.sizing_runs DROP CONSTRAINT IF EXISTS chk_run_compute_time_non_negative;");

            // Remove candidate_designs constraints
            migrationBuilder.Sql("ALTER TABLE sizing.candidate_designs DROP CONSTRAINT IF EXISTS chk_candidate_dimensions_positive;");
            migrationBuilder.Sql("ALTER TABLE sizing.candidate_designs DROP CONSTRAINT IF EXISTS chk_candidate_coefficients_range;");
            migrationBuilder.Sql("ALTER TABLE sizing.candidate_designs DROP CONSTRAINT IF EXISTS chk_candidate_disp_positive;");
            migrationBuilder.Sql("ALTER TABLE sizing.candidate_designs DROP CONSTRAINT IF EXISTS chk_candidate_speed_positive;");
            migrationBuilder.Sql("ALTER TABLE sizing.candidate_designs DROP CONSTRAINT IF EXISTS chk_candidate_resistance_non_negative;");
            migrationBuilder.Sql("ALTER TABLE sizing.candidate_designs DROP CONSTRAINT IF EXISTS chk_candidate_gm_reasonable;");
            migrationBuilder.Sql("ALTER TABLE sizing.candidate_designs DROP CONSTRAINT IF EXISTS chk_candidate_score_range;");

            migrationBuilder.Sql("ALTER TABLE sizing.mission_cases DROP CONSTRAINT IF EXISTS chk_mission_service_speed_positive;");
            migrationBuilder.Sql("ALTER TABLE sizing.mission_cases DROP CONSTRAINT IF EXISTS chk_mission_sea_margin_range;");
            migrationBuilder.Sql("ALTER TABLE sizing.mission_cases DROP CONSTRAINT IF EXISTS chk_mission_service_margin_range;");
            migrationBuilder.Sql("ALTER TABLE sizing.mission_cases DROP CONSTRAINT IF EXISTS chk_mission_cargo_value_positive;");
            migrationBuilder.Sql("ALTER TABLE sizing.mission_cases DROP CONSTRAINT IF EXISTS chk_mission_cargo_volume_positive;");
            migrationBuilder.Sql("ALTER TABLE sizing.mission_cases DROP CONSTRAINT IF EXISTS chk_mission_teu_positive;");
            migrationBuilder.Sql("ALTER TABLE sizing.mission_cases DROP CONSTRAINT IF EXISTS chk_mission_dimensions_positive;");
        }
    }
}
