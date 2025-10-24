using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataService.Migrations
{
    /// <inheritdoc />
    public partial class AddSchemaAndSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "data");

            migrationBuilder.RenameTable(
                name: "Products",
                newName: "Products",
                newSchema: "data");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "data",
                table: "Products",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                schema: "data",
                table: "Products",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "data",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "data",
                table: "Products");

            migrationBuilder.RenameTable(
                name: "Products",
                schema: "data",
                newName: "Products");
        }
    }
}





