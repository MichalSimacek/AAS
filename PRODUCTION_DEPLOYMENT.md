# Production Deployment Guide

Complete guide for deploying Aristocratic Artwork Sale to production with Docker, Nginx, and Let's Encrypt SSL.

## Architecture

```
Internet (port 80/443)
    ↓
Nginx Container (reverse proxy + SSL termination)
    ↓
ASP.NET Core Container (port 8080)
    ↓
PostgreSQL Container
```

## Prerequisites

- Docker and Docker Compose installed
- Domain name (aristocraticartworksale.com) pointing to your server IP
- Port 80 and 443 open in firewall

## Step 1: Clone Repository

```bash
git clone https://github.com/MichalSimacek/AAS.git
cd AAS
```

## Step 2: Configure Environment Variables

Create `.env.production` file:

```bash
cp .env.production.example .env.production
nano .env.production
```

Fill in all required values:

```bash
# Database
DB_NAME=aas_prod
DB_USER=aas
DB_PASSWORD=<generate-strong-password>

# Email (ProtonMail Bridge or other SMTP)
EMAIL_SMTP_HOST=127.0.0.1
EMAIL_SMTP_PORT=1025
EMAIL_USE_STARTTLS=false
EMAIL_USERNAME=Michalsimacek@protonmail.com
EMAIL_PASSWORD=<your-protonmail-bridge-password>
EMAIL_FROM=noreply@aristocraticartworksale.com
EMAIL_TO=inquiry@aristocraticartworksale.com

# Admin Account
ADMIN_EMAIL=admin@localhost
ADMIN_PASSWORD=<strong-password-min-12-chars>

# Translation (optional)
TRANSLATION_ENABLED=false
```

## Step 3: Initial Setup (Without SSL)

First, we need to start Nginx without SSL to obtain certificates:

### 3.1. Create temporary Nginx config

```bash
cat > nginx/nginx.init.conf << 'EOF'
user nginx;
worker_processes auto;
error_log /var/log/nginx/error.log warn;
pid /var/run/nginx.pid;

events {
    worker_connections 1024;
}

http {
    include /etc/nginx/mime.types;
    default_type application/octet-stream;
    
    server {
        listen 80;
        listen [::]:80;
        server_name aristocraticartworksale.com www.aristocraticartworksale.com;

        location /.well-known/acme-challenge/ {
            root /var/www/certbot;
        }

        location / {
            return 200 "Server is ready for SSL setup\n";
            add_header Content-Type text/plain;
        }
    }
}
EOF
```

### 3.2. Modify docker-compose temporarily

```bash
# Backup original
cp docker-compose.production.yml docker-compose.production.yml.backup

# Edit to use init config
sed -i 's|nginx.prod.conf|nginx.init.conf|g' docker-compose.production.yml
```

### 3.3. Start Nginx only (for certificate generation)

```bash
docker-compose -f docker-compose.production.yml up -d nginx
```

## Step 4: Obtain SSL Certificates

```bash
# Request certificates from Let's Encrypt
docker-compose -f docker-compose.production.yml run --rm certbot certonly \
  --webroot \
  --webroot-path=/var/www/certbot \
  --email your-email@example.com \
  --agree-tos \
  --no-eff-email \
  -d aristocraticartworksale.com \
  -d www.aristocraticartworksale.com

# Verify certificates were created
sudo ls -la certbot/conf/live/aristocraticartworksale.com/
```

You should see:
- `fullchain.pem`
- `privkey.pem`

## Step 5: Switch to Production Configuration

```bash
# Restore production config
mv docker-compose.production.yml.backup docker-compose.production.yml

# Stop temporary setup
docker-compose -f docker-compose.production.yml down

# Start full production stack
docker-compose -f docker-compose.production.yml up -d
```

## Step 6: Verify Deployment

```bash
# Check all containers are running
docker-compose -f docker-compose.production.yml ps

# Check logs
docker-compose -f docker-compose.production.yml logs -f web

# Test HTTP → HTTPS redirect
curl -I http://aristocraticartworksale.com

# Test HTTPS
curl -I https://aristocraticartworksale.com

# Test SSL grade (optional)
# Visit: https://www.ssllabs.com/ssltest/analyze.html?d=aristocraticartworksale.com
```

## Step 7: Access Application

Open in browser:
- https://aristocraticartworksale.com

Login as admin:
- Email: admin@localhost
- Password: <your-admin-password>

## Maintenance

### View Logs

```bash
# All services
docker-compose -f docker-compose.production.yml logs -f

# Specific service
docker-compose -f docker-compose.production.yml logs -f web
docker-compose -f docker-compose.production.yml logs -f nginx
docker-compose -f docker-compose.production.yml logs -f db
```

### Restart Services

```bash
# All services
docker-compose -f docker-compose.production.yml restart

# Specific service
docker-compose -f docker-compose.production.yml restart web
```

### Update Application

```bash
# Pull latest code
git pull origin main

# Rebuild and restart
docker-compose -f docker-compose.production.yml up -d --build
```

### Certificate Renewal

Certificates auto-renew via certbot container. To manually renew:

```bash
docker-compose -f docker-compose.production.yml run --rm certbot renew
docker-compose -f docker-compose.production.yml restart nginx
```

### Database Backup

```bash
# Create backup
docker-compose -f docker-compose.production.yml exec db \
  pg_dump -U aas aas_prod > backup_$(date +%Y%m%d_%H%M%S).sql

# Restore backup
cat backup_file.sql | docker-compose -f docker-compose.production.yml exec -T db \
  psql -U aas aas_prod
```

## Troubleshooting

### Nginx won't start (certificate error)

If Nginx fails because certificates don't exist yet, use the init config from Step 3.

### Port 80/443 already in use

```bash
# Find what's using the port
sudo lsof -i :80
sudo lsof -i :443

# If it's another nginx/apache, stop it
sudo systemctl stop nginx
sudo systemctl stop apache2
```

### Database connection error

Check environment variables:
```bash
docker-compose -f docker-compose.production.yml exec web env | grep DB_
```

### Check application health

```bash
# Internal health check
docker-compose -f docker-compose.production.yml exec web curl http://localhost:8080/

# External health check
curl https://aristocraticartworksale.com/health
```

## Security Checklist

- ✅ SSL certificates configured
- ✅ HTTPS redirect enabled
- ✅ Strong admin password set
- ✅ Database password is strong and unique
- ✅ Email credentials configured
- ✅ Rate limiting enabled
- ✅ Security headers configured
- ✅ File upload size limited
- ✅ Non-root user in containers

## Performance Tips

1. **Enable HTTP/2**: Already configured in nginx.prod.conf
2. **Gzip compression**: Already enabled for text/css/js
3. **Static file caching**: Set to 1 year for immutable assets
4. **Connection pooling**: Database connection pool configured
5. **Memory limits**: Set in docker-compose for stability

## Support

For issues or questions:
- Check logs: `docker-compose -f docker-compose.production.yml logs`
- GitHub Issues: https://github.com/MichalSimacek/AAS/issues
