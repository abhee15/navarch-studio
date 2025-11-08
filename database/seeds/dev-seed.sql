-- Seed development data
-- Run with: docker compose exec -T postgres psql -U postgres -d sri_template_dev -f /path/to/dev-seed.sql

-- Enable pgcrypto for password hashing
CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Seed users (password is 'password' for test users)
-- Using PostgreSQL's crypt() function for BCrypt hashing
INSERT INTO identity.users (id, email, name, password_hash, preferred_units, created_at, updated_at) VALUES
('550e8400-e29b-41d4-a716-446655440001', 'admin@example.com', 'Admin User', crypt('password', gen_salt('bf')), 'SI', NOW(), NOW()),
('550e8400-e29b-41d4-a716-446655440002', 'user@example.com', 'Test User', crypt('password', gen_salt('bf')), 'SI', NOW(), NOW()),
('550e8400-e29b-41d4-a716-446655440003', 'abhee15@gmail.com', 'Abhishikth', crypt('Abhishikth12345$', gen_salt('bf')), 'SI', NOW(), NOW())
ON CONFLICT (email) DO NOTHING;

-- Import hull family presets (required for hull sizing service)
-- Note: This is now included inline below instead of using \i to avoid path issues

-- Hull Family Presets Seed Data
-- Typical geometric ranges and form coefficients for common vessel types

