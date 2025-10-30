using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataService.Migrations
{
    /// <inheritdoc />
    public partial class AddBenchmarkTables : Migration
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
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_benchmark_case", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "benchmark_asset",
                schema: "data",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    case_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    s3_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
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
                    s3_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    checksum = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    scale_note = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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
                    fr = table.Column<decimal>(type: "numeric(10,6)", nullable: true),
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
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "ix_benchmark_asset_case_id",
                schema: "data",
                table: "benchmark_asset",
                column: "case_id");

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
                name: "benchmark_case",
                schema: "data");
        }
    }
}
