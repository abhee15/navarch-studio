using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HullSizingService.Migrations
{
    /// <inheritdoc />
    public partial class AddGeometryGenerationStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "geometry_generation_error",
                schema: "sizing",
                table: "candidate_designs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "geometry_generation_status",
                schema: "sizing",
                table: "candidate_designs",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "geometry_generation_error",
                schema: "sizing",
                table: "candidate_designs");

            migrationBuilder.DropColumn(
                name: "geometry_generation_status",
                schema: "sizing",
                table: "candidate_designs");
        }
    }
}
