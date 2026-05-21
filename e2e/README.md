# Weather Station E2E Tests

## Running Tests

### Running with Docker Compose (Local Stack)
To run tests against the default local Docker Compose environment:
```bash
uv run pytest
```

### Running with custom endpoints
1. Set env variables
```
API_SERVER_URL="http://your-custom-api-server:8002",
GATEWAY_URL="http://your-custom-gateway:8000",
ADMIN_API_KEY="your-admin-api-key",
NO_PROXY:="*",
PYTHONPATH="."

2. Run tests
```bash
uv run pytest
```