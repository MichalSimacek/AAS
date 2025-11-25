#!/bin/bash
# Setup Remote Backup Sync to Master.cz
# Run this after getting the backup password

echo "==================================="
echo "Remote Backup Sync Setup"
echo "==================================="

BACKUP_SERVER="backup15.master.cz"
BACKUP_USER="bcp-id-9316"

read -p "Have you retrieved the backup password? (y/n): " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo "Please get password from: https://admin.masterdc.com/sharing/showpass?id=2908&hash=9081592-4602710001763-3161599"
    exit 1
fi

read -sp "Enter backup password: " BACKUP_PASSWORD
echo

# Test FTP connection
echo "Testing FTP connection..."
ftp -n $BACKUP_SERVER << EOF
user $BACKUP_USER $BACKUP_PASSWORD
pwd
bye
EOF

if [ $? -eq 0 ]; then
    echo "✅ FTP connection successful!"
else
    echo "❌ FTP connection failed!"
    exit 1
fi

# Create FTP sync script
echo "Creating remote sync script..."
cat > /AAS/sync-to-remote.sh << 'EOFSCRIPT'
#!/bin/bash
# Sync local backups to remote FTP server

BACKUP_SERVER="backup15.master.cz"
BACKUP_USER="bcp-id-9316"
LOCAL_BACKUP_DIR="/AAS/local-backups"

# Get password from environment or keyring
if [ -z "$BACKUP_PASSWORD" ]; then
    echo "ERROR: BACKUP_PASSWORD environment variable not set"
    exit 1
fi

# Get latest backup directory
LATEST_BACKUP=$(ls -td $LOCAL_BACKUP_DIR/*/ | head -1)

if [ -z "$LATEST_BACKUP" ]; then
    echo "No backups found to sync"
    exit 1
fi

BACKUP_NAME=$(basename $LATEST_BACKUP)

echo "[$(date)] Syncing backup: $BACKUP_NAME"

# Use lftp for robust FTP sync
lftp -c "
set ftp:ssl-allow no
open -u $BACKUP_USER,$BACKUP_PASSWORD $BACKUP_SERVER
mirror -R --verbose $LATEST_BACKUP /AAS/$BACKUP_NAME
bye
"

if [ $? -eq 0 ]; then
    echo "[$(date)] Remote sync successful!"
else
    echo "[$(date)] Remote sync failed!"
    exit 1
fi
EOFSCRIPT

chmod +x /AAS/sync-to-remote.sh

# Store password securely (you should use a better method in production)
echo "BACKUP_PASSWORD='$BACKUP_PASSWORD'" > /root/.backup_credentials
chmod 600 /root/.backup_credentials

# Update backup script to include remote sync
sed -i '/# 5. Sync to remote backup server/a\
source /root/.backup_credentials\
/AAS/sync-to-remote.sh' /AAS/backup.sh

echo ""
echo "✅ Remote sync configured!"
echo ""
echo "Test remote sync: source /root/.backup_credentials && /AAS/sync-to-remote.sh"
echo ""
