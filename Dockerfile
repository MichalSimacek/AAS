FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/AAS.Web/AAS.Web.csproj", "src/AAS.Web/"]
RUN dotnet restore "src/AAS.Web/AAS.Web.csproj"
COPY . .
WORKDIR "/src/src/AAS.Web"
RUN dotnet build "AAS.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AAS.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AAS.Web.dll"]
