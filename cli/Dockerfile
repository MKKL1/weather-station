FROM python:3.11-slim AS builder

ENV PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1 \
    POETRY_VIRTUALENVS_IN_PROJECT=true \
    POETRY_NO_INTERACTION=1

WORKDIR /app

RUN pip install --no-cache-dir poetry==2.3.3

COPY pyproject.toml poetry.lock ./
RUN poetry install --no-root --only main

COPY hw_cli/ ./hw_cli/
COPY README.md ./

RUN poetry install --only-root

FROM python:3.11-slim AS runtime

ENV PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1 \
    PATH="/app/.venv/bin:$PATH" \
    HW_CLI_DATA_DIR="/data"

RUN groupadd -r hwcli && useradd -r -g hwcli -u 1000 hwcli && \
    mkdir -p /data && chown hwcli:hwcli /data

WORKDIR /app

COPY --from=builder --chown=hwcli:hwcli /app/.venv /app/.venv
COPY --from=builder --chown=hwcli:hwcli /app/hw_cli /app/hw_cli

USER hwcli

ENTRYPOINT ["hw"]
CMD ["--help"]
