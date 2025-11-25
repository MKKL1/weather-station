import logging
import threading
import time
from typing import Any, Dict, Optional

from hw_cli.core.constants import TOKEN_REFRESH_BUFFER_SEC
from hw_cli.core.storage import get_data

logger = logging.getLogger(__name__)


class TokenCache:
    """Caches JWT access tokens per device using SQLite backend."""

    def __init__(self):
        self._db = get_data()
        # Lock is less critical now that SQLite handles concurrency,
        # but kept to prevent race conditions in the 'get-check-delete' logic
        self._lock = threading.Lock()

    def get_token(self, device_id: str) -> Optional[str]:
        """Get cached token if still valid."""
        with self._lock:
            entry = self._db.get_token_entry(device_id)
            if not entry:
                return None

            expires_at = entry.get("expires_at", 0)
            current_time = time.time()

            # Buffer before expiration
            if current_time > expires_at - TOKEN_REFRESH_BUFFER_SEC:
                logger.info(f"Token expired or expiring soon for device {device_id}")
                self._db.delete_token(device_id)
                return None

            logger.debug(f"Using cached token for device {device_id}")
            return entry.get("token")

    def set_token(self, device_id: str, token: str, expires_in: int) -> None:
        """Cache a token."""
        with self._lock:
            current_time = int(time.time())
            expires_at = current_time + expires_in

            self._db.save_token(
                device_id=device_id,
                token=token,
                expires_at=expires_at,
                cached_at=current_time,
                expires_in=expires_in
            )
            logger.info(f"Cached token for device {device_id} (expires in {expires_in}s)")

    def invalidate(self, device_id: str) -> bool:
        """Remove cached token for a device."""
        with self._lock:
            if self._db.delete_token(device_id):
                logger.info(f"Invalidated token for device {device_id}")
                return True
            return False

    def clear_all(self) -> int:
        """Clear all cached tokens, return count removed."""
        with self._lock:
            count = self._db.clear_all_tokens()
            logger.info(f"Cleared {count} cached tokens")
            return count

    def get_all_entries(self) -> Dict[str, Any]:
        """Get all cache entries for display."""
        with self._lock:
            return self._db.get_all_tokens()

    def get_stats(self) -> Dict[str, Any]:
        """Get cache statistics."""
        with self._lock:
            entries = self._db.get_all_tokens()
            now = time.time()

            total = len(entries)
            valid = sum(
                1 for e in entries.values()
                if e.get("expires_at", 0) > now
            )
            expired = total - valid

            return {
                "total": total,
                "valid": valid,
                "expired": expired,
            }

    def cleanup_expired(self) -> int:
        """Remove expired entries, return count removed."""
        with self._lock:
            removed = self._db.delete_expired_tokens()
            if removed > 0:
                logger.debug(f"Cleaned up {removed} expired tokens")
            return removed


_cache_instance: Optional[TokenCache] = None
_cache_lock = threading.Lock()


def get_token_cache() -> TokenCache:
    """Get token cache singleton (thread-safe)."""
    global _cache_instance
    if _cache_instance is None:
        with _cache_lock:
            if _cache_instance is None:
                _cache_instance = TokenCache()
    return _cache_instance