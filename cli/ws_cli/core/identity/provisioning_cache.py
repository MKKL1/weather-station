import logging

from ws_cli.core.models.models import Device

logger = logging.getLogger(__name__)

import time

from pathlib import Path
from typing import Dict, Any, Optional
import hashlib

from ws_cli.core.config import ConfigManager


class ProvisioningCacheManager:
    """Manages DPS provisioning cache using a separate ConfigManager instance."""

    def __init__(self, config_manager: Optional[ConfigManager] = None):
        if config_manager:
            self.config_manager = config_manager
        else:
            # Create a separate config manager for DPS cache
            cache_path = Path(ConfigManager().get_config_path()).parent / "dps_cache.json"
            self.config_manager = ConfigManager()
            self.config_manager._config_path = cache_path

    def _cache_key(self, device: Device) -> str:
        """Generate a cache key for a device configuration."""
        # Include auth type and key identifiers that affect provisioning
        key_parts = [
            device.device_id,
            device.dps_config.id_scope if device.dps_config else "",
            device.dps_config.registration_id or device.device_id if device.dps_config else "",
            device.auth.type.value
        ]

        # Add auth-specific identifiers
        if device.auth.type == "x509":
            # For X.509, include cert file path as it affects the identity
            key_parts.append(device.auth.cert_file or "")
        elif device.auth.type == "symmetric_key":
            # For symmetric key, include a hash of the key to detect changes
            key_hash = hashlib.sha256((device.auth.symmetric_key or "").encode()).hexdigest()[:16]
            key_parts.append(key_hash)

        return ":".join(key_parts)

    def get_cached_identity(self, device: Device) -> Optional[Dict[str, Any]]:
        """Get cached identity for a device if available and valid."""
        cache_key = self._cache_key(device)
        identity_data = self.config_manager.get(cache_key)

        if not identity_data:
            return None

        # Check if cache entry is expired
        cached_time = identity_data.get("cached_at", 0)
        cache_ttl = identity_data.get("ttl", 3600)  # Default 1 hour TTL

        if time.time() - cached_time > cache_ttl:
            logger.info(f"DPS cache entry expired for device {device.device_id}")
            # Remove expired entry
            self.config_manager.delete(cache_key)
            return None

        logger.info(f"Using cached DPS identity for device {device.device_id}")
        return identity_data.get("identity")

    def cache_identity(self, device: Device, identity: Dict[str, Any], ttl: int = 3600):
        """Cache an identity for a device."""
        cache_key = self._cache_key(device)

        cache_entry = {
            "identity": identity,
            "cached_at": time.time(),
            "ttl": ttl,
            "device_id": device.device_id
        }

        self.config_manager.set(cache_key, cache_entry)
        logger.info(f"Cached DPS identity for device {device.device_id} (TTL: {ttl}s)")

    def invalidate_device(self, device: Device):
        """Invalidate cached identity for a specific device."""
        cache_key = self._cache_key(device)

        if self.config_manager.exists(cache_key):
            self.config_manager.delete(cache_key)
            logger.info(f"Invalidated DPS cache for device {device.device_id}")

    def clear_cache(self):
        """Clear all cached identities."""
        # Get all cache entries and delete them
        all_config = self.config_manager.get_all()
        cache_keys = list(all_config.keys())

        for key in cache_keys:
            self.config_manager.delete(key)

        logger.info("Cleared all DPS cache entries")

    def get_all_cached_devices(self) -> Dict[str, Dict[str, Any]]:
        """Get all cached device identities with metadata."""
        all_config = self.config_manager.get_all()
        current_time = time.time()

        result = {}
        for cache_key, entry in all_config.items():
            if not isinstance(entry, dict) or "device_id" not in entry:
                continue

            device_id = entry.get("device_id", "unknown")
            cached_at = entry.get("cached_at", 0)
            ttl = entry.get("ttl", 3600)
            expires_at = cached_at + ttl

            result[device_id] = {
                "cache_key": cache_key,
                "cached_at": cached_at,
                "expires_at": expires_at,
                "ttl": ttl,
                "is_expired": current_time > expires_at,
                "identity": entry.get("identity", {})
            }

        return result

    def cleanup_expired_entries(self) -> int:
        """Remove all expired cache entries and return count of removed entries."""
        all_config = self.config_manager.get_all()
        current_time = time.time()
        removed_count = 0

        for cache_key, entry in all_config.items():
            if not isinstance(entry, dict):
                continue

            cached_at = entry.get("cached_at", 0)
            ttl = entry.get("ttl", 3600)

            if current_time - cached_at > ttl:
                self.config_manager.delete(cache_key)
                removed_count += 1
                device_id = entry.get("device_id", "unknown")
                logger.info(f"Removed expired cache entry for device {device_id}")

        return removed_count

    def get_cache_stats(self) -> Dict[str, Any]:
        """Get statistics about the cache."""
        cached_devices = self.get_all_cached_devices()
        total_entries = len(cached_devices)
        expired_entries = sum(1 for entry in cached_devices.values() if entry["is_expired"])
        valid_entries = total_entries - expired_entries

        return {
            "total_entries": total_entries,
            "valid_entries": valid_entries,
            "expired_entries": expired_entries,
            "cache_file": self.config_manager.get_config_path()
        }


# Backward compatibility - keep the old ProvisioningCache name
class ProvisioningCache(ProvisioningCacheManager):
    """Backward compatibility alias for ProvisioningCacheManager."""

    def __init__(self, config_manager=None):
        # For backward compatibility, accept the old-style config_manager parameter
        if config_manager and hasattr(config_manager, 'get_config_path'):
            # Convert old-style config manager to new pattern
            cache_path = Path(config_manager.get_config_path()).parent / "dps_cache.json"
            new_config_manager = ConfigManager()
            new_config_manager._config_path = cache_path
            super().__init__(new_config_manager)
        else:
            super().__init__(config_manager)