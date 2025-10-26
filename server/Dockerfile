# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

COPY WeatherStation.sln ./
COPY src/WeatherStation.API/*.csproj ./src/WeatherStation.API/
COPY src/WeatherStation.Application/*.csproj ./src/WeatherStation.Application/
COPY src/WeatherStation.Domain/*.csproj ./src/WeatherStation.Domain/
COPY src/WeatherStation.Infrastructure/*.csproj ./src/WeatherStation.Infrastructure/
COPY tests/WeatherStation.API.Tests/*.csproj ./tests/WeatherStation.API.Tests/
COPY tests/WeatherStation.Application.Tests/*.csproj ./tests/WeatherStation.Application.Tests/
COPY tests/WeatherStation.Domain.Tests/*.csproj ./tests/WeatherStation.Domain.Tests/
COPY tests/WeatherStation.Infrastructure.Tests/*.csproj ./tests/WeatherStation.Infrastructure.Tests/

COPY Directory.Build.props* ./
COPY Directory.Packages.props* ./
COPY nuget.config* ./

RUN dotnet restore WeatherStation.sln

COPY . .
RUN dotnet build WeatherStation.sln --configuration Release

# Publish stage
FROM build AS publish
RUN dotnet publish src/WeatherStation.API/WeatherStation.API.csproj \
    --configuration Release \
    --output /app/publish \
    /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS final
WORKDIR /app

RUN addgroup -g 1000 appuser && \
    adduser -u 1000 -G appuser -s /bin/sh -D appuser && \
    chown -R appuser:appuser /app

COPY --from=publish --chown=appuser:appuser /app/publish .

USER appuser

EXPOSE 80

HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:80 \
    DOTNET_EnableDiagnostics=0

ENTRYPOINT ["dotnet", "WeatherStation.API.dll"]