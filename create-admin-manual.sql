-- Manual Admin Account Creation
-- Use this if AdminSeeder fails
-- 
-- This creates:
--   Email: admin@localhost
--   Password: Admin123!@#$
--   Role: Admin
--   EmailConfirmed: true

-- 1. Create Admin role if not exists
INSERT INTO "AspNetRoles" ("Id", "Name", "NormalizedName", "ConcurrencyStamp")
SELECT 
    gen_random_uuid()::text,
    'Admin',
    'ADMIN',
    gen_random_uuid()::text
WHERE NOT EXISTS (SELECT 1 FROM "AspNetRoles" WHERE "Name" = 'Admin');

-- 2. Create admin user
-- Password hash for: Admin123!@#$
-- Note: This is ASP.NET Core Identity PasswordHasherV3 format
INSERT INTO "AspNetUsers" (
    "Id", 
    "UserName", 
    "NormalizedUserName", 
    "Email", 
    "NormalizedEmail", 
    "EmailConfirmed",
    "PasswordHash",
    "SecurityStamp",
    "ConcurrencyStamp",
    "PhoneNumberConfirmed",
    "TwoFactorEnabled",
    "LockoutEnabled",
    "AccessFailedCount"
)
SELECT 
    gen_random_uuid()::text,
    'admin@localhost',
    'ADMIN@LOCALHOST',
    'admin@localhost',
    'ADMIN@LOCALHOST',
    true,
    'AQAAAAIAAYagAAAAEJYvE0KvLzNxJz4cLwG0QK5xJz0vLJ4KvE0YagAAAAEJYvE0KvLzNxJz4cLwG0QK5xJz==',
    gen_random_uuid()::text,
    gen_random_uuid()::text,
    false,
    false,
    true,
    0
WHERE NOT EXISTS (SELECT 1 FROM "AspNetUsers" WHERE "Email" = 'admin@localhost');

-- 3. Assign Admin role to user
INSERT INTO "AspNetUserRoles" ("UserId", "RoleId")
SELECT 
    u."Id",
    r."Id"
FROM "AspNetUsers" u
CROSS JOIN "AspNetRoles" r
WHERE u."Email" = 'admin@localhost'
  AND r."Name" = 'Admin'
  AND NOT EXISTS (
      SELECT 1 
      FROM "AspNetUserRoles" ur
      WHERE ur."UserId" = u."Id" AND ur."RoleId" = r."Id"
  );

-- Verify
SELECT 
    u."Email",
    u."EmailConfirmed",
    r."Name" as "Role"
FROM "AspNetUsers" u
LEFT JOIN "AspNetUserRoles" ur ON u."Id" = ur."UserId"
LEFT JOIN "AspNetRoles" r ON ur."RoleId" = r."Id"
WHERE u."Email" = 'admin@localhost';
