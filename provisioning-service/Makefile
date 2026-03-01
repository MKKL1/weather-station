BINARY_NAME := provisioning-service

.PHONY: build clean

build:
	CGO_ENABLED=0 GOOS=linux GOARCH=amd64 \
	go build -ldflags="-s -w" -o $(BINARY_NAME) main.go

clean:
	rm -f $(BINARY_NAME)
