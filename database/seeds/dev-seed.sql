-- Seed development data
-- Run with: docker compose exec -T postgres psql -U postgres -d sri_template_dev -f /path/to/dev-seed.sql

-- Enable pgcrypto for password hashing
CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- Seed users (password is 'password' for test users)
-- Using PostgreSQL's crypt() function for BCrypt hashing
INSERT INTO identity.users (id, email, name, password_hash, preferred_units, created_at, updated_at) VALUES
('550e8400-e29b-41d4-a716-446655440001', 'admin@example.com', 'Admin User', crypt('password', gen_salt('bf')), 'SI', NOW(), NOW()),
('550e8400-e29b-41d4-a716-446655440002', 'user@example.com', 'Test User', crypt('password', gen_salt('bf')), 'SI', NOW(), NOW()),
('550e8400-e29b-41d4-a716-446655440003', 'abhee15@gmail.com', 'Abhishikth', crypt('Abhishikth12345$', gen_salt('bf')), 'SI', NOW(), NOW())
ON CONFLICT (email) DO NOTHING;

-- Add seed data for application-specific entities as needed
