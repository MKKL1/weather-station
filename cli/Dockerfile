FROM python:3.11-slim AS builder

ENV PYTHONUNBUFFERED=1 \
    PYTHONDONTWRITEBYTECODE=1 \
    PIP_NO_CACHE_DIR=1 \
    PIP_DISABLE_PIP_VERSION_CHECK=1 \
    POETRY_VERSION=1.8.3 \
    POETRY_HOME="/opt/poetry" \
    POETRY_NO_INTERACTION=1 \
    POETRY_VIRTUALENVS_IN_PROJECT=true

RUN apt-get update && apt-get install -y --no-install-recommends \
    build-essential \
    curl \
    && rm -rf /var/lib/apt/lists/*

RUN curl -sSL https://install.python-poetry.org | python3 -
ENV PATH="$POETRY_HOME/bin:$PATH"

WORKDIR /app

COPY pyproject.toml poetry.lock* ./

RUN poetry install --no-root --only main --no-directory || \
    poetry install --no-root --without dev

COPY hw_cli/ ./hw_cli/
COPY README.md ./

RUN poetry install --only-root

FROM python:3.11-slim AS runtime

ENV PYTHONUNBUFFERED=1 \
    PYTHONDONTWRITEBYTECODE=1 \
    PATH="/app/.venv/bin:$PATH"

ENV HW_CLI_DATA_DIR=/data

RUN groupadd -r hwcli && useradd -r -g hwcli -u 1000 hwcli

RUN mkdir -p /data && chown hwcli:hwcli /data

WORKDIR /app

COPY --from=builder --chown=hwcli:hwcli /app/.venv /app/.venv

COPY --from=builder --chown=hwcli:hwcli /app/hw_cli /app/hw_cli

USER hwcli

VOLUME ["/data"]

HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD hw --version || exit 1

ENTRYPOINT ["hw"]

CMD ["--help"]
