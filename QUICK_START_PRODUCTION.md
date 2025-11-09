# Quick Start - Production Deployment

Deploy Aristocratic Artwork Sale to production in **5 minutes**.

## Prerequisites

✅ Docker installed  
✅ Domain pointing to server (aristocraticartworksale.com → your-server-ip)  
✅ Ports 80 and 443 open in firewall  

## One-Command Setup

```bash
./setup-production.sh
```

This automated script will:
1. ✅ Check Docker installation
2. ✅ Create and validate `.env.production`
3. ✅ Verify DNS configuration
4. ✅ Check port availability
5. ✅ Obtain SSL certificates from Let's Encrypt
6. ✅ Start all services (Nginx, ASP.NET Core, PostgreSQL)

## Manual Setup (Alternative)

If you prefer manual control:

### Step 1: Configure Environment

```bash
cp .env.production.example .env.production
nano .env.production
```

Fill in required values:
- `DB_PASSWORD` - Strong database password
- `EMAIL_*` - SMTP configuration
- `ADMIN_EMAIL` / `ADMIN_PASSWORD` - Admin account

### Step 2: Obtain SSL Certificates

```bash
# Start Nginx (HTTP only)
docker-compose -f docker-compose.production.yml up -d nginx

# Request certificates
docker-compose -f docker-compose.production.yml run --rm certbot certonly \
  --webroot --webroot-path=/var/www/certbot \
  --email your@email.com --agree-tos --no-eff-email \
  -d aristocraticartworksale.com -d www.aristocraticartworksale.com
```

### Step 3: Start Production Stack

```bash
docker-compose -f docker-compose.production.yml down
docker-compose -f docker-compose.production.yml up -d
```

## Verify Deployment

```bash
# Check services
docker-compose -f docker-compose.production.yml ps

# View logs
docker-compose -f docker-compose.production.yml logs -f web

# Test HTTPS
curl -I https://aristocraticartworksale.com
```

## Access Application

🌐 **URL**: https://aristocraticartworksale.com

👤 **Admin Login**:
- Email: `admin@localhost` (or as configured)
- Password: As set in `.env.production`

## Common Commands

```bash
# View all logs
docker-compose -f docker-compose.production.yml logs -f

# Restart services
docker-compose -f docker-compose.production.yml restart

# Update application
git pull origin main
docker-compose -f docker-compose.production.yml up -d --build

# Stop everything
docker-compose -f docker-compose.production.yml down
```

## Troubleshooting

### "Port already in use"
```bash
sudo systemctl stop nginx apache2
```

### "Certificate not found"
Run the automated setup script again:
```bash
./setup-production.sh
```

### "Database connection error"
Check environment variables:
```bash
docker-compose -f docker-compose.production.yml exec web env | grep DB_
```

## Support

📖 **Full Documentation**: [PRODUCTION_DEPLOYMENT.md](PRODUCTION_DEPLOYMENT.md)  
🐛 **Issues**: https://github.com/MichalSimacek/AAS/issues

---

**Deployment should take 5-10 minutes total.** 🚀
