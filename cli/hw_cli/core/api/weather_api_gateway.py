import logging
from typing import Any, Dict

import httpx

from hw_cli.core.constants import API_PATH_TELEMETRY
from hw_cli.core.models import TelemetryData

logger = logging.getLogger(__name__)


class WeatherApiGateway:
    def __init__(self, base_url: str, client: httpx.AsyncClient):
        self.base_url = base_url.rstrip("/")
        self.client = client

    def _format_telemetry_payload(self, telemetry: TelemetryData) -> Dict[str, Any]:
        dat: Dict[str, Any] = {}
        r = telemetry.reading
        if r.temperature is not None:
            dat["tmp"] = r.temperature
        if r.pressure is not None:
            dat["prs"] = r.pressure
        if r.humidity is not None:
            dat["hum"] = r.humidity
        if r.precipitation_mm is not None:
            dat["mmpt"] = r.precipitation_mm
        if r.rain and r.rain.data:
            dat["rain"] = {
                "dat": r.rain.data,
                "sec": r.rain.bucket_seconds,
                "sts": r.rain.start_timestamp,
                "n": r.rain.num_buckets,
            }
        return {"ts": telemetry.timestamp, "dat": dat}

    async def register_device(
        self, provisioning_token: str, device_id: str
    ) -> dict[str, Any]:
        url = f"{self.base_url}/provisioning/{device_id}/register"
        headers = {"Authorization": f"Bearer {provisioning_token}"}
        resp = await self.client.post(url, headers=headers)
        resp.raise_for_status()
        return resp.json().get("data", {})

    async def request_token(
        self, provisioning_token: str, device_id: str, timestamp: int, signature: str
    ) -> dict[str, Any]:
        url = f"{self.base_url}/provisioning/{device_id}/token"
        headers = {"Authorization": f"Bearer {provisioning_token}"}
        payload = {"timestamp": timestamp, "signature": signature}

        resp = await self.client.post(url, headers=headers, json=payload)
        resp.raise_for_status()
        return resp.json().get("data", {})

    async def request_claim_code(
        self, device_id: str, access_token: str
    ) -> dict[str, Any]:
        url = f"{self.base_url}/provisioning/{device_id}/claim-code"
        headers = {"Authorization": f"Bearer {access_token}"}
        resp = await self.client.post(url, headers=headers)
        resp.raise_for_status()
        return resp.json().get("data", {})

    async def send_telemetry(self, access_token: str, telemetry: TelemetryData) -> None:
        url = f"{self.base_url}{API_PATH_TELEMETRY}"
        headers = {"Authorization": f"Bearer {access_token}"}
        payload = self._format_telemetry_payload(telemetry)

        resp = await self.client.post(url, headers=headers, json=payload)
        resp.raise_for_status()
