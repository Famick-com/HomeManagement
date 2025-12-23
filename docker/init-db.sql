-- Initialize database for self-hosted deployment
-- This script runs automatically when the PostgreSQL container starts for the first time

-- Create fixed tenant record for self-hosted instance
INSERT INTO "Tenants" (
    "Id",
    "Name", 
    "Subdomain",
    "IsActive",
    "CreatedAt",
    "UpdatedAt"
) VALUES (
    '00000000-0000-0000-0000-000000000001'::uuid,
    'Self-Hosted Instance',
    'localhost',
    true,
    CURRENT_TIMESTAMP,
    CURRENT_TIMESTAMP
) ON CONFLICT ("Id") DO NOTHING;

-- Note: Default admin user should be created via the registration endpoint
-- or using the setup script after first deployment
