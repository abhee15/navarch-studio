using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataService.Migrations
{
    /// <inheritdoc />
    public partial class AddHullSizingMetadataToVesselMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "depth",
                schema: "data",
                table: "vessel_metadata",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ehp_kw",
                schema: "data",
                table: "vessel_metadata",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "froude_number",
                schema: "data",
                table: "vessel_metadata",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "gm_initial",
                schema: "data",
                table: "vessel_metadata",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "kb_initial",
                schema: "data",
                table: "vessel_metadata",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "lcb_pct_lpp",
                schema: "data",
                table: "vessel_metadata",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "loa",
                schema: "data",
                table: "vessel_metadata",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "lwl",
                schema: "data",
                table: "vessel_metadata",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "midship_coefficient",
                schema: "data",
                table: "vessel_metadata",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "prismatic_coefficient",
                schema: "data",
                table: "vessel_metadata",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "shp_kw",
                schema: "data",
                table: "vessel_metadata",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "waterplane_coefficient",
                schema: "data",
                table: "vessel_metadata",
                type: "numeric",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "depth",
                schema: "data",
                table: "vessel_metadata");

            migrationBuilder.DropColumn(
                name: "ehp_kw",
                schema: "data",
                table: "vessel_metadata");

            migrationBuilder.DropColumn(
                name: "froude_number",
                schema: "data",
                table: "vessel_metadata");

            migrationBuilder.DropColumn(
                name: "gm_initial",
                schema: "data",
                table: "vessel_metadata");

            migrationBuilder.DropColumn(
                name: "kb_initial",
                schema: "data",
                table: "vessel_metadata");

            migrationBuilder.DropColumn(
                name: "lcb_pct_lpp",
                schema: "data",
                table: "vessel_metadata");

            migrationBuilder.DropColumn(
                name: "loa",
                schema: "data",
                table: "vessel_metadata");

            migrationBuilder.DropColumn(
                name: "lwl",
                schema: "data",
                table: "vessel_metadata");

            migrationBuilder.DropColumn(
                name: "midship_coefficient",
                schema: "data",
                table: "vessel_metadata");

            migrationBuilder.DropColumn(
                name: "prismatic_coefficient",
                schema: "data",
                table: "vessel_metadata");

            migrationBuilder.DropColumn(
                name: "shp_kw",
                schema: "data",
                table: "vessel_metadata");

            migrationBuilder.DropColumn(
                name: "waterplane_coefficient",
                schema: "data",
                table: "vessel_metadata");
        }
    }
}
