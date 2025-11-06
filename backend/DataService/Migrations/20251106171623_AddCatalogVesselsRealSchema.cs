using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataService.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogVesselsRealSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create catalog_user schema for user-editable real-world vessel catalog
            migrationBuilder.Sql("CREATE SCHEMA IF NOT EXISTS catalog_user;");

            // Create vessels_real table
            migrationBuilder.Sql(@"
                CREATE TABLE catalog_user.vessels_real (
                    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                    vessel_id TEXT UNIQUE NOT NULL,
                    vessel_type TEXT NOT NULL,
                    
                    -- Principal Dimensions
                    lpp_m DECIMAL(10,3) NOT NULL CHECK (lpp_m > 0),
                    beam_m DECIMAL(10,3) NOT NULL CHECK (beam_m > 0),
                    draft_m DECIMAL(10,3) NOT NULL CHECK (draft_m > 0),
                    depth_m DECIMAL(10,3) CHECK (depth_m > 0),
                    displacement_t DECIMAL(12,2) NOT NULL CHECK (displacement_t > 0),
                    
                    -- Form Coefficients
                    cb DECIMAL(5,4) NOT NULL CHECK (cb BETWEEN 0.3 AND 0.95),
                    cp DECIMAL(5,4) CHECK (cp BETWEEN 0.5 AND 1.0),
                    cm DECIMAL(5,4) CHECK (cm BETWEEN 0.7 AND 1.0),
                    cw DECIMAL(5,4) CHECK (cw BETWEEN 0.5 AND 1.0),
                    
                    -- Performance
                    service_speed_ms DECIMAL(6,3) CHECK (service_speed_ms > 0),
                    dwt_t DECIMAL(12,2) CHECK (dwt_t >= 0),
                    
                    -- Additional Data
                    engine_type TEXT,
                    year_built INTEGER CHECK (year_built BETWEEN 1900 AND 2100),
                    source TEXT,
                    data_quality TEXT,
                    
                    -- Geometry & Performance Data (JSONB)
                    resistance_curve JSONB,
                    hull_geometry_file TEXT,
                    
                    -- Permissions & Tracking
                    is_system_data BOOLEAN DEFAULT TRUE NOT NULL,
                    created_by UUID,
                    created_at TIMESTAMPTZ DEFAULT NOW() NOT NULL,
                    updated_at TIMESTAMPTZ DEFAULT NOW() NOT NULL,
                    
                    CONSTRAINT chk_system_or_user CHECK (
                        (is_system_data = TRUE AND created_by IS NULL) OR
                        (is_system_data = FALSE AND created_by IS NOT NULL)
                    )
                );
            ");

            // Create indexes for KNN search performance
            migrationBuilder.Sql(@"
                CREATE INDEX idx_vessels_real_type ON catalog_user.vessels_real(vessel_type);
                CREATE INDEX idx_vessels_real_displacement ON catalog_user.vessels_real(displacement_t);
                CREATE INDEX idx_vessels_real_speed ON catalog_user.vessels_real(service_speed_ms);
                CREATE INDEX idx_vessels_real_dims ON catalog_user.vessels_real(lpp_m, beam_m, draft_m);
                CREATE INDEX idx_vessels_real_cb ON catalog_user.vessels_real(cb);
                CREATE INDEX idx_vessels_real_system ON catalog_user.vessels_real(is_system_data);
            ");

            // Create GIN index for JSONB resistance curves
            migrationBuilder.Sql(@"
                CREATE INDEX idx_vessels_real_resistance_gin 
                ON catalog_user.vessels_real USING GIN(resistance_curve);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop table (will cascade drop indexes)
            migrationBuilder.Sql("DROP TABLE IF EXISTS catalog_user.vessels_real CASCADE;");
            
            // Drop schema
            migrationBuilder.Sql("DROP SCHEMA IF EXISTS catalog_user CASCADE;");
        }
    }
}
