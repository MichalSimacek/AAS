# Aristocratic Artwork Sale - Production Application

## 🎯 Overview

Professional ASP.NET Core 8.0 web application for luxury art sales featuring:
- Multi-language support (10 languages)
- Admin panel for collection management
- Image galleries with responsive design
- Email notifications with PDF attachments
- Status tracking (Available/Sold) with pricing
- PostgreSQL database with Entity Framework Core

## 🚀 Quick Deployment

### Prerequisites
- Ubuntu/Debian server with Docker and Docker Compose
- Domain with DNS pointing to server
- Ports 80 and 443 open

### 1. Clone Repository
```bash
git clone <your-repo-url> /AAS
cd /AAS
```

### 2. Configure Environment
```bash
cp .env.example .env.production
nano .env.production
```

Required environment variables:
```bash
# Database
DB_HOST=db
DB_PORT=5432
DB_NAME=aas_db
DB_USER=postgres
DB_PASSWORD=<strong-password>

# Admin Account
ADMIN_EMAIL=admin@yourdomain.com
ADMIN_PASSWORD=<strong-password>

# Email (SMTP)
EMAIL_HOST=smtp.gmail.com
EMAIL_PORT=587
EMAIL_USERNAME=your-email@gmail.com
EMAIL_PASSWORD=<app-password>
EMAIL_FROM=noreply@yourdomain.com

# Application
DOMAIN=yourdomain.com
ASPNETCORE_ENVIRONMENT=Production
```

### 3. Deploy
```bash
# Build and start all services
sudo docker compose -f docker-compose.prod.yml build --no-cache
sudo docker compose -f docker-compose.prod.yml up -d

# Check logs
docker logs -f aas-web-prod

# Setup SSL (Let's Encrypt)
sudo docker compose -f docker-compose.prod.yml run --rm certbot certonly \
  --webroot --webroot-path=/var/www/certbot \
  -d yourdomain.com -d www.yourdomain.com \
  --email your-email@example.com --agree-tos --no-eff-email
```

### 4. Verify Deployment
- Visit: `https://yourdomain.com`
- Admin panel: `https://yourdomain.com/Admin/Collections`
- Check container status: `docker ps`

## 🔄 Updates & Maintenance

### Update Application
```bash
cd /AAS
git pull origin main
sudo docker compose -f docker-compose.prod.yml build --no-cache web
sudo docker compose -f docker-compose.prod.yml up -d --force-recreate web
```

### View Logs
```bash
# Application logs
docker logs -f aas-web-prod

# Database logs
docker logs -f aas-db-prod

# Nginx logs
docker logs -f aas-nginx-prod
```

### Backup Database
```bash
docker exec aas-db-prod pg_dump -U postgres aas_db > backup_$(date +%Y%m%d).sql
```

### Restore Database
```bash
cat backup_20241110.sql | docker exec -i aas-db-prod psql -U postgres aas_db
```

## 📁 Project Structure

```
/AAS/
├── src/AAS.Web/              # ASP.NET Core application
│   ├── Controllers/          # MVC & API controllers
│   ├── Models/               # Entity models
│   ├── Views/                # Razor views
│   ├── Areas/Admin/          # Admin panel
│   ├── Services/             # Business logic
│   ├── Database/             # EF Core & migrations
│   ├── Resources/            # Localization files
│   └── wwwroot/              # Static files
├── nginx/
│   └── nginx.prod.conf       # Nginx configuration
├── docker-compose.prod.yml   # Production Docker setup
├── Dockerfile.prod           # Application Docker image
├── docker-entrypoint.sh      # Container startup script
└── .env.production           # Environment variables
```

## 🔧 Admin Panel Features

### Collections Management
- Create/Edit/Delete collections
- Upload multiple images per collection
- Set status: Available/Sold
- Set price with currency (EUR/USD)
- Categorize: Paintings, Jewelry, Watches, Statues, Other
- Automatic multi-language translation

### Image Management
- Drag & drop reordering
- Automatic resizing (480px, 960px, 1600px)
- Delete individual images
- Responsive galleries

## 🌐 Multi-Language Support

Supported languages:
- English (en) - Default
- Czech (cs)
- Russian (ru)
- German (de)
- Spanish (es)
- French (fr)
- Portuguese (pt)
- Hindi (hi)
- Japanese (ja)
- Chinese (zh)

## 🔒 Security Features

- HTTPS/SSL encryption
- Strong password policies
- Anti-CSRF tokens
- SQL injection prevention
- XSS protection
- Rate limiting on forms
- Secure file uploads

## 📧 Email Configuration

The application sends emails for:
1. Registration confirmations (from: noreply@yourdomain.com)
2. Inquiry notifications (to: inquiry@yourdomain.com)

Configure SMTP in `.env.production`:
```bash
EMAIL_HOST=smtp.gmail.com
EMAIL_PORT=587
EMAIL_USERNAME=your-email@gmail.com
EMAIL_PASSWORD=<app-password>
```

For Gmail: Create an App Password at https://myaccount.google.com/apppasswords

## 🐛 Troubleshooting

### Container won't start
```bash
# Check logs
docker logs aas-web-prod

# Verify environment variables
docker exec aas-web-prod env | grep DB_

# Restart all services
sudo docker compose -f docker-compose.prod.yml restart
```

### Database connection errors
```bash
# Check database is running
docker exec aas-db-prod pg_isready -U postgres

# Test connection
docker exec aas-db-prod psql -U postgres -d aas_db -c "SELECT 1;"
```

### Changes not appearing
```bash
# Full rebuild (this takes 2-5 minutes)
cd /AAS
git pull origin main
sudo docker compose -f docker-compose.prod.yml down web
sudo docker compose -f docker-compose.prod.yml build --no-cache web
sudo docker compose -f docker-compose.prod.yml up -d web
```

### SSL certificate issues
```bash
# Renew certificates
sudo docker compose -f docker-compose.prod.yml run --rm certbot renew

# Reload nginx
sudo docker compose -f docker-compose.prod.yml restart nginx
```

## 📊 Monitoring

### Health Checks
```bash
# Application
curl -I http://localhost:5000

# Database
docker exec aas-db-prod pg_isready

# All containers
docker ps
```

### Performance
```bash
# Container stats
docker stats aas-web-prod aas-db-prod aas-nginx-prod

# Disk usage
docker system df
```

## 🆘 Support

For issues or questions:
1. Check logs: `docker logs -f aas-web-prod`
2. Verify environment variables in `.env.production`
3. Ensure all containers are running: `docker ps`
4. Check database connectivity

## 📝 Version History

### v1.0 (STABLE) - November 10, 2024
- ✅ Fixed Admin Edit form: Status, Currency, and Price now save correctly
- ✅ Added explicit form field names for proper model binding
- ✅ Implemented debug logging for troubleshooting
- ✅ Cleaned up repository structure
- ✅ Production-ready deployment with Docker

## 📜 License

Copyright © 2024 Aristocratic Artwork Sale. All rights reserved.
