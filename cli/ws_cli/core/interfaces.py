from abc import ABC, abstractmethod
from typing import Protocol

from ws_cli.models import WeatherData, Device


class WeatherDataGenerator(ABC):
    """Abstract interface for weather data generation."""

    @abstractmethod
    def generate(self, device: Device) -> WeatherData:
        """Generates a single weather data point."""
        raise NotImplementedError


class TelemetryTransmitter(Protocol):
    """Protocol for sending telemetry data asynchronously."""

    async def connect(self) -> None:
        """Connects to the telemetry endpoint."""
        ...

    async def send(self, data: WeatherData) -> None:
        """Sends a weather data point."""
        ...

    async def disconnect(self) -> None:
        """Disconnects from the telemetry endpoint."""
        ...