#!/bin/bash
# Backup Setup Script for AAS Application
# This script sets up automated backups to Master.cz backup server

echo "==================================="
echo "AAS Backup Setup Script"
echo "==================================="

# Backup configuration
BACKUP_SERVER="backup15.master.cz"
BACKUP_USER="bcp-id-9316"
BACKUP_PATH="/AAS/backups"
LOCAL_BACKUP_DIR="/AAS/local-backups"

# Create local backup directory
echo "Creating local backup directory..."
mkdir -p $LOCAL_BACKUP_DIR
chmod 700 $LOCAL_BACKUP_DIR

# Create backup script
echo "Creating backup script..."
cat > /AAS/backup.sh << 'EOF'
#!/bin/bash
# Automated Backup Script for AAS Application

# Configuration
BACKUP_DATE=$(date +%Y%m%d_%H%M%S)
LOCAL_BACKUP_DIR="/AAS/local-backups"
BACKUP_DIR="$LOCAL_BACKUP_DIR/$BACKUP_DATE"
RETENTION_DAYS=7

# Create backup directory
mkdir -p $BACKUP_DIR

echo "[$(date)] Starting backup..."

# 1. Backup PostgreSQL database
echo "[$(date)] Backing up PostgreSQL database..."
# Load environment variables from .env if exists
if [ -f "/AAS/.env" ]; then
    export $(grep -v '^#' /AAS/.env | xargs)
fi

# Use DB credentials from environment or defaults
DB_USER=${DB_USER:-aasuser}
DB_NAME=${DB_NAME:-aas_production}

docker exec aas-db-prod pg_dump -U $DB_USER $DB_NAME > $BACKUP_DIR/database.sql
if [ $? -eq 0 ]; then
    echo "[$(date)] Database backup successful"
    gzip $BACKUP_DIR/database.sql
else
    echo "[$(date)] ERROR: Database backup failed!"
fi

# 2. Backup uploaded files
echo "[$(date)] Backing up uploaded files..."
if [ -d "/AAS/uploads" ]; then
    tar -czf $BACKUP_DIR/uploads.tar.gz -C /AAS uploads
    echo "[$(date)] Uploads backup successful"
else
    echo "[$(date)] WARNING: Uploads directory not found"
fi

# 3. Backup configuration files
echo "[$(date)] Backing up configuration..."
tar -czf $BACKUP_DIR/config.tar.gz -C /AAS \
    docker-compose.prod.yml \
    src/AAS.Web/.env 2>/dev/null || echo "Some config files not found"

# 4. Create backup info file
cat > $BACKUP_DIR/backup_info.txt << EOFINFO
Backup Date: $(date)
Server: $(hostname)
Docker Containers:
$(docker ps --format "table {{.Names}}\t{{.Status}}")

Database Size: $(docker exec aas-db-prod psql -U postgres -d aas -c "SELECT pg_size_pretty(pg_database_size('aas'));" -t)

Disk Usage:
$(df -h /AAS)
EOFINFO

# 5. Sync to remote backup server
echo "[$(date)] Syncing to remote backup server..."
# Note: You need to set up SSH keys or use FTP
# For now, we'll keep backups local. Follow instructions below to set up remote sync.

# 6. Clean up old local backups (keep last 7 days)
echo "[$(date)] Cleaning up old backups..."
find $LOCAL_BACKUP_DIR -maxdepth 1 -type d -mtime +$RETENTION_DAYS -exec rm -rf {} \;

echo "[$(date)] Backup completed successfully!"
echo "[$(date)] Backup location: $BACKUP_DIR"
EOF

chmod +x /AAS/backup.sh

# Create cron job for daily backups at 2 AM
echo "Setting up cron job for daily backups..."
(crontab -l 2>/dev/null; echo "0 2 * * * /AAS/backup.sh >> /var/log/aas-backup.log 2>&1") | crontab -

echo ""
echo "==================================="
echo "Setup Complete!"
echo "==================================="
echo ""
echo "📁 Local backups will be stored in: $LOCAL_BACKUP_DIR"
echo "⏰ Automated daily backups at 2:00 AM"
echo "📝 Backup logs: /var/log/aas-backup.log"
echo ""
echo "Next steps:"
echo "1. Get backup password from: https://admin.masterdc.com/sharing/showpass?id=2908&hash=9081592-4602710001763-3161599"
echo "2. Run setup-remote-sync.sh to configure remote backups"
echo "3. Test backup manually: /AAS/backup.sh"
echo ""
