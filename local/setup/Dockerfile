FROM python:3.11-alpine

RUN apk add --no-cache openssl bash \
 && pip install --no-cache-dir cryptography mnemonic PyJWT \
 && addgroup -g 1000 appuser && \
    adduser -u 1000 -G appuser -s /bin/sh -D appuser

WORKDIR /scripts
COPY --chown=appuser:appuser entrypoint.sh generate_devices.py ./
RUN chmod +x entrypoint.sh

USER appuser

ENTRYPOINT ["./entrypoint.sh"]