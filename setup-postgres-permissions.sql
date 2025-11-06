-- Setup PostgreSQL permissions for aas_dev user
-- Run this as postgres superuser in SQL Shell (psql)

-- Give aas_dev user CREATEDB privilege
ALTER USER aas_dev CREATEDB;

-- Connect to aas_dev database
\c aas_dev

-- Grant all privileges on database
GRANT ALL PRIVILEGES ON DATABASE aas_dev TO aas_dev;

-- Grant all privileges on schema public
GRANT ALL ON SCHEMA public TO aas_dev;

-- Grant all privileges on existing tables and sequences
GRANT ALL ON ALL TABLES IN SCHEMA public TO aas_dev;
GRANT ALL ON ALL SEQUENCES IN SCHEMA public TO aas_dev;

-- Set default privileges for future objects
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON TABLES TO aas_dev;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON SEQUENCES TO aas_dev;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON FUNCTIONS TO aas_dev;

-- Verify permissions
\du aas_dev
\l aas_dev

-- Success!
SELECT 'PostgreSQL permissions configured successfully!' AS status;
