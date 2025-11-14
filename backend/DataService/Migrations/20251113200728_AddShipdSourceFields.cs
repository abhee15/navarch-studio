using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataService.Migrations
{
    /// <inheritdoc />
    public partial class AddShipdSourceFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "origin_candidate_id",
                schema: "data",
                table: "vessels",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "origin_created_at",
                schema: "data",
                table: "vessels",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "origin_design_name",
                schema: "data",
                table: "vessels",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "origin_idempotency_key",
                schema: "data",
                table: "vessels",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "origin_mission_case_id",
                schema: "data",
                table: "vessels",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "origin_mission_name",
                schema: "data",
                table: "vessels",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "origin_run_name",
                schema: "data",
                table: "vessels",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "origin_sizing_run_id",
                schema: "data",
                table: "vessels",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "origin_system",
                schema: "data",
                table: "vessels",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "origin_user_display_name",
                schema: "data",
                table: "vessels",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "origin_user_id",
                schema: "data",
                table: "vessels",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "pushed_to_hydrostatics_at",
                schema: "data",
                table: "vessels",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "shipd_bow_family",
                schema: "data",
                table: "vessels",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "shipd_category",
                schema: "data",
                table: "vessels",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "shipd_mask_version",
                schema: "data",
                table: "vessels",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "shipd_midship_family",
                schema: "data",
                table: "vessels",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "shipd_parameters_json",
                schema: "data",
                table: "vessels",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "shipd_stern_family",
                schema: "data",
                table: "vessels",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "shipd_type",
                schema: "data",
                table: "vessels",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "shipd_type_display_name",
                schema: "data",
                table: "vessels",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "shipd_category",
                schema: "data",
                table: "vessel_metadata",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "shipd_mask_version",
                schema: "data",
                table: "vessel_metadata",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "shipd_type",
                schema: "data",
                table: "vessel_metadata",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_vessels_origin_candidate_id",
                schema: "data",
                table: "vessels",
                column: "origin_candidate_id");

            migrationBuilder.CreateIndex(
                name: "ix_vessels_origin_mission_case_id",
                schema: "data",
                table: "vessels",
                column: "origin_mission_case_id");

            migrationBuilder.CreateIndex(
                name: "ix_vessels_origin_sizing_run_id",
                schema: "data",
                table: "vessels",
                column: "origin_sizing_run_id");

            migrationBuilder.CreateIndex(
                name: "ix_vessels_shipd_type",
                schema: "data",
                table: "vessels",
                column: "shipd_type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_vessels_origin_candidate_id",
                schema: "data",
                table: "vessels");

            migrationBuilder.DropIndex(
                name: "ix_vessels_origin_mission_case_id",
                schema: "data",
                table: "vessels");

            migrationBuilder.DropIndex(
                name: "ix_vessels_origin_sizing_run_id",
                schema: "data",
                table: "vessels");

            migrationBuilder.DropIndex(
                name: "ix_vessels_shipd_type",
                schema: "data",
                table: "vessels");

            migrationBuilder.DropColumn(
                name: "origin_candidate_id",
                schema: "data",
                table: "vessels");

            migrationBuilder.DropColumn(
                name: "origin_created_at",
                schema: "data",
                table: "vessels");

            migrationBuilder.DropColumn(
                name: "origin_design_name",
                schema: "data",
                table: "vessels");

            migrationBuilder.DropColumn(
                name: "origin_idempotency_key",
                schema: "data",
                table: "vessels");

            migrationBuilder.DropColumn(
                name: "origin_mission_case_id",
                schema: "data",
                table: "vessels");

            migrationBuilder.DropColumn(
                name: "origin_mission_name",
                schema: "data",
                table: "vessels");

            migrationBuilder.DropColumn(
                name: "origin_run_name",
                schema: "data",
                table: "vessels");

            migrationBuilder.DropColumn(
                name: "origin_sizing_run_id",
                schema: "data",
                table: "vessels");

            migrationBuilder.DropColumn(
                name: "origin_system",
                schema: "data",
                table: "vessels");

            migrationBuilder.DropColumn(
                name: "origin_user_display_name",
                schema: "data",
                table: "vessels");

            migrationBuilder.DropColumn(
                name: "origin_user_id",
                schema: "data",
                table: "vessels");

            migrationBuilder.DropColumn(
                name: "pushed_to_hydrostatics_at",
                schema: "data",
                table: "vessels");

            migrationBuilder.DropColumn(
                name: "shipd_bow_family",
                schema: "data",
                table: "vessels");

            migrationBuilder.DropColumn(
                name: "shipd_category",
                schema: "data",
                table: "vessels");

            migrationBuilder.DropColumn(
                name: "shipd_mask_version",
                schema: "data",
                table: "vessels");

            migrationBuilder.DropColumn(
                name: "shipd_midship_family",
                schema: "data",
                table: "vessels");

            migrationBuilder.DropColumn(
                name: "shipd_parameters_json",
                schema: "data",
                table: "vessels");

            migrationBuilder.DropColumn(
                name: "shipd_stern_family",
                schema: "data",
                table: "vessels");

            migrationBuilder.DropColumn(
                name: "shipd_type",
                schema: "data",
                table: "vessels");

            migrationBuilder.DropColumn(
                name: "shipd_type_display_name",
                schema: "data",
                table: "vessels");

            migrationBuilder.DropColumn(
                name: "shipd_category",
                schema: "data",
                table: "vessel_metadata");

            migrationBuilder.DropColumn(
                name: "shipd_mask_version",
                schema: "data",
                table: "vessel_metadata");

            migrationBuilder.DropColumn(
                name: "shipd_type",
                schema: "data",
                table: "vessel_metadata");
        }
    }
}
