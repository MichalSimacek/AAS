╔═══════════════════════════════════════════════════════════════╗
║   ARISTOCRATIC ARTWORK SALE - LOCAL DEVELOPMENT SETUP        ║
╚═══════════════════════════════════════════════════════════════╝

🚀 QUICK START (3 KROKY):

1. OTEVŘI POWERSHELL V ROOT SLOŽCE:
   cd C:\AAS

2. SPUSŤ SETUP SCRIPT:
   .\dev-setup.ps1

3. OTEVŘI VS CODE A STISKNI F5:
   code .
   (Pak stiskni F5)

═══════════════════════════════════════════════════════════════

📚 CO SETUP SCRIPT UDĚLÁ:

✅ Zkontroluje Docker a .NET SDK
✅ Spustí PostgreSQL v Dockeru
✅ Restoruje NuGet packages
✅ Aplikuje database migrations
✅ Vytvoří upload složky

═══════════════════════════════════════════════════════════════

🔐 TEST CREDENTIALS:

Admin:
  Email: admin@localhost
  Password: Admin123!@#
  URL: http://localhost:5000/Identity/Account/Login

Database:
  Host: localhost:5432
  Database: aas_dev
  Username: aas_dev
  Password: dev_password_123

═══════════════════════════════════════════════════════════════

🎮 DEBUGGING OPTIONS (F5):

1. "🚀 Launch Web" - Normální debug (DEFAULT)
2. "🔧 Launch with Watch" - Hot reload
3. "🐛 Attach to Process" - Připojení k běžícímu procesu

═══════════════════════════════════════════════════════════════

🛠️ UŽITEČNÉ PŘÍKAZY:

# Start PostgreSQL:
docker-compose -f docker-compose.dev.yml up -d postgres

# Stop all services:
docker-compose -f docker-compose.dev.yml down

# View logs:
docker logs aas_dev_postgres -f

# Add migration:
dotnet ef migrations add MigrationName --project src/AAS.Web

# Reset database:
dotnet ef database drop --force --project src/AAS.Web
dotnet ef database update --project src/AAS.Web

═══════════════════════════════════════════════════════════════

🌐 OPTIONAL SERVICES:

MailHog (Email testing):
  docker-compose -f docker-compose.dev.yml up -d mailhog
  Web UI: http://localhost:8025

pgAdmin (Database UI):
  docker-compose -f docker-compose.dev.yml up -d pgadmin
  Web UI: http://localhost:5050
  Login: admin@localhost / admin

═══════════════════════════════════════════════════════════════

📖 DETAILNÍ DOKUMENTACE:

Viz DEVELOPMENT.md pro kompletní návod!

═══════════════════════════════════════════════════════════════

🎉 HAPPY CODING!
