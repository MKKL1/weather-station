import math
import random
import time
from typing import Dict, Optional

from hw_cli.core.constants import (
    BASE_HUMIDITY_PERCENT,
    BASE_PRESSURE_HPA,
    BASE_TEMP_C,
    DEFAULT_BUCKET_SECONDS,
    DEFAULT_NUM_BUCKETS,
    RAIN_PROBABILITY_FACTOR
)
from hw_cli.core.models import DeviceConfig, RainfallHistogram, TelemetryData, WeatherReading


class DataGenerator:
    """Generates simulated weather telemetry data."""

    def __init__(self, seed: Optional[int] = None):
        self.base_temp = BASE_TEMP_C
        self.base_pressure = BASE_PRESSURE_HPA
        self.base_humidity = BASE_HUMIDITY_PERCENT
        self.rain_prob_factor = RAIN_PROBABILITY_FACTOR

        if seed is not None:
            random.seed(seed)

    def generate(self, device: DeviceConfig) -> TelemetryData:
        now = int(time.time())
        hour = (now % 86400) / 3600

        temp = self.base_temp + 5 * math.sin((hour - 6) * math.pi / 12) + random.gauss(0, 2)

        pressure = self.base_pressure + random.gauss(0, 5)
        humidity = max(0.0, min(100.0, self.base_humidity + random.gauss(0, 10)))

        rain = self._generate_rain(now, humidity)
        total_tips = sum(rain.data.values()) if rain.data else 0
        precip_mm = total_tips * device.mm_per_tip

        reading = WeatherReading(
            temperature=round(temp, 2),
            pressure=round(pressure, 2),
            humidity=round(humidity, 2),
            precipitation_mm=round(precip_mm, 2),
            rain=rain,
        )
        return TelemetryData(timestamp=now, reading=reading)

    def _generate_rain(self, now: int, humidity: float) -> RainfallHistogram:
        """Generate sparse rainfall histogram."""
        start_ts = now - DEFAULT_NUM_BUCKETS * DEFAULT_BUCKET_SECONDS
        data: Dict[str, int] = {}

        # Higher humidity increases chance of rain
        rain_factor = (humidity / 100.0) * self.rain_prob_factor

        for i in range(DEFAULT_NUM_BUCKETS):
            if random.random() < rain_factor:
                tips = random.randint(1, 5)
                data[str(i)] = tips

        return RainfallHistogram(
            data=data,
            bucket_seconds=DEFAULT_BUCKET_SECONDS,
            start_timestamp=start_ts,
            num_buckets=DEFAULT_NUM_BUCKETS,
        )