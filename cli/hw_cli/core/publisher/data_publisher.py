from typing import Protocol

from hw_cli.core.models.models import WeatherData


class DataPublisher(Protocol):
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