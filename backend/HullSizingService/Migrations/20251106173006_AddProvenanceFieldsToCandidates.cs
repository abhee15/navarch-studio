using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HullSizingService.Migrations
{
    /// <inheritdoc />
    public partial class AddProvenanceFieldsToCandidates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "reference_vessel_id",
                schema: "sizing",
                table: "candidate_designs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reference_vessel_name",
                schema: "sizing",
                table: "candidate_designs",
                type: "text",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "similarity_score",
                schema: "sizing",
                table: "candidate_designs",
                type: "numeric(4,3)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "solver_mode",
                schema: "sizing",
                table: "candidate_designs",
                type: "text",
                maxLength: 50,
                nullable: true);

            // Create index on solver_mode for filtering
            migrationBuilder.Sql(@"
                CREATE INDEX idx_candidate_solver_mode 
                ON sizing.candidate_designs(solver_mode) 
                WHERE solver_mode IS NOT NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "reference_vessel_id",
                schema: "sizing",
                table: "candidate_designs");

            migrationBuilder.DropColumn(
                name: "reference_vessel_name",
                schema: "sizing",
                table: "candidate_designs");

            migrationBuilder.DropColumn(
                name: "similarity_score",
                schema: "sizing",
                table: "candidate_designs");

            migrationBuilder.DropColumn(
                name: "solver_mode",
                schema: "sizing",
                table: "candidate_designs");
        }
    }
}
