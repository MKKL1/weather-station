from dataclasses import dataclass, field
from datetime import datetime
from typing import Any, Dict, Optional


@dataclass
class DeviceConfig:
    device_id: str  # The actual device ID from JWT
    name: str  # Human-friendly name for CLI reference
    api_base_url: str
    provisioning_token: str
    hmac_secret: Optional[str] = None
    mm_per_tip: float = 0.2
    metadata: Dict[str, Any] = field(default_factory=dict)
    created_at: datetime = field(default_factory=datetime.now)

    @property
    def is_registered(self) -> bool:
        return self.hmac_secret is not None

    def to_dict(self) -> Dict[str, Any]:
        return {
            "device_id": self.device_id,
            "name": self.name,
            "api_base_url": self.api_base_url,
            "provisioning_token": self.provisioning_token,
            "hmac_secret": self.hmac_secret,
            "mm_per_tip": self.mm_per_tip,
            "metadata": self.metadata,
            "created_at": self.created_at.isoformat(),
        }

    @classmethod
    def from_dict(cls, data: Dict[str, Any]) -> "DeviceConfig":
        # Handle migration: if 'name' is missing, use device_id as name
        name = data.get("name", data["device_id"])

        return cls(
            device_id=data["device_id"],
            name=name,
            api_base_url=data["api_base_url"],
            provisioning_token=data["provisioning_token"],
            hmac_secret=data.get("hmac_secret"),
            mm_per_tip=data.get("mm_per_tip", 0.2),
            metadata=data.get("metadata", {}),
            created_at=datetime.fromisoformat(data["created_at"]) if "created_at" in data else datetime.now(),
        )


@dataclass
class RainfallHistogram:
    data: Dict[str, int]
    bucket_seconds: int
    start_timestamp: int
    num_buckets: int


@dataclass
class WeatherReading:
    temperature: Optional[float] = None  # Celsius
    pressure: Optional[float] = None  # hPa
    humidity: Optional[float] = None  # 0-100%
    precipitation_mm: Optional[float] = None
    rain: Optional[RainfallHistogram] = None


@dataclass
class TelemetryData:
    timestamp: int
    reading: WeatherReading

    def to_api_payload(self) -> Dict[str, Any]:
        dat: Dict[str, Any] = {}
        r = self.reading
        if r.temperature is not None:
            dat["tmp"] = r.temperature
        if r.pressure is not None:
            dat["prs"] = r.pressure
        if r.humidity is not None:
            dat["hum"] = r.humidity
        if r.precipitation_mm is not None:
            dat["mmpt"] = r.precipitation_mm
        if r.rain:
            dat["rain"] = {
                "dat": r.rain.data,
                "sec": r.rain.bucket_seconds,
                "sts": r.rain.start_timestamp,
                "n": r.rain.num_buckets,
            }
        return {"ts": self.timestamp, "dat": dat}