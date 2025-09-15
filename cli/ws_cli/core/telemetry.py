import asyncio
import random
from datetime import datetime

from ws_cli.core.interfaces import WeatherDataGenerator
from ws_cli.models import WeatherData, Device
from ws_cli.utils.console import print_info, print_success


class DummyDataGenerator(WeatherDataGenerator):
    """Generates simple, random weather data."""

    def generate(self, device: Device) -> WeatherData:
        return WeatherData(
            timestamp=datetime.now(),
            temperature=round(random.uniform(15.0, 25.0), 2),
            humidity=round(random.uniform(40.0, 70.0), 2),
            pressure=round(random.uniform(1010.0, 1025.0), 2),
            rain_tips=random.randint(0, 5),
            device_id=device.device_id,
        )


class ConsoleTelemetryTransmitter:
    """A transmitter that prints telemetry to the console instead of sending it."""

    async def connect(self) -> None:
        print_info("Establishing (dummy) connection...")
        await asyncio.sleep(0.1)  # Simulate connection time
        print_success("Connection established.")

    async def send(self, data: WeatherData) -> None:
        print_info(f"Sending telemetry for device '{data.device_id}':")
        print(data.to_dict())
        await asyncio.sleep(0.2)  # Simulate send time

    async def disconnect(self) -> None:
        print_info("Closing (dummy) connection...")
        await asyncio.sleep(0.1)
        print_success("Connection closed.")

# TODO: Add AzureIoTTransmitter that implements the TelemetryTransmitter protocol
# It will contain the logic from your old project's azure_client.py