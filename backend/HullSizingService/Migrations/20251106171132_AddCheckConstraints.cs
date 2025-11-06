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
            // Add CHECK constraints for mission_cases
            migrationBuilder.Sql(@"
                ALTER TABLE sizing.mission_cases
                ADD CONSTRAINT chk_cargo_value_positive CHECK (cargo_value > 0),
                ADD CONSTRAINT chk_speed_positive CHECK (service_speed_kn > 0);
            ");

            // Add CHECK constraints for candidate_designs
            migrationBuilder.Sql(@"
                ALTER TABLE sizing.candidate_designs
                ADD CONSTRAINT chk_dimensions_positive CHECK (lpp_m > 0 AND b_m > 0 AND t_m > 0),
                ADD CONSTRAINT chk_coefficients_range CHECK (cb BETWEEN 0.3 AND 0.95 AND cp BETWEEN 0.5 AND 1.0);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove CHECK constraints
            migrationBuilder.Sql(@"
                ALTER TABLE sizing.mission_cases
                DROP CONSTRAINT IF EXISTS chk_cargo_value_positive,
                DROP CONSTRAINT IF EXISTS chk_speed_positive;
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE sizing.candidate_designs
                DROP CONSTRAINT IF EXISTS chk_dimensions_positive,
                DROP CONSTRAINT IF EXISTS chk_coefficients_range;
            ");
        }
    }
}
