#!/bin/sh
set -e

# Copy static files to shared volume if they don't exist
# This allows Nginx to serve them directly
if [ -d "/app/wwwroot" ] && [ -d "/shared-static" ]; then
    echo "Copying static files to shared volume..."
    cp -r /app/wwwroot/* /shared-static/ 2>/dev/null || true
    echo "Static files copied."
fi

# Start the ASP.NET Core application
exec dotnet AAS.Web.dll
