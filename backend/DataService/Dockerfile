FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Shared/Shared.csproj", "Shared/"]
COPY ["DataService/DataService.csproj", "DataService/"]
RUN dotnet restore "DataService/DataService.csproj"
COPY Shared/ Shared/
COPY DataService/ DataService/
WORKDIR /src/DataService
RUN dotnet build "DataService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DataService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DataService.dll"]
