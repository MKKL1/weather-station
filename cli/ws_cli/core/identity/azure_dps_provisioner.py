import asyncio
import logging
import random
from typing import Dict, Optional, Any

from azure.iot.device import X509
from azure.iot.device.aio import ProvisioningDeviceClient

from ws_cli.core.config import ConfigManager
from ws_cli.core.identity.provisioning_cache import ProvisioningCache
from ws_cli.core.models.models import Device

logger = logging.getLogger(__name__)
DEFAULT_TTL = 3600

async def exponential_backoff_retry(fn, max_attempts=8, base_backoff=1.0, operation_name="operation"):
    """Exponential backoff retry helper."""
    attempt = 0
    while True:
        try:
            return await fn()
        except Exception as e:
            attempt += 1
            if attempt >= max_attempts:
                logger.exception("Failed %s after %d attempts: %s", operation_name, attempt, e)
                raise
            backoff = base_backoff * (2 ** (attempt - 1)) * (0.5 + random.random())
            logger.warning("%s failed (attempt %d/%d). Retrying in %.1fs. Error: %s",
                           operation_name, attempt, max_attempts, backoff, e)
            await asyncio.sleep(backoff)


class AzureDPSProvisioner:
    """Handles Azure Device Provisioning Service operations with caching."""

    def __init__(self, config_manager: Optional[ConfigManager] = None,
                 max_retry_attempts: int = 8, base_retry_backoff_s: float = 1.0):
        self.config_manager = config_manager or ConfigManager()
        self.cache = ProvisioningCache(self.config_manager)
        self.max_retry_attempts = max_retry_attempts
        self.base_retry_backoff_s = base_retry_backoff_s

    async def get_device_identity(self, device: Device, force_provision: bool = False) -> Dict[str, Any]:
        """
        Get device identity from cache or provision via DPS.

        Args:
            device: Device configuration
            force_provision: Force re-provisioning even if cached identity exists

        Returns:
            Dict containing assigned_hub, device_id, and auth_info
        """
        if not force_provision:
            cached_identity = self.cache.get_cached_identity(device)
            if cached_identity:
                return cached_identity

        # Need to provision
        logger.info(f"Provisioning device {device.device_id} via DPS...")
        identity = await self._provision_device(device)

        self.cache.cache_identity(device, identity, ttl=device.dps_config.ttl or DEFAULT_TTL)

        return identity

    async def _provision_device(self, device: Device) -> Dict[str, Any]:
        """Provision a device via Azure DPS."""
        if not device.dps_config:
            raise ValueError("Device missing DPS configuration")

        registration_id = device.dps_config.registration_id or device.device_id

        # Create provisioning client based on auth type
        if device.auth.type == "x509":
            if not device.auth.cert_file or not device.auth.key_file:
                raise ValueError("X.509 auth selected but cert_file/key_file not provided")

            x509auth = X509(
                cert_file=device.auth.cert_file,
                key_file=device.auth.key_file,
                pass_phrase=device.auth.key_passphrase
            )

            prov_client = ProvisioningDeviceClient.create_from_x509_certificate(
                provisioning_host=device.dps_config.provisioning_host,
                registration_id=registration_id,
                id_scope=device.dps_config.id_scope,
                x509=x509auth
            )

            auth_info = {
                "type": "x509",
                "x509": {
                    "cert_file": device.auth.cert_file,
                    "key_file": device.auth.key_file,
                    "pass_phrase": device.auth.key_passphrase
                }
            }

        elif device.auth.type == "symmetric_key":
            if not device.auth.symmetric_key:
                raise ValueError("Symmetric key auth selected but symmetric_key not provided")

            prov_client = ProvisioningDeviceClient.create_from_symmetric_key(
                provisioning_host=device.dps_config.provisioning_host,
                registration_id=registration_id,
                id_scope=device.dps_config.id_scope,
                symmetric_key=device.auth.symmetric_key
            )

            auth_info = {
                "type": "symmetric_key",
                "symmetric_key": device.auth.symmetric_key
            }

        else:
            raise ValueError(f"Unsupported auth type: {device.auth.type}")

        # Perform registration with retry
        async def do_register():
            logger.info("Registering device '%s' with DPS id_scope=%s ...",
                        registration_id, device.dps_config.id_scope)
            reg_result = await prov_client.register()
            logger.info("DPS registration result: %s", reg_result.status)
            if getattr(reg_result, "status", None) != "assigned":
                raise RuntimeError(f"DPS registration not assigned: {reg_result}")
            return reg_result

        reg_result = await exponential_backoff_retry(
            do_register,
            max_attempts=self.max_retry_attempts,
            base_backoff=self.base_retry_backoff_s,
            operation_name="DPS provisioning"
        )

        assigned_hub = reg_result.registration_state.assigned_hub
        logger.info("Device assigned to hub: %s", assigned_hub)

        return {
            "assigned_hub": assigned_hub,
            "device_id": device.device_id,
            "auth_info": auth_info,
            "registration_result": {
                "status": reg_result.status,
                "assigned_hub": assigned_hub,
                "device_id": reg_result.registration_state.device_id,
                "operation_id": reg_result.operation_id
            }
        }

    def invalidate_device_cache(self, device: Device):
        """Invalidate cached identity for a device (e.g., after config changes)."""
        self.cache.invalidate_device(device)

    def clear_all_cache(self):
        """Clear all cached DPS identities."""
        self.cache.clear_cache()