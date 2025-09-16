import logging
import time
import hashlib
from typing import Optional, Dict, Any

from ws_cli.core.storage import get_data

logger = logging.getLogger(__name__)


class ProvisioningCache:
    """Simple DPS provisioning cache."""

    def __init__(self):
        self._storage = get_data()
        self._cache = self._storage.section("dps_cache")

    def _cache_key(self, device) -> str:
        """Generate cache key for device."""
        parts = [
            device.device_id,
            device.dps_config.id_scope if device.dps_config else "",
            device.dps_config.registration_id or device.device_id if device.dps_config else "",
            device.auth.type.value
        ]

        if device.auth.type == "x509":
            parts.append(device.auth.cert_file or "")
        elif device.auth.type == "symmetric_key":
            key_hash = hashlib.sha256((device.auth.symmetric_key or "").encode()).hexdigest()[:16]
            parts.append(key_hash)

        return ":".join(parts)

    def get_cached_identity(self, device) -> Optional[Dict[str, Any]]:
        """Get cached identity if valid."""
        cache_key = self._cache_key(device)
        entry = self._cache.get_item(cache_key)

        if not entry:
            return None

        cached_at = entry.get("cached_at", 0)
        ttl = entry.get("ttl", 3600)

        if time.time() - cached_at > ttl:
            logger.info(f"DPS cache expired for device {device.device_id}")
            self._cache.delete_item(cache_key)
            return None

        logger.info(f"Using cached DPS identity for device {device.device_id}")
        return entry.get("identity")

    def cache_identity(self, device, identity: Dict[str, Any], ttl: int = 3600):
        """Cache device identity."""
        cache_key = self._cache_key(device)

        entry = {
            "identity": identity,
            "cached_at": time.time(),
            "ttl": ttl,
            "device_id": device.device_id
        }

        self._cache.set_item(cache_key, entry)
        logger.info(f"Cached DPS identity for device {device.device_id} (TTL: {ttl}s)")

    def invalidate_device(self, device):
        """Remove cached identity for device."""
        cache_key = self._cache_key(device)

        if self._cache.exists(cache_key):
            self._cache.delete_item(cache_key)
            logger.info(f"Invalidated DPS cache for device {device.device_id}")

    def clear_cache(self):
        """Clear all cached identities."""
        self._cache.set({})
        logger.info("Cleared all DPS cache entries")

    def get_cache_stats(self) -> Dict[str, Any]:
        """Get cache statistics."""
        cache_data = self._cache.get()
        current_time = time.time()

        total = len(cache_data)
        expired = sum(1 for entry in cache_data.values()
                      if isinstance(entry, dict) and
                      current_time - entry.get("cached_at", 0) > entry.get("ttl", 3600))

        return {
            "total_entries": total,
            "valid_entries": total - expired,
            "expired_entries": expired,
            "cache_file": self._storage.path
        }

    def cleanup_expired(self) -> int:
        """Remove expired entries, return count removed."""
        cache_data = self._cache.get()
        current_time = time.time()
        removed = 0

        for cache_key, entry in list(cache_data.items()):
            if not isinstance(entry, dict):
                continue

            cached_at = entry.get("cached_at", 0)
            ttl = entry.get("ttl", 3600)

            if current_time - cached_at > ttl:
                self._cache.delete_item(cache_key)
                removed += 1
                device_id = entry.get("device_id", "unknown")
                logger.info(f"Removed expired cache entry for device {device_id}")

        return removed


# Global instance for convenience
_cache_instance: Optional[ProvisioningCache] = None


def get_provisioning_cache() -> ProvisioningCache:
    """Get provisioning cache singleton."""
    global _cache_instance
    if _cache_instance is None:
        _cache_instance = ProvisioningCache()
    return _cache_instance