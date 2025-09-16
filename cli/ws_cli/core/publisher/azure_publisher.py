import asyncio
import logging
import uuid
from typing import Optional

from azure.iot.device import Message, X509
from azure.iot.device.aio import IoTHubDeviceClient

from .data_publisher import DataPublisher
from ..identity.azure_dps_provisioner import AzureDPSProvisioner, exponential_backoff_retry
from ..models import weather_pb2
from ..models.models import Device, WeatherData

logger = logging.getLogger(__name__)


class AzureDataPublisher(DataPublisher):
    """Implements TelemetryTransmitter protocol for Azure IoT Hub via DPS with caching."""

    def __init__(self, device_cfg: Device, provisioner: Optional[AzureDPSProvisioner] = None,
                 max_retry_attempts: int = 8, base_retry_backoff_s: float = 1.0,
                 progress=None, task_id=None, force_provision: bool = False):
        self.device_cfg = device_cfg
        self.provisioner = provisioner or AzureDPSProvisioner()
        self.max_retry_attempts = max_retry_attempts
        self.base_retry_backoff_s = base_retry_backoff_s
        self.force_provision = force_provision
        self.client: Optional[IoTHubDeviceClient] = None
        self.progress = progress
        self.task_id = task_id
        self._device_identity: Optional[dict] = None

    def _update_progress(self, description: str):
        """Update progress bar if available."""
        if self.progress and self.task_id is not None:
            self.progress.update(self.task_id, description=description)

    async def connect(self) -> None:
        """Connects to the telemetry endpoint via DPS provisioning (cached)."""
        self._update_progress("Getting device identity...")

        # Get identity (from cache or provision)
        self._device_identity = await self.provisioner.get_device_identity(
            self.device_cfg,
            force_provision=self.force_provision
        )

        self._update_progress("Connecting to IoT Hub...")
        self.client = await self._create_iot_hub_client()

    async def send(self, data: WeatherData) -> None:
        """Sends a weather data point."""
        if not self.client:
            raise RuntimeError("Client not connected. Call connect() first.")

        self._update_progress("Sending telemetry to Azure...")

        # Serialize to protobuf
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
            'deviceId': self.device_cfg.device_id,
            'type': 'weather-proto',
            'schema-version': '1'
        }

        try:
            await self.client.send_message(msg)
            logger.info("Sent telemetry: temp=%.2f°C hum=%.2f%% pres=%.2fhPa",
                        data.temperature, data.humidity, data.pressure)
        except Exception as e:
            logger.error("Failed to send telemetry: %s", e)
            # If send fails, it could be due to expired connection or other issues
            # The caller can decide whether to retry or invalidate cache
            raise

    async def disconnect(self) -> None:
        """Disconnects from the telemetry endpoint."""
        if self.client:
            self._update_progress("Disconnecting from Azure...")
            try:
                await self.client.disconnect()
                logger.info("Disconnected from IoT Hub")
            except Exception as e:
                logger.warning("Error while disconnecting: %s", e)
            finally:
                self.client = None

    async def _create_iot_hub_client(self) -> IoTHubDeviceClient:
        """Create IoT Hub client from cached identity."""
        if not self._device_identity:
            raise RuntimeError("No device identity available")

        assigned_hub = self._device_identity["assigned_hub"]
        auth_info = self._device_identity["auth_info"]

        # Create client based on auth type from cached identity
        if auth_info["type"] == "x509":
            x509_info = auth_info["x509"]
            x509auth = X509(
                cert_file=x509_info["cert_file"],
                key_file=x509_info["key_file"],
                pass_phrase=x509_info.get("pass_phrase")
            )
            client = IoTHubDeviceClient.create_from_x509_certificate(
                x509=x509auth,
                hostname=assigned_hub,
                device_id=self.device_cfg.device_id
            )

        elif auth_info["type"] == "symmetric_key":
            client = IoTHubDeviceClient.create_from_symmetric_key(
                symmetric_key=auth_info["symmetric_key"],
                hostname=assigned_hub,
                device_id=self.device_cfg.device_id
            )

        else:
            raise ValueError(f"Unsupported auth type in cached identity: {auth_info['type']}")

        # Connect with retry
        async def do_connect():
            logger.info("Connecting IoT Hub client to %s ...", assigned_hub)
            await client.connect()
            logger.info("IoT Hub client connected")
            return client

        return await exponential_backoff_retry(
            do_connect,
            max_attempts=self.max_retry_attempts,
            base_backoff=self.base_retry_backoff_s,
            operation_name="IoT Hub connect"
        )

    async def invalidate_cache_and_reconnect(self):
        """
        Invalidate cached DPS identity and reconnect.
        Useful when connection fails due to potentially stale cached data.
        """
        logger.info("Invalidating DPS cache and reconnecting...")

        # Disconnect current client
        await self.disconnect()

        # Invalidate cache for this device
        self.provisioner.invalidate_device_cache(self.device_cfg)

        # Force provision on next connect
        self.force_provision = True

        # Reconnect
        await self.connect()