using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataService.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogMLParametricSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create catalog_ml schema for ML/Parametric hull catalog (read-only, system-managed)
            migrationBuilder.Sql("CREATE SCHEMA IF NOT EXISTS catalog_ml;");

            // Create parametric_hulls table
            migrationBuilder.Sql(@"
                CREATE TABLE catalog_ml.parametric_hulls (
                    id SERIAL PRIMARY KEY,
                    hull_id TEXT UNIQUE NOT NULL,
                    dataset_source TEXT NOT NULL,
                    row_index INTEGER NOT NULL,

                    -- 45 Parametric Vector (JSONB)
                    parametric_vector JSONB NOT NULL,

                    -- Key Parameters (Extracted)
                    loa_m DECIMAL(10,3) NOT NULL DEFAULT 10.0,
                    lb_ratio DECIMAL(6,4) NOT NULL,
                    ls_ratio DECIMAL(6,4) NOT NULL,
                    bd_ratio DECIMAL(8,6) NOT NULL,
                    dd_ratio DECIMAL(8,6) NOT NULL,
                    bs_ratio DECIMAL(6,4) NOT NULL,

                    -- Geometric Measures @ Design Draft (T/Dd = 0.5)
                    volume_norm DECIMAL(12,8) NOT NULL CHECK (volume_norm > 0),
                    lcb_norm DECIMAL(6,4) NOT NULL,
                    vcb_norm DECIMAL(6,4),
                    area_wp_norm DECIMAL(10,8) NOT NULL,
                    cw_coeff DECIMAL(5,4) NOT NULL,
                    area_ws_norm DECIMAL(10,8),
                    ixx_norm DECIMAL(12,8),
                    iyy_norm DECIMAL(12,8),

                    -- All Geometric Measures (JSONB - 10 draft ratios)
                    geometric_measures JSONB NOT NULL,

                    -- Derived Principal Dimensions
                    lpp_m_derived DECIMAL(10,3) NOT NULL CHECK (lpp_m_derived > 0),
                    beam_m_derived DECIMAL(10,3) NOT NULL CHECK (beam_m_derived > 0),
                    draft_m_derived DECIMAL(10,3) NOT NULL CHECK (draft_m_derived > 0),
                    depth_m_derived DECIMAL(10,3) NOT NULL CHECK (depth_m_derived > 0),

                    -- Derived Form Coefficients
                    cb_derived DECIMAL(5,4) NOT NULL CHECK (cb_derived BETWEEN 0.25 AND 0.98),
                    cp_derived DECIMAL(5,4) CHECK (cp_derived BETWEEN 0.50 AND 1.0),
                    cm_derived DECIMAL(5,4) CHECK (cm_derived BETWEEN 0.70 AND 1.0),

                    -- Quality Metrics
                    conversion_quality TEXT,
                    has_valid_coefficients BOOLEAN DEFAULT TRUE,
                    distortion_score DECIMAL(5,4),

                    -- Metadata
                    imported_at TIMESTAMPTZ DEFAULT NOW() NOT NULL,
                    data_version INTEGER DEFAULT 1,
                    is_active BOOLEAN DEFAULT TRUE
                );
            ");

            // Create indexes for performance (11 total)
            migrationBuilder.Sql(@"
                CREATE INDEX idx_ml_source ON catalog_ml.parametric_hulls(dataset_source);
                CREATE INDEX idx_ml_loa ON catalog_ml.parametric_hulls(loa_m);
                CREATE INDEX idx_ml_volume ON catalog_ml.parametric_hulls(volume_norm);
                CREATE INDEX idx_ml_lcb ON catalog_ml.parametric_hulls(lcb_norm);
                CREATE INDEX idx_ml_bd ON catalog_ml.parametric_hulls(bd_ratio);
                CREATE INDEX idx_ml_dd ON catalog_ml.parametric_hulls(dd_ratio);
                CREATE INDEX idx_ml_cb ON catalog_ml.parametric_hulls(cb_derived);
                CREATE INDEX idx_ml_beam_draft ON catalog_ml.parametric_hulls(beam_m_derived, draft_m_derived);
                CREATE INDEX idx_ml_active ON catalog_ml.parametric_hulls(is_active) WHERE is_active = TRUE;
            ");

            // Create JSONB GIN indexes (2 total)
            migrationBuilder.Sql(@"
                CREATE INDEX idx_ml_params_gin ON catalog_ml.parametric_hulls USING GIN(parametric_vector);
                CREATE INDEX idx_ml_geom_gin ON catalog_ml.parametric_hulls USING GIN(geometric_measures);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop table (will cascade drop indexes)
            migrationBuilder.Sql("DROP TABLE IF EXISTS catalog_ml.parametric_hulls CASCADE;");

            // Drop schema
            migrationBuilder.Sql("DROP SCHEMA IF EXISTS catalog_ml CASCADE;");
        }
    }
}
