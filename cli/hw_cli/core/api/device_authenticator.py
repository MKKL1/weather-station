import hashlib
import hmac
import logging

logger = logging.getLogger(__name__)

class DeviceAuthenticator:
    """Handles device authentication logic."""

    def __init__(self, device_id: str, hmac_secret: str):
        self.device_id = device_id
        self.hmac_secret = hmac_secret

    def create_signature(self, timestamp: int) -> str:
        """Generate HMAC-SHA256 signature for auth challenge."""
        message = f"{self.device_id}:{timestamp}"
        sig = hmac.new(
            bytes.fromhex(self.hmac_secret),
            message.encode(),
            hashlib.sha256
        ).hexdigest()
        return sig