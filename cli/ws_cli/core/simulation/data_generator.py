from abc import ABC, abstractmethod

from ws_cli.core.models.models import Device, WeatherData


class WeatherDataGenerator(ABC):
    """Abstract interface for weather data generation."""

    @abstractmethod
    def generate(self, device: Device) -> WeatherData:
        """Generates a single weather data point."""
        raise NotImplementedError