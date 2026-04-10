# syntax=docker/dockerfile:1.10
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY shared/ ./shared/
COPY telemetry-worker/Worker/ ./telemetry-worker/Worker/
RUN dotnet publish telemetry-worker/Worker/Worker.csproj \
      --configuration Release \
      --output /home/site/wwwroot

# This base image doesn't have arm variant.
# Microsoft also doesn't publish dockerfiles for it so it's hard to modify.
FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0

ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true \
    FUNCTIONS_WORKER_RUNTIME=dotnet-isolated

COPY --from=build /home/site/wwwroot /home/site/wwwroot
