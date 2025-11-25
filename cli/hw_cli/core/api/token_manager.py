import logging
import time

from hw_cli.core.api.device_authenticator import DeviceAuthenticator
from hw_cli.core.api.weather_api_gateway import WeatherApiGateway
from hw_cli.core.token_cache import get_token_cache

logger = logging.getLogger(__name__)


class TokenManager:
    """Manages access token lifecycle"""

    def __init__(
            self,
            device_id: str,
            provisioning_token: str,
            authenticator: DeviceAuthenticator,
            api_gateway: WeatherApiGateway,
            cache=None
    ):
        self.device_id = device_id
        self.provisioning_token = provisioning_token
        self.authenticator = authenticator
        self.api_gateway = api_gateway
        self.cache = cache or get_token_cache()

    async def get_token(self, force_refresh: bool = False) -> str:
        """Get valid access token, refreshing if needed."""
        if not force_refresh:
            cached = self.cache.get_token(self.device_id)
            if cached:
                logger.debug("Using cached access token")
                return cached

        logger.info("Requesting new access token")
        timestamp = int(time.time())
        signature = self.authenticator.create_signature(timestamp)

        response = await self.api_gateway.request_token(
            self.provisioning_token,
            timestamp,
            signature
        )

        token = response["token"]
        expires_in = response.get("expiresIn", 86400)

        self.cache.set_token(self.device_id, token, expires_in)
        logger.info(f"Access token obtained (expires in {expires_in}s)")

        return token

    def invalidate(self) -> None:
        """Invalidate cached token."""
        self.cache.invalidate(self.device_id)
