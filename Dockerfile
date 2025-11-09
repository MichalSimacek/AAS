# Aristocratic Artwork Sale - Production Dockerfile
# Multi-stage build for optimal image size and security

# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies (cached layer)
COPY ["src/AAS.Web/AAS.Web.csproj", "src/AAS.Web/"]
RUN dotnet restore "src/AAS.Web/AAS.Web.csproj"

# Copy everything else and build
COPY src/AAS.Web/. src/AAS.Web/
WORKDIR "/src/src/AAS.Web"
RUN dotnet build "AAS.Web.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "AAS.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y --no-install-recommends curl && \
    rm -rf /var/lib/apt/lists/*

# Create non-root user for security
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Create directories for uploads with correct permissions
RUN mkdir -p /app/wwwroot/uploads/images /app/wwwroot/uploads/audio

# Copy published app
COPY --from=publish /app/publish .

# Set ownership to non-root user
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Expose port (note: running as non-root, so using port > 1024)
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:8080/ || exit 1

# Set environment
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

# Start application
ENTRYPOINT ["dotnet", "AAS.Web.dll"]
