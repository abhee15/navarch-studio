# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["backend/AIAgentService/AIAgentService.csproj", "backend/AIAgentService/"]
COPY ["backend/Shared/Shared.csproj", "backend/Shared/"]
COPY ["packages/unit-conversion/dotnet/NavArch.UnitConversion/NavArch.UnitConversion.csproj", "packages/unit-conversion/dotnet/NavArch.UnitConversion/"]
COPY ["packages/unit-conversion/config/", "packages/unit-conversion/config/"]

# Restore dependencies
RUN dotnet restore "backend/AIAgentService/AIAgentService.csproj"

# Copy everything else and build
COPY backend/Shared/ backend/Shared/
COPY backend/AIAgentService/ backend/AIAgentService/
COPY packages/unit-conversion/ packages/unit-conversion/
WORKDIR "/src/backend/AIAgentService"
RUN dotnet build "AIAgentService.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "AIAgentService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 5005

# Create logs directory
RUN mkdir -p /app/logs

# Copy published files
COPY --from=publish /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
  CMD curl --fail http://localhost:5005/health || exit 1

ENTRYPOINT ["dotnet", "AIAgentService.dll"]
