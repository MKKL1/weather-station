FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

COPY server/WeatherStation.sln ./server/
COPY server/src/WeatherStation.API/WeatherStation.API.csproj ./server/src/WeatherStation.API/
COPY server/src/WeatherStation.Core/WeatherStation.Core.csproj ./server/src/WeatherStation.Core/
COPY server/src/WeatherStation.Infrastructure/WeatherStation.Infrastructure.csproj ./server/src/WeatherStation.Infrastructure/
COPY server/tests/WeatherStation.API.Tests/WeatherStation.API.Tests.csproj ./server/tests/WeatherStation.API.Tests/
COPY server/tests/WeatherStation.Core.Tests/WeatherStation.Core.Tests.csproj ./server/tests/WeatherStation.Core.Tests/
COPY shared/WeatherStation.Shared.csproj ./shared/

RUN dotnet restore server/WeatherStation.sln

COPY server/src/ ./server/src/
COPY server/tests/ ./server/tests/
COPY shared/ ./shared/

RUN dotnet publish server/src/WeatherStation.API/WeatherStation.API.csproj \
    --configuration Release \
    --output /app/publish \
    --runtime linux-x64 \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:OpenApiGenerateDocuments=false

FROM gcr.io/distroless/cc-debian13:nonroot AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:80 \
    DOTNET_EnableDiagnostics=0 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1

EXPOSE 80

ENTRYPOINT ["./WeatherStation.API"]
