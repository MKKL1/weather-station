import logging
import uuid
from typing import Optional

from azure.iot.device import Message, X509
from azure.iot.device.aio import IoTHubDeviceClient

from .data_publisher import DataPublisher
from ..identity.azure_dps_provisioner import retry_with_backoff, provision_device
from ..models import weather_pb2
from ..models.models import Device, WeatherData
from ..identity.provisioning_cache import get_provisioning_cache

logger = logging.getLogger(__name__)


def create_azure_message(data: WeatherData, device: Device) -> Message:
    """Convert WeatherData to Azure IoT Hub Message."""
    # Create protobuf
    tips_msg = weather_pb2.Histogram(
        data=data.tips.data,
        count=data.tips.count,
        interval_duration=data.tips.interval_duration,
        start_time=data.tips.start_time
    )

    info_msg = weather_pb2.DeviceInfo(
        id=data.info.id,
        mmPerTip=data.info.mm_per_tip,
        instanceId=data.info.instance_id
    )

    weather_msg = weather_pb2.WeatherData(
        created_at=data.created_at,
        temperature=data.temperature,
        pressure=data.pressure,
        humidity=data.humidity,
        tips=tips_msg,
        info=info_msg
    )

    payload_bytes = weather_msg.SerializeToString()

    msg = Message(payload_bytes)
    msg.message_id = str(uuid.uuid4())
    msg.content_type = 'application/x-protobuf'
    msg.custom_properties = {
        'deviceId': device.device_id,
        'type': 'weather-proto',
        'schema-version': '1'
    }

    return msg


async def create_iot_hub_client(device: Device, identity: dict) -> IoTHubDeviceClient:
    """Create and connect IoT Hub client from identity."""
    assigned_hub = identity["assigned_hub"]
    auth_info = identity["auth_info"]

    # Create client
    if auth_info["type"] == "x509":
        x509auth = X509(
            cert_file=auth_info["cert_file"],
            key_file=auth_info["key_file"],
            pass_phrase=auth_info.get("pass_phrase")
        )
        client = IoTHubDeviceClient.create_from_x509_certificate(
            x509=x509auth,
            hostname=assigned_hub,
            device_id=device.device_id
        )
    elif auth_info["type"] == "symmetric_key":
        client = IoTHubDeviceClient.create_from_symmetric_key(
            symmetric_key=auth_info["symmetric_key"],
            hostname=assigned_hub,
            device_id=device.device_id
        )
    else:
        raise ValueError(f"Unknown auth type: {auth_info['type']}")

    # Connect with retry
    async def do_connect():
        logger.info(f"Connecting to IoT Hub: {assigned_hub}")
        await client.connect()
        logger.info("IoT Hub client connected")

    await retry_with_backoff(do_connect, operation_name="IoT Hub connect")
    return client


class AzureDataPublisher(DataPublisher):
    """Publishes telemetry to Azure IoT Hub via DPS."""

    def __init__(self, device_cfg: Device, force_provision=False, progress=None, task_id=None):
        self.device_cfg = device_cfg
        self.force_provision = force_provision
        self.progress = progress
        self.task_id = task_id
        self.client: Optional[IoTHubDeviceClient] = None
        self.cache = get_provisioning_cache()

    def _update_progress(self, description: str):
        if self.progress and self.task_id is not None:
            self.progress.update(self.task_id, description=description)

    async def connect(self) -> None:
        self._update_progress("Getting device identity...")

        # Try cache first unless forcing provision
        identity = None
        if not self.force_provision:
            identity = self.cache.get_cached_identity(self.device_cfg)

        # Provision if needed
        if not identity:
            self._update_progress("Provisioning via DPS...")
            identity = await provision_device(self.device_cfg)

            # Cache it
            ttl = self.device_cfg.dps_config.ttl if self.device_cfg.dps_config else 3600
            self.cache.cache_identity(self.device_cfg, identity, ttl=ttl)

        self._update_progress("Connecting to IoT Hub...")
        self.client = await create_iot_hub_client(self.device_cfg, identity)

    async def send(self, data: WeatherData) -> None:
        if not self.client:
            raise RuntimeError("Not connected. Call connect() first.")

        self._update_progress("Sending telemetry...")

        message = create_azure_message(data, self.device_cfg)
        await self.client.send_message(message)

        logger.info(f"Sent telemetry: temp={data.temperature:.1f}°C "
                    f"hum={data.humidity:.1f}% pres={data.pressure:.1f}hPa")

    async def disconnect(self) -> None:
        if self.client:
            self._update_progress("Disconnecting...")
            try:
                await self.client.disconnect()
                logger.info("Disconnected from IoT Hub")
            except Exception as e:
                logger.warning(f"Disconnect error: {e}")
            finally:
                self.client = None

    async def invalidate_cache_and_reconnect(self):
        """Clear cache and reconnect (for handling stale credentials)."""
        logger.info("Invalidating cache and reconnecting...")
        await self.disconnect()
        self.cache.invalidate_device(self.device_cfg)
        self.force_provision = True
        await self.connect()