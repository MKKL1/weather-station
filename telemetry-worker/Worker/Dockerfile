# syntax=docker/dockerfile:1.10
#
# Normally mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0 image would be used
# but microsoft doesn't publish arm images, so it has to be done manually
#
# Using azure-functions-core-tools-4, not ideal
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY shared/                  ./shared/
COPY telemetry-worker/Worker/ ./telemetry-worker/Worker/
RUN dotnet publish telemetry-worker/Worker/Worker.csproj \
      --configuration Release \
      --output /home/site/wwwroot

FROM mcr.microsoft.com/dotnet/aspnet:8.0
RUN apt-get update && \
    apt-get install -y wget && \
    wget -q https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb && \
    dpkg -i packages-microsoft-prod.deb && \
    apt-get update && \
    apt-get install -y azure-functions-core-tools-4 && \
    rm -rf /var/lib/apt/lists/*

COPY --from=build /home/site/wwwroot /home/site/wwwroot

ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    ASPNETCORE_URLS=http://+:80 \
    FUNCTIONS_WORKER_RUNTIME=dotnet-isolated

EXPOSE 80
CMD ["func", "start", "--port", "80"]
