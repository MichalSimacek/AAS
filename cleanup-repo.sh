#!/bin/bash

echo "🧹 Cleaning up repository - keeping only essential files"

cd /app

# Create archive directory for old files
mkdir -p .archive

# Move old documentation files
echo "📝 Archiving old documentation..."
mv -f *.md .archive/ 2>/dev/null || true
mv .archive/README.md . 2>/dev/null

# Move old shell scripts
echo "🗑️  Removing old scripts..."
rm -f add-status-to-index.sh
rm -f backup.sh
rm -f complete-diagnostic-and-fix.sh
rm -f continue-after-disk-cleanup.sh
rm -f create-admin-account.sh
rm -f debug-static-files.sh
rm -f deploy.sh
rm -f diagnose-and-fix-errors.sh
rm -f diagnose-server-structure.sh
rm -f dotnet-install.sh
rm -f fix-*.sh
rm -f quick-fix-*.sh
rm -f rebuild-and-deploy-docker.sh
rm -f restart-deployment.sh
rm -f run-migration.sh
rm -f setup-production.sh
rm -f setup-ssl.sh
rm -f stop-and-redeploy.sh
rm -f test-static-files.sh
rm -f update-with-price-status.sh

# Remove old docker-compose files (keep only prod)
echo "🐋 Cleaning docker-compose files..."
rm -f docker-compose.yml
rm -f docker-compose.dev.yml
rm -f docker-compose.host.yml
rm -f docker-compose.production.yml
rm -f docker-compose.override.yml.example

# Remove old Dockerfile (keep only prod)
rm -f Dockerfile

# Remove SQL test files
rm -f create-admin-manual.sql
rm -f setup-postgres-permissions.sql
rm -f test-set-price.sql

# Remove PowerShell scripts
rm -f dev-setup.ps1

# Remove deployment folder if exists
rm -rf deployment/

# Remove certbot and backups if they exist
rm -rf certbot/
rm -rf backups/
rm -f protonmail-bridge_3.9.1-1_amd64.deb

echo ""
echo "✅ Cleanup complete!"
echo ""
echo "📦 Kept essential files:"
echo "  - README.md (new comprehensive guide)"
echo "  - docker-compose.prod.yml"
echo "  - Dockerfile.prod"
echo "  - docker-entrypoint.sh"
echo "  - nginx/"
echo "  - src/"
echo "  - AAS.sln"
echo ""
echo "📁 Old files moved to: .archive/"
echo ""
