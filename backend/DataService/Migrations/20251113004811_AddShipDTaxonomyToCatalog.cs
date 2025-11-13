using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataService.Migrations
{
    /// <inheritdoc />
    public partial class AddShipDTaxonomyToCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "bow_family",
                schema: "catalog_user",
                table: "vessels_real",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "family_mask_version",
                schema: "catalog_user",
                table: "vessels_real",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "midship_family",
                schema: "catalog_user",
                table: "vessels_real",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "shipd_parameters_json",
                schema: "catalog_user",
                table: "vessels_real",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "shipd_vessel_type",
                schema: "catalog_user",
                table: "vessels_real",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "stern_family",
                schema: "catalog_user",
                table: "vessels_real",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "vessel_category",
                schema: "catalog_user",
                table: "vessels_real",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_vessels_real_shipd_taxonomy",
                schema: "catalog_user",
                table: "vessels_real",
                columns: new[] { "vessel_category", "shipd_vessel_type" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_vessels_real_shipd_taxonomy",
                schema: "catalog_user",
                table: "vessels_real");

            migrationBuilder.DropColumn(
                name: "bow_family",
                schema: "catalog_user",
                table: "vessels_real");

            migrationBuilder.DropColumn(
                name: "family_mask_version",
                schema: "catalog_user",
                table: "vessels_real");

            migrationBuilder.DropColumn(
                name: "midship_family",
                schema: "catalog_user",
                table: "vessels_real");

            migrationBuilder.DropColumn(
                name: "shipd_parameters_json",
                schema: "catalog_user",
                table: "vessels_real");

            migrationBuilder.DropColumn(
                name: "shipd_vessel_type",
                schema: "catalog_user",
                table: "vessels_real");

            migrationBuilder.DropColumn(
                name: "stern_family",
                schema: "catalog_user",
                table: "vessels_real");

            migrationBuilder.DropColumn(
                name: "vessel_category",
                schema: "catalog_user",
                table: "vessels_real");
        }
    }
}
