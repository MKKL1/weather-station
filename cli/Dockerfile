# syntax=docker/dockerfile:1.10
FROM python:3.13-slim AS builder

COPY --from=ghcr.io/astral-sh/uv:latest /uv /uvx /bin/

ENV PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1 \
    UV_LINK_MODE=copy \
    UV_COMPILE_BYTECODE=1

WORKDIR /app

RUN --mount=type=cache,target=/root/.cache/uv \
    --mount=type=bind,source=uv.lock,target=uv.lock \
    --mount=type=bind,source=pyproject.toml,target=pyproject.toml \
    uv sync --frozen --no-install-project --no-dev

COPY hw_cli/ ./hw_cli/
COPY README.md ./
RUN mkdir -p /data_dir


FROM gcr.io/distroless/python3-debian13:nonroot

COPY --from=builder --chown=nonroot:nonroot /app/.venv/lib /app/.venv/lib
COPY --from=builder --chown=nonroot:nonroot /app/hw_cli /app/hw_cli
COPY --from=builder --chown=nonroot:nonroot /data_dir /data

WORKDIR /app

ENV PYTHONPATH="/app/.venv/lib/python3.13/site-packages" \
    PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1 \
    HW_CLI_DATA_DIR="/data"

ENTRYPOINT ["python3", "-m", "hw_cli.__main__"]
CMD ["--help"]
