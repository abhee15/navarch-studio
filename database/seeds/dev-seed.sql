-- Seed development data
-- Run with: docker compose exec -T postgres psql -U postgres -d sri_template_dev -f /path/to/dev-seed.sql

-- Seed users (password is 'password' hashed with BCrypt)
INSERT INTO identity."Users" ("Id", "Email", "Name", "PasswordHash", "PreferredUnits", "CreatedAt", "UpdatedAt") VALUES
('550e8400-e29b-41d4-a716-446655440001', 'admin@example.com', 'Admin User', '$2a$10$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi', 'SI', NOW(), NOW()),
('550e8400-e29b-41d4-a716-446655440002', 'user@example.com', 'Test User', '$2a$10$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi', 'SI', NOW(), NOW())
ON CONFLICT ("Email") DO NOTHING;

-- Add seed data for application-specific entities as needed
