import logging
from typing import Any

import httpx

from hw_cli.core.constants import (
    API_PATH_REGISTER,
    API_PATH_TELEMETRY,
    API_PATH_TOKEN
)

logger = logging.getLogger(__name__)

class WeatherApiGateway:

    def __init__(self, base_url: str, client: httpx.AsyncClient):
        self.base_url = base_url.rstrip("/")
        self.client = client

    async def register_device(self, provisioning_token: str) -> dict[str, Any]:
        """POST /provisioning/register"""
        url = f"{self.base_url}{API_PATH_REGISTER}"
        headers = {"Authorization": f"Bearer {provisioning_token}"}

        resp = await self.client.post(url, headers=headers)
        resp.raise_for_status()
        return resp.json()

    async def request_token(
            self,
            provisioning_token: str,
            timestamp: int,
            signature: str
    ) -> dict[str, Any]:
        """POST /device/auth/token"""
        url = f"{self.base_url}{API_PATH_TOKEN}"
        headers = {"Authorization": f"Bearer {provisioning_token}"}
        payload = {"timestamp": timestamp, "signature": signature}

        resp = await self.client.post(url, headers=headers, json=payload)
        resp.raise_for_status()
        return resp.json()

    async def send_telemetry(self, access_token: str, payload: dict[str, Any]) -> None:
        """POST /device/telemetry"""
        url = f"{self.base_url}{API_PATH_TELEMETRY}"
        headers = {"Authorization": f"Bearer {access_token}"}

        resp = await self.client.post(url, headers=headers, json=payload)
        resp.raise_for_status()