FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

COPY telemetry-worker/TelemetryWorker.sln ./telemetry-worker/
COPY telemetry-worker/Worker/Worker.csproj ./telemetry-worker/Worker/
COPY telemetry-worker/Tests/Tests.csproj ./telemetry-worker/Tests/
COPY shared/WeatherStation.Shared.csproj ./shared/

RUN dotnet restore telemetry-worker/TelemetryWorker.sln

COPY telemetry-worker/ ./telemetry-worker/
COPY shared/ ./shared/

RUN dotnet publish telemetry-worker/Worker/Worker.csproj \
    --configuration Release \
    --output /app/publish \
    /p:UseAppHost=false

FROM node:20-slim AS final
WORKDIR /app

RUN apt-get update && apt-get install -y \
    wget \
    apt-transport-https \
    software-properties-common \
    && wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh \
    && bash dotnet-install.sh --channel 8.0 --runtime aspnetcore --install-dir /usr/share/dotnet \
    && ln -s /usr/share/dotnet/dotnet /usr/local/bin/dotnet \
    && rm dotnet-install.sh \
    && rm -rf /var/lib/apt/lists/*

RUN npm install -g azure-functions-core-tools@4 --unsafe-perm true

COPY --from=build /app/publish .

ENV FUNCTIONS_WORKER_RUNTIME=dotnet-isolated \
    DOTNET_EnableDiagnostics=0 \
    AzureFunctionsJobHost__logging__console__isEnabled=true

EXPOSE 7071

CMD ["func", "start", "--port", "7071"]
