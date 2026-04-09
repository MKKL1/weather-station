from __future__ import annotations

import logging
from typing import Any, Optional

import httpx

from hw_cli.core.api.token_manager import TokenManager
from hw_cli.core.api.weather_api_gateway import WeatherApiGateway
from hw_cli.core.models import DeviceConfig, TelemetryData

logger = logging.getLogger(__name__)


class WeatherIoTClient:
    def __init__(self, device: DeviceConfig):
        self.device = device
        self._client: Optional[httpx.AsyncClient] = None
        self._api_gateway: Optional[WeatherApiGateway] = None
        self._token_manager: Optional[TokenManager] = None

    async def __aenter__(self) -> WeatherIoTClient:
        await self.connect()
        return self

    async def __aexit__(self, exc_type: Any, exc_val: Any, exc_tb: Any) -> None:
        await self.close()

    async def connect(self) -> None:
        if self._client is None:
            self._client = httpx.AsyncClient(timeout=30.0)
            self._api_gateway = WeatherApiGateway(
                self.device.api_base_url, self._client
            )
            self._init_token_manager_if_registered()

    async def close(self) -> None:
        if self._client:
            await self._client.aclose()
            self._client = None
            self._api_gateway = None
            self._token_manager = None

    def update_device(self, device: DeviceConfig) -> None:
        self.device = device
        self._init_token_manager_if_registered()

    def _init_token_manager_if_registered(self) -> None:
        if self.device.hmac_secret and self._api_gateway:
            self._token_manager = TokenManager(
                device_id=self.device.device_id,
                provisioning_token=self.device.provisioning_token,
                hmac_secret=self.device.hmac_secret,
                api_gateway=self._api_gateway,
            )
            logger.debug(
                f"Token manager initialized for device {self.device.device_id}"
            )

    async def register(self) -> str:
        if not self._api_gateway:
            raise RuntimeError("Client not connected")

        logger.info(f"Registering device {self.device.device_id}")
        data = await self._api_gateway.register_device(
            self.device.provisioning_token, self.device.device_id
        )
        return data["hmac_secret"]

    async def get_claim_code(self) -> str:
        if not self._token_manager or not self._api_gateway:
            raise RuntimeError("Device not registered or client not connected")

        token = await self._token_manager.get_token()
        data = await self._api_gateway.request_claim_code(self.device.device_id, token)
        return data["claim_code"]

    async def send_telemetry(self, telemetry: TelemetryData) -> None:
        if not self._token_manager or not self._api_gateway:
            raise RuntimeError("Device not registered. Call register() first.")

        token = await self._token_manager.get_token()
        logger.info(f"Sending telemetry for ts={telemetry.timestamp}")
        await self._api_gateway.send_telemetry(token, telemetry)
        logger.info("Telemetry sent successfully")

    def invalidate_token(self) -> None:
        if self._token_manager:
            self._token_manager.invalidate()
