FROM golang:1.26-alpine AS builder

WORKDIR /app

COPY go.mod go.sum ./
RUN go mod download

COPY . .

RUN CGO_ENABLED=0 go build -o /app/provisioning-service .

FROM gcr.io/distroless/static-debian13

WORKDIR /app

COPY --from=builder /app/provisioning-service .

ENV PORT=8080
EXPOSE 8080

ENTRYPOINT ["/app/provisioning-service"]
