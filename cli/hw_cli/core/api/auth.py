import hashlib
import hmac

def create_hmac_signature(device_id: str, hmac_secret: str, timestamp: int) -> str:
    """Generate HMAC-SHA256 signature for auth challenge."""
    message = f"{device_id}:{timestamp}"
    return hmac.new(
        hmac_secret.encode(),
        message.encode(),
        hashlib.sha256
    ).hexdigest()
