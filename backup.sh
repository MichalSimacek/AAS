#!/bin/bash
set -e

BACKUP_DIR="backups"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
BACKUP_FILE="aas_backup_$TIMESTAMP"

mkdir -p $BACKUP_DIR

if [ -f .env.production ]; then
    export $(cat .env.production | grep -v '^#' | xargs)
fi

echo "Creating backup: $BACKUP_FILE"

# Database
docker-compose -f docker-compose.prod.yml exec -T db pg_dump -U ${DB_USER:-aasuser} ${DB_NAME:-aas_production} > $BACKUP_DIR/${BACKUP_FILE}_database.sql

# Uploads
tar -czf $BACKUP_DIR/${BACKUP_FILE}_uploads.tar.gz wwwroot/uploads/

# Config
cp .env.production $BACKUP_DIR/${BACKUP_FILE}_env.production 2>/dev/null || true

# Final archive
tar -czf $BACKUP_DIR/${BACKUP_FILE}.tar.gz -C $BACKUP_DIR ${BACKUP_FILE}_*

# Cleanup
rm $BACKUP_DIR/${BACKUP_FILE}_database.sql
rm $BACKUP_DIR/${BACKUP_FILE}_uploads.tar.gz
rm $BACKUP_DIR/${BACKUP_FILE}_env.production 2>/dev/null || true

echo "Backup completed: $BACKUP_DIR/${BACKUP_FILE}.tar.gz"
echo "Size: $(du -h $BACKUP_DIR/${BACKUP_FILE}.tar.gz | cut -f1)"

# Keep last 7 backups
ls -t $BACKUP_DIR/aas_backup_*.tar.gz | tail -n +8 | xargs -r rm
