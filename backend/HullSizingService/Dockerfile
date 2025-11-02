# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and projects
COPY ["backend/navarch-studio.sln", "backend/"]
COPY ["backend/HullSizingService/HullSizingService.csproj", "backend/HullSizingService/"]
COPY ["backend/Shared/Shared.csproj", "backend/Shared/"]
COPY ["packages/unit-conversion/dotnet/NavArch.UnitConversion/NavArch.UnitConversion.csproj", "packages/unit-conversion/dotnet/NavArch.UnitConversion/"]

# Restore dependencies
RUN dotnet restore "backend/HullSizingService/HullSizingService.csproj"

# Copy source code
COPY backend/HullSizingService/ backend/HullSizingService/
COPY backend/Shared/ backend/Shared/
COPY packages/unit-conversion/dotnet/NavArch.UnitConversion/ packages/unit-conversion/dotnet/NavArch.UnitConversion/
COPY packages/unit-conversion/config/ /app/config/

# Build
WORKDIR /src/backend/HullSizingService
RUN dotnet build "HullSizingService.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "HullSizingService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published app
COPY --from=publish /app/publish .
COPY --from=build /app/config/ /app/config/

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

EXPOSE 8080

ENTRYPOINT ["dotnet", "HullSizingService.dll"]

