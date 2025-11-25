import logging
from typing import Optional

import httpx

from hw_cli.core.api.device_authenticator import DeviceAuthenticator
from hw_cli.core.api.token_manager import TokenManager
from hw_cli.core.api.weather_api_gateway import WeatherApiGateway
from hw_cli.core.models import DeviceConfig, TelemetryData

logger = logging.getLogger(__name__)

class WeatherIoTClient:
    """Main client for Weather IoT API operations."""

    def __init__(self, device: DeviceConfig, timeout: float = 30.0):
        self.device = device
        self.timeout = timeout
        self._client: Optional[httpx.AsyncClient] = None
        self._api_gateway: Optional[WeatherApiGateway] = None
        self._token_manager: Optional[TokenManager] = None

    async def __aenter__(self):
        """Initialize HTTP client and API gateway only."""
        self._client = httpx.AsyncClient(timeout=self.timeout)
        self._api_gateway = WeatherApiGateway(self.device.api_base_url, self._client)

        self._init_token_manager_if_registered()

        return self

    async def __aexit__(self, *args):
        if self._client:
            await self._client.aclose()
            self._client = None

    def _init_token_manager_if_registered(self) -> None:
        """Initialize token manager if device has HMAC secret."""
        if self.device.hmac_secret and self._api_gateway:
            authenticator = DeviceAuthenticator(
                self.device.device_id,
                self.device.hmac_secret
            )
            self._token_manager = TokenManager(
                self.device.device_id,
                self.device.provisioning_token,
                authenticator,
                self._api_gateway
            )
            logger.debug(f"Token manager initialized for device {self.device.device_id}")

    def update_device(self, device: DeviceConfig) -> None:
        self.device = device
        self._init_token_manager_if_registered()

    async def register(self) -> str:
        if not self._api_gateway:
            raise RuntimeError("Client not initialized. Use async with.")

        logger.info(f"Registering device {self.device.device_id}")
        response = await self._api_gateway.register_device(self.device.provisioning_token)

        secret = response["hmac_secret"]
        logger.info("Device registered successfully")
        return secret

    async def send_telemetry(self, telemetry: TelemetryData) -> None:
        if not self._api_gateway:
            raise RuntimeError("Client not initialized. Use async with.")

        if not self._token_manager:
            raise RuntimeError(
                "Device not registered. Call register() and update_device() first."
            )

        token = await self._token_manager.get_token()

        logger.info(f"Sending telemetry for ts={telemetry.timestamp}")
        payload = telemetry.to_api_payload()
        await self._api_gateway.send_telemetry(token, payload)
        logger.info("Telemetry sent successfully")

    def invalidate_token(self) -> None:
        """Invalidate cached token for this device."""
        if self._token_manager:
            self._token_manager.invalidate()