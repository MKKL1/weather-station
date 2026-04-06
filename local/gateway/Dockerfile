FROM golang:1.26.1-alpine AS build
WORKDIR /src

COPY go.mod go.sum ./
RUN go mod download

COPY main.go ./
RUN CGO_ENABLED=0 GOOS=linux go build -o /gateway .

FROM scratch
COPY --from=build /gateway /gateway
COPY --from=build /etc/ssl/certs/ca-certificates.crt /etc/ssl/certs/

EXPOSE 8000
ENTRYPOINT ["/gateway"]