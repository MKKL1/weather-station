# syntax=docker/dockerfile:1.10
FROM golang:1.26-alpine AS builder

ARG TARGETOS=linux
ARG TARGETARCH=amd64

WORKDIR /app

COPY go.mod go.sum ./
RUN go mod download && go mod verify

COPY . .

RUN CGO_ENABLED=0 GOOS=${TARGETOS} GOARCH=${TARGETARCH} \
    go build \
      -trimpath \
      -ldflags="-s -w" \
      -o /app/provisioning-service \
      .

FROM gcr.io/distroless/static-debian13:nonroot AS runtime

WORKDIR /app

COPY --from=builder /app/provisioning-service .

ENV PORT=8080
EXPOSE 8080

ENTRYPOINT ["/app/provisioning-service"]
