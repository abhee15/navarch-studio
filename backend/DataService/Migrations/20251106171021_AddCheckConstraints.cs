using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataService.Migrations
{
    /// <inheritdoc />
    public partial class AddCheckConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add CHECK constraints for loadcases
            migrationBuilder.Sql(@"
                ALTER TABLE vessels.loadcases
                ADD CONSTRAINT chk_draft_positive CHECK (draft_m > 0),
                ADD CONSTRAINT chk_trim_range CHECK (trim_angle_deg BETWEEN -10 AND 10);
            ");

            // Add CHECK constraints for vessels
            migrationBuilder.Sql(@"
                ALTER TABLE vessels.vessels
                ADD CONSTRAINT chk_dimensions_positive CHECK (lpp_m > 0 AND beam_m > 0);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove CHECK constraints
            migrationBuilder.Sql(@"
                ALTER TABLE vessels.loadcases
                DROP CONSTRAINT IF EXISTS chk_draft_positive,
                DROP CONSTRAINT IF EXISTS chk_trim_range;
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE vessels.vessels
                DROP CONSTRAINT IF EXISTS chk_dimensions_positive;
            ");
        }
    }
}
