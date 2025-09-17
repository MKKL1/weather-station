import asyncio
import logging
import random
from typing import Dict, Any

from azure.iot.device import X509
from azure.iot.device.aio import ProvisioningDeviceClient

from hw_cli.core.models.models import Device

logger = logging.getLogger(__name__)


async def retry_with_backoff(operation, max_attempts=8, base_delay=1.0, operation_name="operation"):
    """Simple retry with exponential backoff."""
    for attempt in range(max_attempts):
        try:
            return await operation()
        except Exception as e:
            if attempt == max_attempts - 1:
                logger.error(f"Failed {operation_name} after {max_attempts} attempts: {e}")
                raise

            delay = base_delay * (2 ** attempt) * (0.5 + random.random())
            logger.warning(f"{operation_name} failed (attempt {attempt + 1}/{max_attempts}). "
                           f"Retrying in {delay:.1f}s. Error: {e}")
            await asyncio.sleep(delay)


async def provision_device(device: Device) -> Dict[str, Any]:
    """
    Provision device via Azure DPS.
    Returns dict with assigned_hub, device_id, and auth_info.
    """
    if not device.dps_config:
        raise ValueError("Device missing DPS configuration")

    registration_id = device.dps_config.registration_id or device.device_id

    # Create provisioning client
    if device.auth.type == "x509":
        if not device.auth.cert_file or not device.auth.key_file:
            raise ValueError("X.509 auth requires cert_file and key_file")

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
            "cert_file": device.auth.cert_file,
            "key_file": device.auth.key_file,
            "pass_phrase": device.auth.key_passphrase
        }

    elif device.auth.type == "symmetric_key":
        if not device.auth.symmetric_key:
            raise ValueError("Symmetric key auth requires symmetric_key")

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

    # Register device with retry
    async def do_register():
        logger.info(f"Registering device '{registration_id}' with DPS...")
        reg_result = await prov_client.register()
        if getattr(reg_result, "status", None) != "assigned":
            raise RuntimeError(f"DPS registration failed: {reg_result}")
        return reg_result

    reg_result = await retry_with_backoff(do_register, operation_name="DPS provisioning")

    assigned_hub = reg_result.registration_state.assigned_hub
    logger.info(f"Device assigned to hub: {assigned_hub}")

    return {
        "assigned_hub": assigned_hub,
        "device_id": device.device_id,
        "auth_info": auth_info
    }