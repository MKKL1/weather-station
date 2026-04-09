import logging
import time

from hw_cli.core.api.auth import create_hmac_signature
from hw_cli.core.api.weather_api_gateway import WeatherApiGateway
from hw_cli.core.token_cache import get_token_cache

logger = logging.getLogger(__name__)


class TokenManager:
    def __init__(
        self,
        device_id: str,
        provisioning_token: str,
        hmac_secret: str,
        api_gateway: WeatherApiGateway,
        cache=None,
    ):
        self.device_id = device_id
        self.provisioning_token = provisioning_token
        self.hmac_secret = hmac_secret
        self.api_gateway = api_gateway
        self.cache = cache or get_token_cache()

    async def get_token(self, force_refresh: bool = False) -> str:
        if not force_refresh:
            cached = self.cache.get_token(self.device_id)
            if cached:
                logger.debug("Using cached access token")
                return cached

        logger.info("Requesting new access token")
        timestamp = int(time.time())
        signature = create_hmac_signature(self.device_id, self.hmac_secret, timestamp)

        response = await self.api_gateway.request_token(
            self.provisioning_token, self.device_id, timestamp, signature
        )

        token = response["token"]
        expires_in = response.get("expires_in", 86400)

        self.cache.set_token(self.device_id, token, expires_in)
        logger.info(f"Access token obtained (expires in {expires_in}s)")

        return token

    def invalidate(self) -> None:
        self.cache.invalidate(self.device_id)
