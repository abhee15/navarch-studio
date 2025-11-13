using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HullSizingService.Migrations
{
    /// <inheritdoc />
    public partial class AddShipDTaxonomyFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "bow_family",
                schema: "sizing",
                table: "sizing_runs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "family_mask_version",
                schema: "sizing",
                table: "sizing_runs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "midship_family",
                schema: "sizing",
                table: "sizing_runs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "shipd_input_vector_json",
                schema: "sizing",
                table: "sizing_runs",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "stern_family",
                schema: "sizing",
                table: "sizing_runs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "vessel_category",
                schema: "sizing",
                table: "sizing_runs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "vessel_type",
                schema: "sizing",
                table: "sizing_runs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "bow_family",
                schema: "sizing",
                table: "mission_cases",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "family_mask_version",
                schema: "sizing",
                table: "mission_cases",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "midship_family",
                schema: "sizing",
                table: "mission_cases",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "shipd_inputs_json",
                schema: "sizing",
                table: "mission_cases",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "stern_family",
                schema: "sizing",
                table: "mission_cases",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "bow_family",
                schema: "sizing",
                table: "candidate_designs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "family_mask_version",
                schema: "sizing",
                table: "candidate_designs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "midship_family",
                schema: "sizing",
                table: "candidate_designs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "shipd_parameters_json",
                schema: "sizing",
                table: "candidate_designs",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "stern_family",
                schema: "sizing",
                table: "candidate_designs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "vessel_category",
                schema: "sizing",
                table: "candidate_designs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "vessel_type",
                schema: "sizing",
                table: "candidate_designs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "bow_family",
                schema: "sizing",
                table: "sizing_runs");

            migrationBuilder.DropColumn(
                name: "family_mask_version",
                schema: "sizing",
                table: "sizing_runs");

            migrationBuilder.DropColumn(
                name: "midship_family",
                schema: "sizing",
                table: "sizing_runs");

            migrationBuilder.DropColumn(
                name: "shipd_input_vector_json",
                schema: "sizing",
                table: "sizing_runs");

            migrationBuilder.DropColumn(
                name: "stern_family",
                schema: "sizing",
                table: "sizing_runs");

            migrationBuilder.DropColumn(
                name: "vessel_category",
                schema: "sizing",
                table: "sizing_runs");

            migrationBuilder.DropColumn(
                name: "vessel_type",
                schema: "sizing",
                table: "sizing_runs");

            migrationBuilder.DropColumn(
                name: "bow_family",
                schema: "sizing",
                table: "mission_cases");

            migrationBuilder.DropColumn(
                name: "family_mask_version",
                schema: "sizing",
                table: "mission_cases");

            migrationBuilder.DropColumn(
                name: "midship_family",
                schema: "sizing",
                table: "mission_cases");

            migrationBuilder.DropColumn(
                name: "shipd_inputs_json",
                schema: "sizing",
                table: "mission_cases");

            migrationBuilder.DropColumn(
                name: "stern_family",
                schema: "sizing",
                table: "mission_cases");

            migrationBuilder.DropColumn(
                name: "bow_family",
                schema: "sizing",
                table: "candidate_designs");

            migrationBuilder.DropColumn(
                name: "family_mask_version",
                schema: "sizing",
                table: "candidate_designs");

            migrationBuilder.DropColumn(
                name: "midship_family",
                schema: "sizing",
                table: "candidate_designs");

            migrationBuilder.DropColumn(
                name: "shipd_parameters_json",
                schema: "sizing",
                table: "candidate_designs");

            migrationBuilder.DropColumn(
                name: "stern_family",
                schema: "sizing",
                table: "candidate_designs");

            migrationBuilder.DropColumn(
                name: "vessel_category",
                schema: "sizing",
                table: "candidate_designs");

            migrationBuilder.DropColumn(
                name: "vessel_type",
                schema: "sizing",
                table: "candidate_designs");
        }
    }
}
