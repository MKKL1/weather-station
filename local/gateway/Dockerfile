# syntax=docker/dockerfile:1.10
FROM golang:1.26-alpine AS build

ARG TARGETOS
ARG TARGETARCH

WORKDIR /src

COPY go.mod go.sum ./
RUN go mod download && go mod verify

COPY main.go ./

RUN CGO_ENABLED=0 GOOS=${TARGETOS} GOARCH=${TARGETARCH} \
    go build \
      -trimpath \
      -ldflags="-s -w" \
      -o /gateway \
      .

FROM scratch AS runtime

COPY --from=build /gateway /gateway
COPY --from=build /etc/ssl/certs/ca-certificates.crt /etc/ssl/certs/

EXPOSE 8000

ENTRYPOINT ["/gateway"]