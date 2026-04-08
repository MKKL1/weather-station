FROM mcr.microsoft.com/dotnet/sdk:8.0 AS installer-env

WORKDIR /src
COPY shared/ ./shared/
COPY telemetry-worker/Worker/ ./telemetry-worker/Worker/

RUN dotnet publish telemetry-worker/Worker/Worker.csproj \
    --output /home/site/wwwroot

FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0

ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true \
    DOTNET_EnableDiagnostics=0

COPY --from=installer-env /home/site/wwwroot /home/site/wwwroot

EXPOSE 80