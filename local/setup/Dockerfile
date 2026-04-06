FROM python:3.11-alpine

RUN apk add --no-cache openssl bash \
 && pip install --no-cache-dir cryptography mnemonic PyJWT

WORKDIR /scripts
COPY entrypoint.sh generate_devices.py ./
RUN chmod +x entrypoint.sh

ENTRYPOINT ["./entrypoint.sh"]