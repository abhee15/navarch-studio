FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
# Copy unit conversion package and config
COPY ["packages/unit-conversion/dotnet/NavArch.UnitConversion/NavArch.UnitConversion.csproj", "packages/unit-conversion/dotnet/NavArch.UnitConversion/"]
COPY ["packages/unit-conversion/config/", "packages/unit-conversion/config/"]
# Copy backend projects
COPY ["backend/Shared/Shared.csproj", "backend/Shared/"]
COPY ["backend/IdentityService/IdentityService.csproj", "backend/IdentityService/"]
RUN dotnet restore "backend/IdentityService/IdentityService.csproj"
# Copy source files
COPY packages/unit-conversion/dotnet/NavArch.UnitConversion/ packages/unit-conversion/dotnet/NavArch.UnitConversion/
COPY backend/Shared/ backend/Shared/
COPY backend/IdentityService/ backend/IdentityService/
WORKDIR /src/backend/IdentityService
RUN dotnet build "IdentityService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "IdentityService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "IdentityService.dll"]
