# syntax=docker/dockerfile:1.10
#
# Normally mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0 image would be used
# but microsoft doesn't publish arm images, so it has to be done manually
ARG HOST_VERSION=4.1044.400
ARG EXTENSION_BUNDLE_VERSION_V4=4.28.0

FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim AS host-builder
ARG HOST_VERSION
ARG EXTENSION_BUNDLE_VERSION_V4
ARG TARGETARCH

RUN git clone --branch v${HOST_VERSION} \
      https://github.com/Azure/azure-functions-host /src/azure-functions-host

RUN case "${TARGETARCH}" in \
      amd64) DOTNET_RID=linux-x64    ;; \
      arm64) DOTNET_RID=linux-arm64  ;; \
      *)     echo "Unsupported arch: ${TARGETARCH}" && exit 1 ;; \
    esac && \
    cd /src/azure-functions-host && \
    dotnet restore --verbosity detailed \
      src/WebJobs.Script.WebHost/WebJobs.Script.WebHost.csproj && \
    dotnet publish -v q /p:CI=true \
      src/WebJobs.Script.WebHost/WebJobs.Script.WebHost.csproj \
      -c Release \
      --output /azure-functions-host \
      --self-contained \
      --runtime ${DOTNET_RID}

RUN apt-get update && \
    apt-get install -y gnupg wget unzip && \
    case "${TARGETARCH}" in \
      amd64) BUNDLE_ARCH=linux-x64   ;; \
      arm64) BUNDLE_ARCH=linux-arm64 ;; \
    esac && \
    BUNDLE_ZIP=Microsoft.Azure.Functions.ExtensionBundle.${EXTENSION_BUNDLE_VERSION_V4}_${BUNDLE_ARCH}.zip && \
    wget https://cdn.functions.azure.com/public/ExtensionBundles/Microsoft.Azure.Functions.ExtensionBundle/${EXTENSION_BUNDLE_VERSION_V4}/${BUNDLE_ZIP} && \
    mkdir -p /FuncExtensionBundles/Microsoft.Azure.Functions.ExtensionBundle/${EXTENSION_BUNDLE_VERSION_V4} && \
    unzip /${BUNDLE_ZIP} \
      -d /FuncExtensionBundles/Microsoft.Azure.Functions.ExtensionBundle/${EXTENSION_BUNDLE_VERSION_V4} && \
    rm -f /${BUNDLE_ZIP} && \
    find /FuncExtensionBundles/ -type f -exec chmod 644 {} \;

FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim AS worker-builder
ARG TARGETARCH
WORKDIR /src
COPY shared/                  ./shared/
COPY telemetry-worker/Worker/ ./telemetry-worker/Worker/
RUN case "${TARGETARCH}" in \
      amd64) DOTNET_RID=linux-x64   ;; \
      arm64) DOTNET_RID=linux-arm64 ;; \
      *)     echo "Unsupported arch: ${TARGETARCH}" && exit 1 ;; \
    esac && \
    dotnet publish telemetry-worker/Worker/Worker.csproj \
      --configuration Release \
      --runtime ${DOTNET_RID} \
      --self-contained false \
      --output /home/site/wwwroot

FROM mcr.microsoft.com/dotnet/runtime-deps:8.0-bookworm-slim
ARG HOST_VERSION

COPY --from=host-builder   /azure-functions-host  /azure-functions-host
COPY --from=host-builder   /FuncExtensionBundles  /FuncExtensionBundles
COPY --from=worker-builder /home/site/wwwroot      /home/site/wwwroot

ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true \
    HOME=/home \
    DOTNET_USE_POLLING_FILE_WATCHER=true \
    DOTNET_EnableDiagnostics=0 \
    HOST_VERSION=${HOST_VERSION} \
    PORT=80 \
    ASPNETCORE_URLS=http://+:80 \
    FUNCTIONS_WORKER_RUNTIME=dotnet-isolated

EXPOSE 2222 80
ENV SSH_PORT=2222

RUN apt-get update && \
    apt-get install -y --no-install-recommends openssh-server dialog && \
    echo "root:Docker!" | chpasswd

RUN echo "Port $SSH_PORT"             >  /etc/ssh/sshd_config && \
    echo "ListenAddress 0.0.0.0"      >> /etc/ssh/sshd_config && \
    echo "LoginGraceTime 180"         >> /etc/ssh/sshd_config && \
    echo "X11Forwarding yes"          >> /etc/ssh/sshd_config && \
    echo "Ciphers aes128-cbc,3des-cbc,aes256-cbc" >> /etc/ssh/sshd_config && \
    echo "MACs hmac-sha1,hmac-sha1-96"            >> /etc/ssh/sshd_config && \
    echo "StrictModes yes"            >> /etc/ssh/sshd_config && \
    echo "SyslogFacility DAEMON"      >> /etc/ssh/sshd_config && \
    echo "PasswordAuthentication yes" >> /etc/ssh/sshd_config && \
    echo "PermitEmptyPasswords no"    >> /etc/ssh/sshd_config && \
    echo "PermitRootLogin yes"        >> /etc/ssh/sshd_config

RUN printf '#!/bin/bash\n\
eval $(printenv | sed -n "s/^\([^=]\+\)=\(.*\)$/export \1=\2/p" | sed "s/\"/\\\\\"/g" | sed "/=/s//=\"/" | sed "s/$/\"/" >> /etc/profile)\n\
service ssh start\n\
/azure-functions-host/Microsoft.Azure.WebJobs.Script.WebHost\n' > /startup.sh && \
    chmod +x /startup.sh

CMD ["/startup.sh"]
