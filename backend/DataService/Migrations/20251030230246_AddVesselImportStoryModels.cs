using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataService.Migrations
{
    /// <inheritdoc />
    public partial class AddVesselImportStoryModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "s3_key",
                schema: "data",
                table: "benchmark_geometry",
                newName: "s3key");

            migrationBuilder.RenameColumn(
                name: "s3_key",
                schema: "data",
                table: "benchmark_asset",
                newName: "s3key");

            migrationBuilder.AddColumn<string>(
                name: "version_notes",
                schema: "data",
                table: "vessels",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "fr",
                schema: "data",
                table: "benchmark_metric_ref",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,6)",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "engine_curves",
                schema: "data",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    vessel_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_engine_curves", x => x.id);
                    table.ForeignKey(
                        name: "fk_engine_curves_vessels_vessel_id",
                        column: x => x.vessel_id,
                        principalSchema: "data",
                        principalTable: "vessels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "project_boards",
                schema: "data",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_project_boards", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sea_states",
                schema: "data",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    vessel_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    wave_height = table.Column<decimal>(type: "numeric(8,3)", nullable: false),
                    wave_period = table.Column<decimal>(type: "numeric(8,3)", nullable: false),
                    wave_direction = table.Column<decimal>(type: "numeric(6,2)", nullable: false),
                    wind_speed = table.Column<decimal>(type: "numeric(8,3)", nullable: true),
                    wind_direction = table.Column<decimal>(type: "numeric(6,2)", nullable: true),
                    water_depth = table.Column<decimal>(type: "numeric(10,3)", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sea_states", x => x.id);
                    table.ForeignKey(
                        name: "fk_sea_states_vessels_vessel_id",
                        column: x => x.vessel_id,
                        principalSchema: "data",
                        principalTable: "vessels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "speed_grids",
                schema: "data",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    vessel_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_speed_grids", x => x.id);
                    table.ForeignKey(
                        name: "fk_speed_grids_vessels_vessel_id",
                        column: x => x.vessel_id,
                        principalSchema: "data",
                        principalTable: "vessels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "engine_points",
                schema: "data",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    engine_curve_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rpm = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    power_kw = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    torque = table.Column<decimal>(type: "numeric(12,2)", nullable: true),
                    fuel_consumption = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_engine_points", x => x.id);
                    table.ForeignKey(
                        name: "fk_engine_points_engine_curves_engine_curve_id",
                        column: x => x.engine_curve_id,
                        principalSchema: "data",
                        principalTable: "engine_curves",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "board_cards",
                schema: "data",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    board_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vessel_id = table.Column<Guid>(type: "uuid", nullable: true),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    tags = table.Column<string>(type: "text", nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_board_cards", x => x.id);
                    table.ForeignKey(
                        name: "fk_board_cards_project_boards_board_id",
                        column: x => x.board_id,
                        principalSchema: "data",
                        principalTable: "project_boards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_board_cards_vessels_vessel_id",
                        column: x => x.vessel_id,
                        principalSchema: "data",
                        principalTable: "vessels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "speed_points",
                schema: "data",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    speed_grid_id = table.Column<Guid>(type: "uuid", nullable: false),
                    speed = table.Column<decimal>(type: "numeric(10,3)", nullable: false),
                    speed_knots = table.Column<decimal>(type: "numeric(10,3)", nullable: true),
                    froude_number = table.Column<decimal>(type: "numeric(8,4)", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_speed_points", x => x.id);
                    table.ForeignKey(
                        name: "fk_speed_points_speed_grids_speed_grid_id",
                        column: x => x.speed_grid_id,
                        principalSchema: "data",
                        principalTable: "speed_grids",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_board_cards_board_id",
                schema: "data",
                table: "board_cards",
                column: "board_id");

            migrationBuilder.CreateIndex(
                name: "ix_board_cards_vessel_id",
                schema: "data",
                table: "board_cards",
                column: "vessel_id");

            migrationBuilder.CreateIndex(
                name: "ix_engine_curves_vessel_id",
                schema: "data",
                table: "engine_curves",
                column: "vessel_id");

            migrationBuilder.CreateIndex(
                name: "ix_engine_points_engine_curve_id",
                schema: "data",
                table: "engine_points",
                column: "engine_curve_id");

            migrationBuilder.CreateIndex(
                name: "ix_project_boards_user_id",
                schema: "data",
                table: "project_boards",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_sea_states_vessel_id",
                schema: "data",
                table: "sea_states",
                column: "vessel_id");

            migrationBuilder.CreateIndex(
                name: "ix_speed_grids_vessel_id",
                schema: "data",
                table: "speed_grids",
                column: "vessel_id");

            migrationBuilder.CreateIndex(
                name: "ix_speed_points_speed_grid_id",
                schema: "data",
                table: "speed_points",
                column: "speed_grid_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "board_cards",
                schema: "data");

            migrationBuilder.DropTable(
                name: "engine_points",
                schema: "data");

            migrationBuilder.DropTable(
                name: "sea_states",
                schema: "data");

            migrationBuilder.DropTable(
                name: "speed_points",
                schema: "data");

            migrationBuilder.DropTable(
                name: "project_boards",
                schema: "data");

            migrationBuilder.DropTable(
                name: "engine_curves",
                schema: "data");

            migrationBuilder.DropTable(
                name: "speed_grids",
                schema: "data");

            migrationBuilder.DropColumn(
                name: "version_notes",
                schema: "data",
                table: "vessels");

            migrationBuilder.RenameColumn(
                name: "s3key",
                schema: "data",
                table: "benchmark_geometry",
                newName: "s3_key");

            migrationBuilder.RenameColumn(
                name: "s3key",
                schema: "data",
                table: "benchmark_asset",
                newName: "s3_key");

            migrationBuilder.AlterColumn<decimal>(
                name: "fr",
                schema: "data",
                table: "benchmark_metric_ref",
                type: "numeric(10,6)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);
        }
    }
}
