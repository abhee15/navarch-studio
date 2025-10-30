using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataService.Migrations
{
    /// <inheritdoc />
    public partial class AddHydrostaticsSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "data");

            migrationBuilder.CreateTable(
                name: "vessels",
                schema: "data",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    lpp = table.Column<decimal>(type: "numeric(10,3)", nullable: false),
                    beam = table.Column<decimal>(type: "numeric(10,3)", nullable: false),
                    design_draft = table.Column<decimal>(type: "numeric(10,3)", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vessels", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "curves",
                schema: "data",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    vessel_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    x_label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    y_label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    meta = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_curves", x => x.id);
                    table.ForeignKey(
                        name: "fk_curves_vessels_vessel_id",
                        column: x => x.vessel_id,
                        principalSchema: "data",
                        principalTable: "vessels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "loadcases",
                schema: "data",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    vessel_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    rho = table.Column<decimal>(type: "numeric(10,3)", nullable: false),
                    kg = table.Column<decimal>(type: "numeric(10,3)", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_loadcases", x => x.id);
                    table.ForeignKey(
                        name: "fk_loadcases_vessels_vessel_id",
                        column: x => x.vessel_id,
                        principalSchema: "data",
                        principalTable: "vessels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "offsets",
                schema: "data",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    vessel_id = table.Column<Guid>(type: "uuid", nullable: false),
                    station_index = table.Column<int>(type: "integer", nullable: false),
                    waterline_index = table.Column<int>(type: "integer", nullable: false),
                    half_breadth_y = table.Column<decimal>(type: "numeric(10,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_offsets", x => x.id);
                    table.ForeignKey(
                        name: "fk_offsets_vessels_vessel_id",
                        column: x => x.vessel_id,
                        principalSchema: "data",
                        principalTable: "vessels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "stations",
                schema: "data",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    vessel_id = table.Column<Guid>(type: "uuid", nullable: false),
                    station_index = table.Column<int>(type: "integer", nullable: false),
                    x = table.Column<decimal>(type: "numeric(10,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stations", x => x.id);
                    table.ForeignKey(
                        name: "fk_stations_vessels_vessel_id",
                        column: x => x.vessel_id,
                        principalSchema: "data",
                        principalTable: "vessels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "waterlines",
                schema: "data",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    vessel_id = table.Column<Guid>(type: "uuid", nullable: false),
                    waterline_index = table.Column<int>(type: "integer", nullable: false),
                    z = table.Column<decimal>(type: "numeric(10,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_waterlines", x => x.id);
                    table.ForeignKey(
                        name: "fk_waterlines_vessels_vessel_id",
                        column: x => x.vessel_id,
                        principalSchema: "data",
                        principalTable: "vessels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "curve_points",
                schema: "data",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    curve_id = table.Column<Guid>(type: "uuid", nullable: false),
                    x = table.Column<decimal>(type: "numeric(15,6)", nullable: false),
                    y = table.Column<decimal>(type: "numeric(15,6)", nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_curve_points", x => x.id);
                    table.ForeignKey(
                        name: "fk_curve_points_curves_curve_id",
                        column: x => x.curve_id,
                        principalSchema: "data",
                        principalTable: "curves",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "hydro_results",
                schema: "data",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    vessel_id = table.Column<Guid>(type: "uuid", nullable: false),
                    loadcase_id = table.Column<Guid>(type: "uuid", nullable: true),
                    draft = table.Column<decimal>(type: "numeric(10,4)", nullable: false),
                    disp_volume = table.Column<decimal>(type: "numeric(15,4)", nullable: true),
                    disp_weight = table.Column<decimal>(type: "numeric(15,4)", nullable: true),
                    k_bz = table.Column<decimal>(type: "numeric(10,4)", nullable: true),
                    lc_bx = table.Column<decimal>(type: "numeric(10,4)", nullable: true),
                    tc_by = table.Column<decimal>(type: "numeric(10,4)", nullable: true),
                    b_mt = table.Column<decimal>(type: "numeric(10,4)", nullable: true),
                    b_ml = table.Column<decimal>(type: "numeric(10,4)", nullable: true),
                    g_mt = table.Column<decimal>(type: "numeric(10,4)", nullable: true),
                    g_ml = table.Column<decimal>(type: "numeric(10,4)", nullable: true),
                    awp = table.Column<decimal>(type: "numeric(12,4)", nullable: true),
                    iwp = table.Column<decimal>(type: "numeric(15,4)", nullable: true),
                    cb = table.Column<decimal>(type: "numeric(6,4)", nullable: true),
                    cp = table.Column<decimal>(type: "numeric(6,4)", nullable: true),
                    cm = table.Column<decimal>(type: "numeric(6,4)", nullable: true),
                    cwp = table.Column<decimal>(type: "numeric(6,4)", nullable: true),
                    trim_angle = table.Column<decimal>(type: "numeric(6,3)", nullable: true),
                    meta = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_hydro_results", x => x.id);
                    table.ForeignKey(
                        name: "fk_hydro_results_loadcases_loadcase_id",
                        column: x => x.loadcase_id,
                        principalSchema: "data",
                        principalTable: "loadcases",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_hydro_results_vessels_vessel_id",
                        column: x => x.vessel_id,
                        principalSchema: "data",
                        principalTable: "vessels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_curve_points_curve_id_sequence",
                schema: "data",
                table: "curve_points",
                columns: new[] { "curve_id", "sequence" });

            migrationBuilder.CreateIndex(
                name: "ix_curves_vessel_id",
                schema: "data",
                table: "curves",
                column: "vessel_id");

            migrationBuilder.CreateIndex(
                name: "ix_hydro_results_loadcase_id",
                schema: "data",
                table: "hydro_results",
                column: "loadcase_id");

            migrationBuilder.CreateIndex(
                name: "ix_hydro_results_vessel_id",
                schema: "data",
                table: "hydro_results",
                column: "vessel_id");

            migrationBuilder.CreateIndex(
                name: "ix_loadcases_vessel_id",
                schema: "data",
                table: "loadcases",
                column: "vessel_id");

            migrationBuilder.CreateIndex(
                name: "ix_offsets_vessel_id_station_index_waterline_index",
                schema: "data",
                table: "offsets",
                columns: new[] { "vessel_id", "station_index", "waterline_index" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_stations_vessel_id_station_index",
                schema: "data",
                table: "stations",
                columns: new[] { "vessel_id", "station_index" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_vessels_user_id",
                schema: "data",
                table: "vessels",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_waterlines_vessel_id_waterline_index",
                schema: "data",
                table: "waterlines",
                columns: new[] { "vessel_id", "waterline_index" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "curve_points",
                schema: "data");

            migrationBuilder.DropTable(
                name: "hydro_results",
                schema: "data");

            migrationBuilder.DropTable(
                name: "offsets",
                schema: "data");

            migrationBuilder.DropTable(
                name: "stations",
                schema: "data");

            migrationBuilder.DropTable(
                name: "waterlines",
                schema: "data");

            migrationBuilder.DropTable(
                name: "curves",
                schema: "data");

            migrationBuilder.DropTable(
                name: "loadcases",
                schema: "data");

            migrationBuilder.DropTable(
                name: "vessels",
                schema: "data");
        }
    }
}
