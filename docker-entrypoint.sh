#!/bin/sh
set -e

echo "==================================="
echo "AAS Web Application Starting..."
echo "==================================="

# Copy static files to shared volume for Nginx to serve
if [ -d "/app/wwwroot" ] && [ -d "/shared-static" ]; then
    echo "📁 Copying static files to shared volume..."
    
    # Copy all static files
    if cp -r /app/wwwroot/* /shared-static/ 2>/dev/null; then
        echo "✅ Static files copied successfully"
        
        # List copied files for verification
        echo "📋 Static files in shared volume:"
        ls -la /shared-static/ || true
    else
        echo "⚠️  Warning: Could not copy some static files, but continuing..."
    fi
else
    echo "⚠️  Warning: Static file directories not found"
    echo "   /app/wwwroot exists: $([ -d '/app/wwwroot' ] && echo 'yes' || echo 'no')"
    echo "   /shared-static exists: $([ -d '/shared-static' ] && echo 'yes' || echo 'no')"
fi

echo "==================================="
echo "🚀 Starting ASP.NET Core application"
echo "==================================="

# Start the ASP.NET Core application
exec dotnet AAS.Web.dll
