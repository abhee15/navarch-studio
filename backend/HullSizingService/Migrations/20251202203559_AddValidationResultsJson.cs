using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HullSizingService.Migrations
{
    /// <inheritdoc />
    public partial class AddValidationResultsJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "validation_results_json",
                schema: "sizing",
                table: "candidate_designs",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "validation_results_json",
                schema: "sizing",
                table: "candidate_designs");
        }
    }
}
