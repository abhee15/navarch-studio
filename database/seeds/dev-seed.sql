-- Seed development data
-- Run with: docker compose exec -T postgres psql -U postgres -d sri_template_dev -f /path/to/dev-seed.sql

-- Seed users (password is 'password' hashed with BCrypt)
INSERT INTO identity."Users" ("Id", "Email", "Name", "PasswordHash", "CreatedAt", "UpdatedAt") VALUES
('550e8400-e29b-41d4-a716-446655440001', 'admin@example.com', 'Admin User', '$2a$10$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi', NOW(), NOW()),
('550e8400-e29b-41d4-a716-446655440002', 'user@example.com', 'Test User', '$2a$10$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi', NOW(), NOW())
ON CONFLICT ("Email") DO NOTHING;

-- Seed products
INSERT INTO data."Products" ("Id", "Name", "Price", "Description", "CreatedAt", "UpdatedAt") VALUES
('660e8400-e29b-41d4-a716-446655440001', 'Laptop', 999.99, 'High-performance laptop', NOW(), NOW()),
('660e8400-e29b-41d4-a716-446655440002', 'Mouse', 29.99, 'Wireless mouse', NOW(), NOW()),
('660e8400-e29b-41d4-a716-446655440003', 'Keyboard', 79.99, 'Mechanical keyboard', NOW(), NOW()),
('660e8400-e29b-41d4-a716-446655440004', 'Monitor', 299.99, '27-inch 4K monitor', NOW(), NOW())
ON CONFLICT ("Id") DO NOTHING;
