# syntax=docker/dockerfile:1.10
FROM python:3.13-slim AS builder

ENV PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1 \
    POETRY_VIRTUALENVS_IN_PROJECT=true \
    POETRY_NO_INTERACTION=1

WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends \
    gcc \
    libffi-dev \
    libc6-dev \
    python3-dev \
    && pip install --no-cache-dir poetry==2.3.3 \
    && apt-get purge -y --auto-remove gcc libffi-dev libc6-dev python3-dev \
    && rm -rf /var/lib/apt/lists/*

COPY pyproject.toml poetry.lock ./
RUN poetry install --no-root --only main

COPY hw_cli/ ./hw_cli/
COPY README.md ./
RUN poetry install --only-root


FROM gcr.io/distroless/python3-debian13:nonroot

COPY --from=builder --chown=nonroot:nonroot /app/.venv/lib /app/.venv/lib
COPY --from=builder --chown=nonroot:nonroot /app/hw_cli /app/hw_cli

WORKDIR /app

ENV PYTHONPATH="/app/.venv/lib/python3.13/site-packages" \
    PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1 \
    HW_CLI_DATA_DIR="/data"

ENTRYPOINT ["python3", "-m", "hw_cli.__main__"]
CMD ["--help"]
