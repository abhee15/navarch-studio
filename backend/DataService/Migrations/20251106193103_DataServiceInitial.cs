using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DataService.Migrations
{
    /// <inheritdoc />
    public partial class DataServiceInitial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "catalog_ml");

            migrationBuilder.CreateTable(
                name: "parametric_hulls",
                schema: "catalog_ml",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    hull_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    dataset_source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    row_index = table.Column<int>(type: "integer", nullable: false),
                    parametric_vector = table.Column<string>(type: "jsonb", nullable: false),
                    loa_m = table.Column<decimal>(type: "numeric(10,3)", nullable: false),
                    lb_ratio = table.Column<decimal>(type: "numeric(6,4)", nullable: false),
                    ls_ratio = table.Column<decimal>(type: "numeric(6,4)", nullable: false),
                    bd_ratio = table.Column<decimal>(type: "numeric(8,6)", nullable: false),
                    dd_ratio = table.Column<decimal>(type: "numeric(8,6)", nullable: false),
                    bs_ratio = table.Column<decimal>(type: "numeric(6,4)", nullable: false),
                    volume_norm = table.Column<decimal>(type: "numeric(12,8)", nullable: false),
                    lcb_norm = table.Column<decimal>(type: "numeric(6,4)", nullable: false),
                    vcb_norm = table.Column<decimal>(type: "numeric(6,4)", nullable: true),
                    area_wp_norm = table.Column<decimal>(type: "numeric(10,8)", nullable: false),
                    cw_coeff = table.Column<decimal>(type: "numeric(5,4)", nullable: false),
                    area_ws_norm = table.Column<decimal>(type: "numeric(10,8)", nullable: true),
                    ixx_norm = table.Column<decimal>(type: "numeric(12,8)", nullable: true),
                    iyy_norm = table.Column<decimal>(type: "numeric(12,8)", nullable: true),
                    geometric_measures = table.Column<string>(type: "jsonb", nullable: false),
                    lpp_m_derived = table.Column<decimal>(type: "numeric(10,3)", nullable: false),
                    beam_m_derived = table.Column<decimal>(type: "numeric(10,3)", nullable: false),
                    draft_m_derived = table.Column<decimal>(type: "numeric(10,3)", nullable: false),
                    depth_m_derived = table.Column<decimal>(type: "numeric(10,3)", nullable: false),
                    cb_derived = table.Column<decimal>(type: "numeric(5,4)", nullable: false),
                    cp_derived = table.Column<decimal>(type: "numeric(5,4)", nullable: true),
                    cm_derived = table.Column<decimal>(type: "numeric(5,4)", nullable: true),
                    conversion_quality = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    has_valid_coefficients = table.Column<bool>(type: "boolean", nullable: false),
                    distortion_score = table.Column<decimal>(type: "numeric(5,4)", nullable: true),
                    imported_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_version = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_parametric_hulls", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "parametric_hulls",
                schema: "catalog_ml");
        }
    }
}
