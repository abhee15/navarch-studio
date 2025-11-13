using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataService.Migrations
{
    /// <inheritdoc />
    public partial class AddShipDTaxonomy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "shipd_parameter_metadata",
                schema: "data",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    parameter_index = table.Column<int>(type: "integer", nullable: false),
                    label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    group = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    min = table.Column<decimal>(type: "numeric(12,6)", nullable: true),
                    max = table.Column<decimal>(type: "numeric(12,6)", nullable: true),
                    mean = table.Column<decimal>(type: "numeric(12,6)", nullable: true),
                    std_dev = table.Column<decimal>(type: "numeric(12,6)", nullable: true),
                    metadata_json = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shipd_parameter_metadata", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "shipd_vessel_taxonomy",
                schema: "data",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    display_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    bow_families_json = table.Column<string>(type: "jsonb", nullable: false),
                    midship_families_json = table.Column<string>(type: "jsonb", nullable: false),
                    stern_families_json = table.Column<string>(type: "jsonb", nullable: false),
                    additional_parameters_json = table.Column<string>(type: "jsonb", nullable: true),
                    mask_version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shipd_vessel_taxonomy", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_shipd_parameter_metadata_parameter_index",
                schema: "data",
                table: "shipd_parameter_metadata",
                column: "parameter_index",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_shipd_vessel_taxonomy_category_type",
                schema: "data",
                table: "shipd_vessel_taxonomy",
                columns: new[] { "category", "type" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "shipd_parameter_metadata",
                schema: "data");

            migrationBuilder.DropTable(
                name: "shipd_vessel_taxonomy",
                schema: "data");
        }
    }
}
