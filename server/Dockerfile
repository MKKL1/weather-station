# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

COPY server/WeatherStation.sln ./server/
COPY server/src/WeatherStation.API/*.csproj ./server/src/WeatherStation.API/
COPY server/src/WeatherStation.Core/*.csproj ./server/src/WeatherStation.Core/
COPY server/src/WeatherStation.Core.Tests/*.csproj ./server/src/WeatherStation.Core.Tests/
COPY server/src/WeatherStation.Infrastructure/*.csproj ./server/src/WeatherStation.Infrastructure/
COPY server/tests/WeatherStation.API.Tests/*.csproj ./server/tests/WeatherStation.API.Tests/
COPY server/tests/WeatherStation.Core.Tests/*.csproj ./server/tests/WeatherStation.Core.Tests/
COPY shared/WeatherStation.Shared.csproj ./shared/

COPY server/Directory.Build.props* ./server/
COPY server/Directory.Packages.props* ./server/
COPY server/nuget.config* ./server/

RUN dotnet restore server/WeatherStation.sln

COPY server/ ./server/
COPY shared/ ./shared/

RUN mkdir -p docs/public/specs

RUN dotnet build server/WeatherStation.sln --configuration Release -p:OpenApiGenerateDocuments=false

FROM build AS publish
RUN dotnet publish server/src/WeatherStation.API/WeatherStation.API.csproj \
    --configuration Release \
    --output /app/publish \
    -p:OpenApiGenerateDocuments=false \
    /p:UseAppHost=false
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS final
WORKDIR /app

RUN addgroup -g 1000 appuser && \
    adduser -u 1000 -G appuser -s /bin/sh -D appuser && \
    chown -R appuser:appuser /app

RUN apk add --no-cache icu-libs icu-data-full

COPY --from=publish --chown=appuser:appuser /app/publish .

USER appuser

EXPOSE 80

HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:80 \
    DOTNET_EnableDiagnostics=0 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=0

ENTRYPOINT ["dotnet", "WeatherStation.API.dll"]