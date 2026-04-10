# syntax=docker/dockerfile:1.10
FROM alpine:3.23

RUN apk update && apk upgrade --no-cache && \
    apk add --no-cache python3 py3-pip bash openssl && \
    python3 -m venv /opt/venv && \
    /opt/venv/bin/pip install --no-cache-dir mnemonic==0.21 PyJWT==2.12.1 cryptography && \
    addgroup -g 1000 appuser && \
    adduser -u 1000 -G appuser -s /bin/sh -D appuser

WORKDIR /scripts

COPY --chown=appuser:appuser entrypoint.sh generate_devices.py ./
RUN sed -i 's/\r$//' entrypoint.sh && chmod +x entrypoint.sh

USER appuser

ENTRYPOINT ["./entrypoint.sh"]
