import asyncio
import logging
import random
import uuid
from typing import Optional

from azure.iot.device import Message, X509
from azure.iot.device.aio import ProvisioningDeviceClient, IoTHubDeviceClient

from . import weather_pb2
from ws_cli.core.interfaces import TelemetryTransmitter

from ws_cli.models import AuthConfig, DPSConfig, Device, WeatherData

logger = logging.getLogger(__name__)

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


class AzureTelemetryTransmitter(TelemetryTransmitter):
    """Implements TelemetryTransmitter protocol for Azure IoT Hub via DPS."""

    def __init__(self, device_cfg: Device, dps_cfg: DPSConfig, max_retry_attempts: int = 8,
                 base_retry_backoff_s: float = 1.0, progress=None, task_id=None):
        self.device_cfg = device_cfg
        self.dps_cfg = dps_cfg
        self.max_retry_attempts = max_retry_attempts
        self.base_retry_backoff_s = base_retry_backoff_s
        self.client: Optional[IoTHubDeviceClient] = None
        self.auth: AuthConfig = device_cfg.auth
        self.progress = progress
        self.task_id = task_id

    def _update_progress(self, description: str):
        """Update progress bar if available."""
        if self.progress and self.task_id is not None:
            self.progress.update(self.task_id, description=description)

    async def connect(self) -> None:
        """Connects to the telemetry endpoint via DPS provisioning."""
        self._update_progress("Connecting to Azure IoT Hub...")
        self.client = await self._provision_and_connect()

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

        await self.client.send_message(msg)
        logger.info("Sent telemetry: temp=%.2f°C hum=%.2f%% pres=%.2fhPa",
                    data.temperature, data.humidity, data.pressure)

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

    async def _provision_and_connect(self):
        """Internal: Provision via DPS and connect to IoT Hub."""
        registration_id = self.dps_cfg.registration_id or self.device_cfg.device_id

        self._update_progress("Provisioning device with Azure DPS...")

        # Prepare provisioning client
        def create_provisioning_client_x509():
            x509auth = X509(
                cert_file=self.auth.cert_file,
                key_file=self.auth.key_file,
                pass_phrase=self.auth.key_passphrase
            )
            return ProvisioningDeviceClient.create_from_x509_certificate(
                provisioning_host=self.dps_cfg.provisioning_host,
                registration_id=registration_id,
                id_scope=self.dps_cfg.id_scope,
                x509=x509auth
            ), x509auth

        def create_provisioning_client_symmetric():
            return ProvisioningDeviceClient.create_from_symmetric_key(
                provisioning_host=self.dps_cfg.provisioning_host,
                registration_id=registration_id,
                id_scope=self.dps_cfg.id_scope,
                symmetric_key=self.auth.symmetric_key
            ), None

        if self.auth.type == "x509":
            if not self.auth.cert_file or not self.auth.key_file:
                raise ValueError("X.509 auth selected but cert_file/key_file not provided")
            prov_client, x509 = create_provisioning_client_x509()
        else:
            if not self.auth.symmetric_key:
                raise ValueError("Symmetric key auth selected but symmetric_key not provided")
            prov_client, x509 = create_provisioning_client_symmetric()

        async def do_register():
            logger.info("Registering device '%s' with DPS id_scope=%s ...", registration_id, self.dps_cfg.id_scope)
            reg_result = await prov_client.register()
            logger.info("DPS registration result: %s", reg_result.status)
            if getattr(reg_result, "status", None) != "assigned":
                raise RuntimeError(f"DPS registration not assigned: {reg_result}")
            return reg_result

        reg = await exponential_backoff_retry(
            lambda: do_register(),
            max_attempts=self.max_retry_attempts,
            base_backoff=self.base_retry_backoff_s,
            operation_name="DPS provisioning"
        )

        assigned_hub = reg.registration_state.assigned_hub
        logger.info("Device assigned to hub: %s", assigned_hub)

        self._update_progress("Establishing IoT Hub connection...")

        # Create IoTHubDeviceClient
        if self.auth.type == "x509":
            client = IoTHubDeviceClient.create_from_x509_certificate(
                x509=x509,
                hostname=assigned_hub,
                device_id=self.device_cfg.device_id
            )
        else:
            client = IoTHubDeviceClient.create_from_symmetric_key(
                symmetric_key=self.auth.symmetric_key,
                hostname=assigned_hub,
                device_id=self.device_cfg.device_id
            )

        async def do_connect():
            logger.info("Connecting IoT Hub client to %s ...", assigned_hub)
            await client.connect()
            logger.info("IoT Hub client connected")
            return client

        return await exponential_backoff_retry(
            lambda: do_connect(),
            max_attempts=self.max_retry_attempts,
            base_backoff=self.base_retry_backoff_s,
            operation_name="IoT Hub connect"
        )