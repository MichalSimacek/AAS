# AAS - Příkazový Cheat Sheet

## 🚀 Quick Start

```bash
# 1. Stažení projektu
git clone <repo-url> /var/www/aas
cd /var/www/aas

# 2. Automatická instalace
chmod +x quick-deploy.sh
./quick-deploy.sh

# 3. Přístup
http://your-server-ip:5000
```

---

## 🐳 Docker Commands

```bash
# Start
docker-compose -f docker-compose.prod.yml up -d

# Stop
docker-compose -f docker-compose.prod.yml down

# Restart
docker-compose -f docker-compose.prod.yml restart

# Rebuild
docker-compose -f docker-compose.prod.yml build --no-cache web

# Logs
docker logs -f aas-web-prod
docker logs -f aas-db-prod

# Status
docker-compose -f docker-compose.prod.yml ps

# Shell do containeru
docker exec -it aas-web-prod bash
docker exec -it aas-db-prod psql -U aas_user -d aas_production
```

---

## ⚙️ Systemd Commands

```bash
# Start
sudo systemctl start aas-web

# Stop
sudo systemctl stop aas-web

# Restart
sudo systemctl restart aas-web

# Status
sudo systemctl status aas-web

# Logs
sudo journalctl -u aas-web -f
sudo journalctl -u aas-web --since "1 hour ago"

# Enable auto-start
sudo systemctl enable aas-web
```

---

## 🔧 Nginx Commands

```bash
# Test konfigurace
sudo nginx -t

# Reload
sudo systemctl reload nginx

# Restart
sudo systemctl restart nginx

# Logs
sudo tail -f /var/log/nginx/access.log
sudo tail -f /var/log/nginx/error.log

# Status
sudo systemctl status nginx
```

---

## 🗄️ Database Commands

```bash
# Připojení k databázi
psql -h localhost -U aas_user -d aas_production

# Backup
pg_dump -h localhost -U aas_user aas_production > backup.sql

# Restore
psql -h localhost -U aas_user aas_production < backup.sql

# Docker databáze
docker exec -it aas-db-prod psql -U aas_user -d aas_production

# Zobrazit tabulky
\dt

# Quit
\q
```

---

## 📊 Monitoring

```bash
# Disk usage
df -h

# Memory usage
free -h

# CPU usage
top
htop

# Network connections
sudo netstat -tulpn | grep :5000

# Process list
ps aux | grep dotnet
ps aux | grep nginx
```

---

## 🔄 Update & Maintenance

```bash
# Update aplikace
cd /var/www/aas
git pull

# Docker metoda
docker-compose -f docker-compose.prod.yml build --no-cache web
docker-compose -f docker-compose.prod.yml up -d

# Systemd metoda
cd src/AAS.Web
dotnet publish -c Release -o /var/www/aas-app
sudo systemctl restart aas-web

# Update systému
sudo apt update
sudo apt upgrade -y
```

---

## 🔒 SSL/HTTPS (Certbot)

```bash
# Získání certifikátu
sudo certbot --nginx -d yourdomain.com

# Renewal test
sudo certbot renew --dry-run

# Manual renewal
sudo certbot renew

# List certifikátů
sudo certbot certificates

# Revoke certifikát
sudo certbot revoke --cert-name yourdomain.com
```

---

## 🛡️ Firewall (UFW)

```bash
# Status
sudo ufw status

# Enable
sudo ufw enable

# Allow ports
sudo ufw allow 22/tcp
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp

# Deny port
sudo ufw deny 5000/tcp

# Delete rule
sudo ufw delete allow 80/tcp

# Reset
sudo ufw reset
```

---

## 🧹 Cleanup

```bash
# Docker cleanup
docker system prune -a
docker volume prune

# Logs cleanup
sudo journalctl --vacuum-time=7d
sudo journalctl --vacuum-size=100M

# Apt cleanup
sudo apt autoremove -y
sudo apt autoclean
```

---

## 🚨 Troubleshooting

```bash
# Kontrola portu 5000
sudo lsof -i :5000
sudo netstat -tulpn | grep :5000

# Kill proces na portu
sudo kill -9 $(sudo lsof -t -i:5000)

# Kontrola app běhu
curl http://localhost:5000
curl -I http://localhost:5000

# DNS test
nslookup yourdomain.com
dig yourdomain.com

# Permissions fix
sudo chown -R $USER:$USER /var/www/aas
sudo chmod -R 755 /var/www/aas
```

---

## 📝 Logs Locations

```bash
# Application logs
/var/log/supervisor/aas-web.log          # Docker
sudo journalctl -u aas-web               # Systemd

# Nginx logs
/var/log/nginx/access.log
/var/log/nginx/error.log

# PostgreSQL logs
/var/log/postgresql/postgresql-*.log

# System logs
/var/log/syslog
```

---

## 🔑 Environment Variables

```bash
# View environment
env | grep ASPNETCORE

# Docker .env file
cat .env

# Systemd environment
sudo systemctl show aas-web | grep Environment
```

---

## 📦 Package Management

```bash
# .NET packages
dotnet list package
dotnet add package PackageName
dotnet restore

# System packages
sudo apt search package-name
sudo apt show package-name
sudo apt list --installed | grep package
```

---

## 🎯 Quick Fixes

### Aplikace nenaběhne
```bash
# Kontrola logů
docker logs aas-web-prod
sudo journalctl -u aas-web -f

# Restart
docker-compose restart
sudo systemctl restart aas-web
```

### 502 Bad Gateway
```bash
# Kontrola backend
curl http://localhost:5000

# Restart služeb
sudo systemctl restart aas-web
sudo systemctl restart nginx
```

### Database connection failed
```bash
# Kontrola PostgreSQL
sudo systemctl status postgresql

# Test connection
psql -h localhost -U aas_user -d aas_production

# Restart DB
sudo systemctl restart postgresql
```

### Port 5000 already in use
```bash
# Find process
sudo lsof -i :5000

# Kill process
sudo kill -9 <PID>
```

---

## 📚 Další zdroje

- Kompletní návod: `UBUNTU_DEPLOYMENT.md`
- Project dokumentace: `PROJECT-GUIDE.md`
- Docker deployment: `DEPLOYMENT.md`
