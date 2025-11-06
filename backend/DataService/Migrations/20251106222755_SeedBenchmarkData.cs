using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataService.Migrations
{
    /// <inheritdoc />
    public partial class SeedBenchmarkData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Seed 9 benchmark hulls
            migrationBuilder.Sql(@"
                INSERT INTO catalog_user.vessels_real
                (id, vessel_id, vessel_type, lpp_m, beam_m, draft_m, depth_m, displacement_t, cb, cp, cm, source, is_system_data, data_quality, created_at, updated_at)
                VALUES
                (gen_random_uuid(), 'KVLCC2', 'Tanker', 320.0, 58.0, 20.8, 23.92, 320115, 0.8098, 0.8598, 0.9418, 'SIMMAN_2008', true, 'Reference', NOW(), NOW()),
                (gen_random_uuid(), 'KCS', 'Container', 230.0, 32.2, 10.8, 13.5, 53281, 0.6505, 0.7005, 0.9286, 'SIMMAN_2008', true, 'Reference', NOW(), NOW()),
                (gen_random_uuid(), 'DTMB5415', 'Combatant', 142.0, 19.06, 6.16, 8.316, 8636, 0.506, 0.586, 0.8635, 'SIMMAN_2014', true, 'Reference', NOW(), NOW()),
                (gen_random_uuid(), 'ONR_Tumblehome', 'Combatant', 154.0, 18.8, 5.49, 7.4115, 9259, 0.535, 0.615, 0.8699, 'SIMMAN_2014', true, 'Reference', NOW(), NOW()),
                (gen_random_uuid(), 'KRISO_Container_Ship', 'Container', 175.0, 25.4, 9.5, 11.875, 25215, 0.600, 0.680, 0.8824, 'SIMMAN_2020', true, 'Reference', NOW()),
                (gen_random_uuid(), 'SUBOFF', 'Submarine', 4.356, 0.508, 0.508, 0.508, 723.65, 1.000, 1.000, 1.000, 'DARPA', true, 'Reference', NOW(), NOW())
                ON CONFLICT (vessel_id) DO NOTHING;
            ");

            // Seed 19 test conditions
            migrationBuilder.Sql(@"
                INSERT INTO catalog_real.benchmark_test_conditions
                (id, test_type, hull_name, speed_knots, froude_number, reynolds_number, wave_height_m, wave_period_s, heading_deg, description, standard, created_at)
                VALUES
                (gen_random_uuid(), 'Resistance', 'KVLCC2', 15.5, 0.142, 2.5e9, 0, 0, 0, 'Calm water resistance', 'ITTC', NOW()),
                (gen_random_uuid(), 'Resistance', 'KCS', 24.0, 0.260, 1.4e9, 0, 0, 0, 'Calm water resistance', 'ITTC', NOW()),
                (gen_random_uuid(), 'Resistance', 'DTMB5415', 18.0, 0.248, 1.2e9, 0, 0, 0, 'Calm water resistance', 'ITTC', NOW()),
                (gen_random_uuid(), 'Self_Propulsion', 'KVLCC2', 15.5, 0.142, 2.5e9, 0, 0, 0, 'Propeller open water + behind hull', 'ITTC', NOW()),
                (gen_random_uuid(), 'Self_Propulsion', 'KCS', 24.0, 0.260, 1.4e9, 0, 0, 0, 'Propeller open water + behind hull', 'ITTC', NOW()),
                (gen_random_uuid(), 'Turning_Circle', 'KVLCC2', 15.5, 0.142, 2.5e9, 0, 0, 0, '35deg rudder angle', 'SIMMAN', NOW()),
                (gen_random_uuid(), 'Turning_Circle', 'KCS', 24.0, 0.260, 1.4e9, 0, 0, 0, '35deg rudder angle', 'SIMMAN', NOW()),
                (gen_random_uuid(), 'Zigzag_10-10', 'KVLCC2', 15.5, 0.142, 2.5e9, 0, 0, 0, '10deg rudder zigzag', 'SIMMAN', NOW()),
                (gen_random_uuid(), 'Zigzag_20-20', 'KVLCC2', 15.5, 0.142, 2.5e9, 0, 0, 0, '20deg rudder zigzag', 'SIMMAN', NOW()),
                (gen_random_uuid(), 'Zigzag_10-10', 'KCS', 24.0, 0.260, 1.4e9, 0, 0, 0, '10deg rudder zigzag', 'SIMMAN', NOW()),
                (gen_random_uuid(), 'Seakeeping_Head', 'KVLCC2', 15.5, 0.142, 2.5e9, 2.0, 12.0, 180, 'Head seas regular wave', 'ITTC', NOW()),
                (gen_random_uuid(), 'Seakeeping_Head', 'KCS', 24.0, 0.260, 1.4e9, 1.5, 10.0, 180, 'Head seas regular wave', 'ITTC', NOW()),
                (gen_random_uuid(), 'Seakeeping_Beam', 'KVLCC2', 15.5, 0.142, 2.5e9, 2.0, 12.0, 90, 'Beam seas regular wave', 'ITTC', NOW()),
                (gen_random_uuid(), 'Seakeeping_Quartering', 'DTMB5415', 18.0, 0.248, 1.2e9, 1.2, 8.5, 135, 'Quartering seas', 'ITTC', NOW()),
                (gen_random_uuid(), 'Added_Resistance', 'KCS', 24.0, 0.260, 1.4e9, 1.0, 6.0, 180, 'Head waves added resistance', 'ITTC', NOW()),
                (gen_random_uuid(), 'PMM_Pure_Sway', 'KVLCC2', 15.5, 0.142, 2.5e9, 0, 0, 0, 'Planar motion mechanism', 'SIMMAN', NOW()),
                (gen_random_uuid(), 'PMM_Pure_Yaw', 'KVLCC2', 15.5, 0.142, 2.5e9, 0, 0, 0, 'Planar motion mechanism', 'SIMMAN', NOW()),
                (gen_random_uuid(), 'Static_Drift', 'KCS', 24.0, 0.260, 1.4e9, 0, 0, 0, 'Oblique towing 0-30deg', 'SIMMAN', NOW());
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM catalog_user.vessels_real WHERE data_quality = 'Reference';
                DELETE FROM catalog_real.benchmark_test_conditions;
            ");
        }
    }
}
