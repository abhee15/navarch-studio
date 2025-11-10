using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HullSizingService.Migrations
{
    /// <inheritdoc />
    public partial class FixGmConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the old constraint that was too restrictive (< 10.0)
            migrationBuilder.Sql(@"
                ALTER TABLE sizing.candidate_designs
                DROP CONSTRAINT IF EXISTS chk_candidate_gm_reasonable;
            ");

            // Add a new constraint with a more reasonable upper limit (< 20.0)
            // This allows for large vessels with higher metacentric heights
            migrationBuilder.Sql(@"
                ALTER TABLE sizing.candidate_designs
                ADD CONSTRAINT chk_candidate_gm_reasonable
                    CHECK (gm_est_m IS NULL OR (gm_est_m > -5.0 AND gm_est_m < 20.0));
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert to the old constraint (< 10.0)
            migrationBuilder.Sql(@"
                ALTER TABLE sizing.candidate_designs
                DROP CONSTRAINT IF EXISTS chk_candidate_gm_reasonable;
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE sizing.candidate_designs
                ADD CONSTRAINT chk_candidate_gm_reasonable
                    CHECK (gm_est_m IS NULL OR (gm_est_m > -5.0 AND gm_est_m < 10.0));
            ");
        }
    }
}
