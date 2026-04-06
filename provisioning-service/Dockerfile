FROM golang:1.26-alpine AS builder

WORKDIR /app

COPY go.mod go.sum ./
RUN go mod download

COPY . .

RUN CGO_ENABLED=0 go build -o /app/provisioning-service .

FROM alpine:latest

WORKDIR /app

RUN apk add --no-cache ca-certificates bash coreutils

COPY --from=builder /app/provisioning-service .

RUN echo '#!/bin/bash' > /app/entrypoint.sh && \
    echo 'if [ -f /keys/private.pem ]; then export ACCESS_TOKEN_PRIVATE_KEY_B64=$(cat /keys/private.pem | base64 | tr -d "\n"); fi' >> /app/entrypoint.sh && \
    echo 'exec "$@"' >> /app/entrypoint.sh && \
    chmod +x /app/entrypoint.sh

ENV PORT=8080
EXPOSE 8080

ENTRYPOINT ["/app/entrypoint.sh"]
CMD ["/app/provisioning-service"]
