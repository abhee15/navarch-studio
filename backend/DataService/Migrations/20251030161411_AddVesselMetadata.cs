using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataService.Migrations
{
    /// <inheritdoc />
    public partial class AddVesselMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "ix_vessel_metadata_vessel_id",
                schema: "data",
                table: "vessel_metadata",
                column: "vessel_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "loading_conditions",
                schema: "data");

            migrationBuilder.DropTable(
                name: "materials_config",
                schema: "data");

            migrationBuilder.DropTable(
                name: "vessel_metadata",
                schema: "data");
        }
    }
}
