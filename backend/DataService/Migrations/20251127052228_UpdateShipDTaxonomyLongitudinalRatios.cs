using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataService.Migrations
{
    /// <inheritdoc />
    public partial class UpdateShipDTaxonomyLongitudinalRatios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update all vessel types with bow/stern length ratio defaults
            // These defaults were missing from the original seed data and caused
            // all candidates to have identical longitudinal distributions (Lb=0%, Lm=100%, Ls=0%)

            // Standard defaults (Lb=0.30, Ls=0.30): general_cargo, bulk_carrier, container, tanker,
            // lng_carrier, cruise_vessel, passenger_vessel, cutters, medical_ship, general_military,
            // high_speed_craft, research_vessel
            var standardDefaults = new[] {
                "general_cargo", "bulk_carrier", "container", "tanker", "lng_carrier",
                "cruise_vessel", "passenger_vessel", "cutters", "medical_ship",
                "general_military", "high_speed_craft", "research_vessel"
            };

            foreach (var type in standardDefaults)
            {
                migrationBuilder.Sql($@"
                    UPDATE data.shipd_vessel_taxonomy
                    SET additional_parameters_json =
                        COALESCE(additional_parameters_json, '{{}}'::jsonb) ||
                        '{{""bowLengthRatio"": 0.30, ""sternLengthRatio"": 0.30}}'::jsonb
                    WHERE type = '{type}';
                ");
            }

            // Fishing vessels (Lb=0.35, Ls=0.35)
            migrationBuilder.Sql(@"
                UPDATE data.shipd_vessel_taxonomy
                SET additional_parameters_json =
                    COALESCE(additional_parameters_json, '{}'::jsonb) ||
                    '{""bowLengthRatio"": 0.35, ""sternLengthRatio"": 0.35}'::jsonb
                WHERE type IN ('fishing', 'fishing_recreational');
            ");

            // Yachts (Lb=0.45, Ls=0.35)
            migrationBuilder.Sql(@"
                UPDATE data.shipd_vessel_taxonomy
                SET additional_parameters_json =
                    COALESCE(additional_parameters_json, '{}'::jsonb) ||
                    '{""bowLengthRatio"": 0.45, ""sternLengthRatio"": 0.35}'::jsonb
                WHERE type = 'yacht';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove bowLengthRatio and sternLengthRatio from all vessel types
            migrationBuilder.Sql(@"
                UPDATE data.shipd_vessel_taxonomy
                SET additional_parameters_json =
                    additional_parameters_json - 'bowLengthRatio' - 'sternLengthRatio'
                WHERE additional_parameters_json IS NOT NULL;
            ");
        }
    }
}
