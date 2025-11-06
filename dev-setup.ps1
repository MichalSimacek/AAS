# Aristocratic Artwork Sale - Development Setup Script
# Run this script to setup local development environment

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "AAS - Development Setup" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if Docker is installed
Write-Host "Checking Docker..." -ForegroundColor Yellow
if (!(Get-Command docker -ErrorAction SilentlyContinue)) {
    Write-Host "❌ Docker is not installed!" -ForegroundColor Red
    Write-Host "Please install Docker Desktop from: https://www.docker.com/products/docker-desktop" -ForegroundColor Yellow
    exit 1
}
Write-Host "✅ Docker is installed" -ForegroundColor Green

# Check if .NET SDK is installed
Write-Host "Checking .NET SDK..." -ForegroundColor Yellow
if (!(Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host "❌ .NET SDK is not installed!" -ForegroundColor Red
    Write-Host "Please install .NET 8 SDK from: https://dotnet.microsoft.com/download" -ForegroundColor Yellow
    exit 1
}
$dotnetVersion = dotnet --version
Write-Host "✅ .NET SDK $dotnetVersion is installed" -ForegroundColor Green

# Setup HTTPS development certificate
Write-Host ""
Write-Host "Setting up HTTPS development certificate..." -ForegroundColor Yellow
dotnet dev-certs https --trust
if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ HTTPS certificate is trusted" -ForegroundColor Green
}
else {
    Write-Host "⚠️  HTTPS certificate setup failed or was cancelled" -ForegroundColor Yellow
    Write-Host "You can run 'dotnet dev-certs https --trust' manually later" -ForegroundColor Yellow
}

# Start PostgreSQL in Docker
Write-Host ""
Write-Host "Starting PostgreSQL in Docker..." -ForegroundColor Yellow
docker-compose -f docker-compose.dev.yml up -d postgres

# Wait for PostgreSQL to be ready
Write-Host "Waiting for PostgreSQL to be ready..." -ForegroundColor Yellow
$attempts = 0
$maxAttempts = 30
while ($attempts -lt $maxAttempts) {
    $result = docker exec aas_dev_postgres pg_isready -U aas_dev 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ PostgreSQL is ready" -ForegroundColor Green
        break
    }
    $attempts++
    Start-Sleep -Seconds 1
}

if ($attempts -eq $maxAttempts) {
    Write-Host "❌ PostgreSQL failed to start" -ForegroundColor Red
    exit 1
}

# Restore NuGet packages
Write-Host ""
Write-Host "Restoring NuGet packages..." -ForegroundColor Yellow
Set-Location src\AAS.Web
dotnet restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Failed to restore packages" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Packages restored" -ForegroundColor Green

# Apply database migrations
Write-Host ""
Write-Host "Applying database migrations..." -ForegroundColor Yellow
dotnet ef database update
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Failed to apply migrations" -ForegroundColor Red
    Write-Host "Run this command manually: dotnet ef database update" -ForegroundColor Yellow
}
else {
    Write-Host "✅ Database migrations applied" -ForegroundColor Green
}

# Create upload directories
Write-Host ""
Write-Host "Creating upload directories..." -ForegroundColor Yellow
New-Item -ItemType Directory -Force -Path "wwwroot\uploads\images" | Out-Null
New-Item -ItemType Directory -Force -Path "wwwroot\uploads\audio" | Out-Null
Write-Host "✅ Upload directories created" -ForegroundColor Green

Set-Location ..\..

# Summary
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "✅ Setup Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Press F5 in VS Code to start debugging" -ForegroundColor White
Write-Host "2. Open browser: http://localhost:5000" -ForegroundColor White
Write-Host "3. Admin login:" -ForegroundColor White
Write-Host "   Email: admin@localhost" -ForegroundColor Cyan
Write-Host "   Password: Admin123!@#" -ForegroundColor Cyan
Write-Host ""
Write-Host "Optional services:" -ForegroundColor Yellow
Write-Host "- MailHog (email testing): docker-compose -f docker-compose.dev.yml up -d mailhog" -ForegroundColor White
Write-Host "  Web UI: http://localhost:8025" -ForegroundColor Cyan
Write-Host "- pgAdmin (database UI): docker-compose -f docker-compose.dev.yml up -d pgadmin" -ForegroundColor White
Write-Host "  Web UI: http://localhost:5050" -ForegroundColor Cyan
Write-Host "  Login: admin@localhost / admin" -ForegroundColor Cyan
Write-Host ""
Write-Host "To stop services: docker-compose -f docker-compose.dev.yml down" -ForegroundColor Yellow
Write-Host ""
