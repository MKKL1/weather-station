# Multi-stage build for Heavy Weather CLI
# Stage 1: Builder
FROM python:3.11-slim AS builder

# Set environment variables
ENV PYTHONUNBUFFERED=1 \
    PYTHONDONTWRITEBYTECODE=1 \
    PIP_NO_CACHE_DIR=1 \
    PIP_DISABLE_PIP_VERSION_CHECK=1 \
    POETRY_VERSION=1.8.3 \
    POETRY_HOME="/opt/poetry" \
    POETRY_NO_INTERACTION=1 \
    POETRY_VIRTUALENVS_IN_PROJECT=true

# Install system dependencies
RUN apt-get update && apt-get install -y --no-install-recommends \
    build-essential \
    curl \
    && rm -rf /var/lib/apt/lists/*

# Install Poetry
RUN curl -sSL https://install.python-poetry.org | python3 -
ENV PATH="$POETRY_HOME/bin:$PATH"

# Set working directory
WORKDIR /app

# Copy dependency files
COPY pyproject.toml poetry.lock* ./

# Install dependencies (without dev dependencies)
# Use --no-directory to avoid package mode requirements
RUN poetry install --no-root --only main --no-directory || \
    poetry install --no-root --without dev

# Copy application code
COPY hw_cli/ ./hw_cli/
COPY README.md ./

# Install the application
RUN poetry install --only-root

# Stage 2: Runtime
FROM python:3.11-slim AS runtime

# Set environment variables
ENV PYTHONUNBUFFERED=1 \
    PYTHONDONTWRITEBYTECODE=1 \
    PATH="/app/.venv/bin:$PATH"

# Set default data directory for the CLI
ENV HW_CLI_DATA_DIR=/data

# Create non-root user
RUN groupadd -r hwcli && useradd -r -g hwcli -u 1000 hwcli

# Create data directory with proper permissions
RUN mkdir -p /data && chown hwcli:hwcli /data

# Set working directory
WORKDIR /app

# Copy virtual environment from builder
COPY --from=builder --chown=hwcli:hwcli /app/.venv /app/.venv

# Copy application from builder
COPY --from=builder --chown=hwcli:hwcli /app/hw_cli /app/hw_cli

# Switch to non-root user
USER hwcli

# Define volume for persistent data
VOLUME ["/data"]

# Health check (adjust if your CLI has a specific health check command)
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD hw --version || exit 1

# Default entrypoint
ENTRYPOINT ["hw"]

# Default command (shows help)
CMD ["--help"]