INSERT INTO sizing.hull_family_presets (
    id, family, display_name, l_over_b_min, l_over_b_max, b_over_t_min, b_over_t_max,
    d_over_t_min, d_over_t_max, cb_min, cb_max, cp_min, cp_max, cwp_min, cwp_max,
    fn_min, fn_max, generator_type, is_active, notes
) VALUES
(uuid_generate_v4(), 'container', 'Container Ship', 6.50, 8.00, 2.80, 3.50, 1.30, 1.50, 0.60, 0.70, 0.70, 0.75, 0.75, 0.85, 0.22, 0.28, 'wigley', true, 'High-speed container vessels'),
(uuid_generate_v4(), 'tanker', 'Tanker', 5.00, 6.50, 2.00, 2.50, 1.20, 1.40, 0.78, 0.85, 0.82, 0.88, 0.85, 0.92, 0.12, 0.18, 'series60', true, 'VLCC/Suezmax/Aframax tankers'),
(uuid_generate_v4(), 'bulk', 'Bulk Carrier', 5.50, 7.00, 2.20, 2.80, 1.25, 1.45, 0.72, 0.80, 0.78, 0.84, 0.80, 0.88, 0.14, 0.20, 'series60', true, 'Bulk carriers'),
(uuid_generate_v4(), 'cargo', 'General Cargo', 5.50, 7.00, 2.50, 3.50, 1.30, 1.50, 0.62, 0.73, 0.72, 0.78, 0.75, 0.85, 0.16, 0.24, 'wigley', true, 'Multipurpose cargo vessels'),
(uuid_generate_v4(), 'roro', 'RoRo / Car Carrier', 6.00, 7.50, 3.50, 4.50, 1.60, 2.00, 0.55, 0.65, 0.65, 0.75, 0.70, 0.80, 0.20, 0.26, 'wigley', true, 'Roll-on/Roll-off vessels'),
(uuid_generate_v4(), 'lng', 'LNG Carrier', 5.50, 6.50, 2.20, 2.80, 1.30, 1.50, 0.70, 0.78, 0.75, 0.82, 0.78, 0.85, 0.18, 0.24, 'series60', true, 'LNG carriers'),
(uuid_generate_v4(), 'osv', 'Offshore Supply', 4.00, 5.50, 2.50, 3.50, 1.40, 1.80, 0.55, 0.70, 0.65, 0.75, 0.70, 0.82, 0.20, 0.30, 'wigley', true, 'Platform supply vessels'),
(uuid_generate_v4(), 'fishing', 'Fishing Vessel', 3.50, 5.00, 2.80, 3.80, 1.40, 1.70, 0.50, 0.65, 0.60, 0.72, 0.70, 0.82, 0.22, 0.32, 'wigley', true, 'Fishing trawlers'),
(uuid_generate_v4(), 'tug', 'Tugboat', 3.00, 4.50, 2.50, 3.80, 1.30, 1.60, 0.48, 0.62, 0.58, 0.70, 0.68, 0.80, 0.15, 0.28, 'wigley', true, 'Harbor and ocean-going tugs'),
(uuid_generate_v4(), 'yacht_disp', 'Displacement Yacht', 5.00, 7.00, 3.00, 4.50, 1.40, 1.80, 0.42, 0.58, 0.55, 0.68, 0.65, 0.78, 0.25, 0.38, 'wigley', true, 'Motor yachts (displacement mode)'),
(uuid_generate_v4(), 'ferry_fast', 'Fast Ferry', 7.00, 9.00, 3.50, 5.00, 1.50, 2.00, 0.38, 0.52, 0.52, 0.65, 0.60, 0.75, 0.35, 0.50, 'wigley', true, 'High-speed passenger ferries'),
(uuid_generate_v4(), 'ferry_conv', 'Conventional Ferry', 5.50, 7.00, 3.00, 4.00, 1.50, 1.90, 0.55, 0.68, 0.65, 0.75, 0.72, 0.82, 0.22, 0.30, 'wigley', true, 'Conventional displacement ferries'),
(uuid_generate_v4(), 'research', 'Research Vessel', 5.00, 6.50, 2.80, 3.80, 1.40, 1.70, 0.52, 0.68, 0.62, 0.75, 0.70, 0.82, 0.18, 0.26, 'wigley', true, 'Oceanographic research vessels'),
(uuid_generate_v4(), 'patrol', 'Patrol Boat', 5.50, 7.50, 3.00, 4.50, 1.40, 1.80, 0.45, 0.62, 0.58, 0.72, 0.65, 0.78, 0.28, 0.42, 'wigley', true, 'Military and coast guard patrol craft'),
(uuid_generate_v4(), 'barge', 'Barge', 5.00, 8.00, 2.00, 3.50, 1.15, 1.35, 0.85, 0.95, 0.90, 0.98, 0.88, 0.95, 0.08, 0.15, 'series60', true, 'Pushed/towed barges')
ON CONFLICT (family) DO UPDATE SET
    display_name = EXCLUDED.display_name,
    l_over_b_min = EXCLUDED.l_over_b_min,
    l_over_b_max = EXCLUDED.l_over_b_max,
    b_over_t_min = EXCLUDED.b_over_t_min,
    b_over_t_max = EXCLUDED.b_over_t_max,
    d_over_t_min = EXCLUDED.d_over_t_min,
    d_over_t_max = EXCLUDED.d_over_t_max,
    cb_min = EXCLUDED.cb_min,
    cb_max = EXCLUDED.cb_max,
    cp_min = EXCLUDED.cp_min,
    cp_max = EXCLUDED.cp_max,
    cwp_min = EXCLUDED.cwp_min,
    cwp_max = EXCLUDED.cwp_max,
    fn_min = EXCLUDED.fn_min,
    fn_max = EXCLUDED.fn_max,
    generator_type = EXCLUDED.generator_type,
    is_active = EXCLUDED.is_active,
    notes = EXCLUDED.notes;

-- Insert default KPI weights (system-wide)
INSERT INTO sizing.kpi_weights (id, user_id, metric, weight) VALUES
(uuid_generate_v4(), NULL, 'delta_balance', 0.35),
(uuid_generate_v4(), NULL, 'installed_power', 0.25),
(uuid_generate_v4(), NULL, 'constraints_ok', 0.20),
(uuid_generate_v4(), NULL, 'stability_screen', 0.10),
(uuid_generate_v4(), NULL, 'teu_or_volume_fit', 0.10)
ON CONFLICT DO NOTHING;
