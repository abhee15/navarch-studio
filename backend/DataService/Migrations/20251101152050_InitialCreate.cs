using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataService.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "data");

            migrationBuilder.CreateTable(
                name: "benchmark_case",
                schema: "data",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    canonical_refs = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    hull_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    lpp_m = table.Column<decimal>(type: "numeric(10,3)", nullable: true),
                    b_m = table.Column<decimal>(type: "numeric(10,3)", nullable: true),
                    t_m = table.Column<decimal>(type: "numeric(10,3)", nullable: true),
                    cb = table.Column<decimal>(type: "numeric(6,4)", nullable: true),
                    cp = table.Column<decimal>(type: "numeric(6,4)", nullable: true),
                    lcb_pct_lpp = table.Column<decimal>(type: "numeric(6,3)", nullable: true),
                    lcf_pct_lpp = table.Column<decimal>(type: "numeric(6,3)", nullable: true),
                    geometry_missing = table.Column<bool>(type: "boolean", nullable: false),
                    catalog_hull_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_benchmark_case", x => x.id);
                    table.ForeignKey(
                        name: "fk_benchmark_case_benchmark_case_catalog_hull_id",
                        column: x => x.catalog_hull_id,
                        principalSchema: "data",
                        principalTable: "benchmark_case",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "catalog_propeller_series",
                schema: "data",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    blade_count = table.Column<int>(type: "integer", nullable: false),
                    expanded_area_ratio = table.Column<decimal>(type: "numeric(6,4)", nullable: false),
                    pitch_diameter_ratio = table.Column<decimal>(type: "numeric(6,4)", nullable: true),
                    source_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    license = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    is_demo = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_propeller_series", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "catalog_water_properties",
                schema: "data",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    medium = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    temperature_c = table.Column<decimal>(type: "numeric(6,2)", nullable: false),
                    salinity_psu = table.Column<decimal>(type: "numeric(8,4)", nullable: false),
                    density_kgm3 = table.Column<decimal>(type: "numeric(10,4)", nullable: false),
                    kinematic_viscosity_m2s = table.Column<decimal>(type: "numeric(12,8)", nullable: false),
                    source_ref = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    retrieved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_water_properties", x => x.id);
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
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    source_catalog_hull_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version_notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vessels", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "benchmark_asset",
                schema: "data",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    case_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    s3key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    caption = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    figure_ref = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    source_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_benchmark_asset", x => x.id);
                    table.ForeignKey(
                        name: "fk_benchmark_asset_benchmark_case_case_id",
                        column: x => x.case_id,
                        principalSchema: "data",
                        principalTable: "benchmark_case",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "benchmark_geometry",
                schema: "data",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    case_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    source_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    s3key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    checksum = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    scale_note = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    stations_json = table.Column<string>(type: "text", nullable: true),
                    waterlines_json = table.Column<string>(type: "text", nullable: true),
                    offsets_json = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_benchmark_geometry", x => x.id);
                    table.ForeignKey(
                        name: "fk_benchmark_geometry_benchmark_case_case_id",
                        column: x => x.case_id,
                        principalSchema: "data",
                        principalTable: "benchmark_case",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "benchmark_metric_ref",
                schema: "data",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    case_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fr = table.Column<decimal>(type: "numeric", nullable: true),
                    metric = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    value_num = table.Column<decimal>(type: "numeric(18,8)", nullable: true),
                    unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    tol_rel = table.Column<decimal>(type: "numeric(10,6)", nullable: true),
                    figure_ref = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    source_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_benchmark_metric_ref", x => x.id);
                    table.ForeignKey(
                        name: "fk_benchmark_metric_ref_benchmark_case_case_id",
                        column: x => x.case_id,
                        principalSchema: "data",
                        principalTable: "benchmark_case",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "benchmark_testpoint",
                schema: "data",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    case_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fr = table.Column<decimal>(type: "numeric(10,6)", nullable: false),
                    vm = table.Column<decimal>(type: "numeric(10,6)", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    medium = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    temperature_c = table.Column<decimal>(type: "numeric(6,2)", nullable: true),
                    salinity_psu = table.Column<decimal>(type: "numeric(8,4)", nullable: true),
                    density_kgm3 = table.Column<decimal>(type: "numeric(10,4)", nullable: true),
                    kinematic_viscosity_m2s = table.Column<decimal>(type: "numeric(12,6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_benchmark_testpoint", x => x.id);
                    table.ForeignKey(
                        name: "fk_benchmark_testpoint_benchmark_case_case_id",
                        column: x => x.case_id,
                        principalSchema: "data",
                        principalTable: "benchmark_case",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "benchmark_validation_run",
                schema: "data",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    case_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fr = table.Column<decimal>(type: "numeric(10,6)", nullable: true),
                    metrics = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_benchmark_validation_run", x => x.id);
                    table.ForeignKey(
                        name: "fk_benchmark_validation_run_benchmark_case_case_id",
                        column: x => x.case_id,
                        principalSchema: "data",
                        principalTable: "benchmark_case",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "catalog_propeller_points",
                schema: "data",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    series_id = table.Column<Guid>(type: "uuid", nullable: false),
                    j = table.Column<decimal>(type: "numeric(10,6)", nullable: false),
                    kt = table.Column<decimal>(type: "numeric(10,6)", nullable: false),
                    kq = table.Column<decimal>(type: "numeric(10,6)", nullable: false),
                    eta0 = table.Column<decimal>(type: "numeric(10,6)", nullable: false),
                    reynolds_number = table.Column<decimal>(type: "numeric(18,6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_propeller_points", x => x.id);
                    table.ForeignKey(
                        name: "fk_catalog_propeller_points_catalog_propeller_series_series_id",
                        column: x => x.series_id,
                        principalSchema: "data",
                        principalTable: "catalog_propeller_series",
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
                name: "loading_conditions",
                schema: "data",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    vessel_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lightship_tonnes = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    deadweight_tonnes = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_loading_conditions", x => x.id);
                    table.ForeignKey(
                        name: "fk_loading_conditions_vessels_vessel_id",
                        column: x => x.vessel_id,
                        principalSchema: "data",
                        principalTable: "vessels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "materials_config",
                schema: "data",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    vessel_id = table.Column<Guid>(type: "uuid", nullable: false),
                    hull_material = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    superstructure_material = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_materials_config", x => x.id);
                    table.ForeignKey(
                        name: "fk_materials_config_vessels_vessel_id",
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
                name: "vessel_metadata",
                schema: "data",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    vessel_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vessel_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    size = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    block_coefficient = table.Column<decimal>(type: "numeric(5,3)", nullable: true),
                    hull_family = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vessel_metadata", x => x.id);
                    table.ForeignKey(
                        name: "fk_vessel_metadata_vessels_vessel_id",
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
                name: "ix_benchmark_asset_case_id",
                schema: "data",
                table: "benchmark_asset",
                column: "case_id");

            migrationBuilder.CreateIndex(
                name: "ix_benchmark_case_catalog_hull_id",
                schema: "data",
                table: "benchmark_case",
                column: "catalog_hull_id");

            migrationBuilder.CreateIndex(
                name: "ix_benchmark_case_slug",
                schema: "data",
                table: "benchmark_case",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_benchmark_geometry_case_id",
                schema: "data",
                table: "benchmark_geometry",
                column: "case_id");

            migrationBuilder.CreateIndex(
                name: "ix_benchmark_metric_ref_case_id",
                schema: "data",
                table: "benchmark_metric_ref",
                column: "case_id");

            migrationBuilder.CreateIndex(
                name: "ix_benchmark_testpoint_case_id_fr",
                schema: "data",
                table: "benchmark_testpoint",
                columns: new[] { "case_id", "fr" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_benchmark_validation_run_case_id",
                schema: "data",
                table: "benchmark_validation_run",
                column: "case_id");

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
                name: "ix_catalog_propeller_points_series_id",
                schema: "data",
                table: "catalog_propeller_points",
                column: "series_id");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_water_properties_medium_temperature_c",
                schema: "data",
                table: "catalog_water_properties",
                columns: new[] { "medium", "temperature_c" });

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
                name: "ix_loading_conditions_vessel_id",
                schema: "data",
                table: "loading_conditions",
                column: "vessel_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_materials_config_vessel_id",
                schema: "data",
                table: "materials_config",
                column: "vessel_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_offsets_vessel_id_station_index_waterline_index",
                schema: "data",
                table: "offsets",
                columns: new[] { "vessel_id", "station_index", "waterline_index" },
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "ix_stations_vessel_id_station_index",
                schema: "data",
                table: "stations",
                columns: new[] { "vessel_id", "station_index" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_vessel_metadata_vessel_id",
                schema: "data",
                table: "vessel_metadata",
                column: "vessel_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_vessels_source_catalog_hull_id",
                schema: "data",
                table: "vessels",
                column: "source_catalog_hull_id");

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
                name: "benchmark_asset",
                schema: "data");

            migrationBuilder.DropTable(
                name: "benchmark_geometry",
                schema: "data");

            migrationBuilder.DropTable(
                name: "benchmark_metric_ref",
                schema: "data");

            migrationBuilder.DropTable(
                name: "benchmark_testpoint",
                schema: "data");

            migrationBuilder.DropTable(
                name: "benchmark_validation_run",
                schema: "data");

            migrationBuilder.DropTable(
                name: "board_cards",
                schema: "data");

            migrationBuilder.DropTable(
                name: "catalog_propeller_points",
                schema: "data");

            migrationBuilder.DropTable(
                name: "catalog_water_properties",
                schema: "data");

            migrationBuilder.DropTable(
                name: "curve_points",
                schema: "data");

            migrationBuilder.DropTable(
                name: "engine_points",
                schema: "data");

            migrationBuilder.DropTable(
                name: "hydro_results",
                schema: "data");

            migrationBuilder.DropTable(
                name: "loading_conditions",
                schema: "data");

            migrationBuilder.DropTable(
                name: "materials_config",
                schema: "data");

            migrationBuilder.DropTable(
                name: "offsets",
                schema: "data");

            migrationBuilder.DropTable(
                name: "sea_states",
                schema: "data");

            migrationBuilder.DropTable(
                name: "speed_points",
                schema: "data");

            migrationBuilder.DropTable(
                name: "stations",
                schema: "data");

            migrationBuilder.DropTable(
                name: "vessel_metadata",
                schema: "data");

            migrationBuilder.DropTable(
                name: "waterlines",
                schema: "data");

            migrationBuilder.DropTable(
                name: "benchmark_case",
                schema: "data");

            migrationBuilder.DropTable(
                name: "project_boards",
                schema: "data");

            migrationBuilder.DropTable(
                name: "catalog_propeller_series",
                schema: "data");

            migrationBuilder.DropTable(
                name: "curves",
                schema: "data");

            migrationBuilder.DropTable(
                name: "engine_curves",
                schema: "data");

            migrationBuilder.DropTable(
                name: "loadcases",
                schema: "data");

            migrationBuilder.DropTable(
                name: "speed_grids",
                schema: "data");

            migrationBuilder.DropTable(
                name: "vessels",
                schema: "data");
        }
    }
}
