from enum import Enum
from typing import Optional, Dict, Any
from dataclasses import dataclass, field
from datetime import datetime

class AuthType(str, Enum):
    """Authentication type enumeration."""
    X509 = "x509"
    SYMMETRIC_KEY = "symmetric_key"


@dataclass
class AuthConfig:
    """Authentication configuration."""
    type: AuthType
    cert_file: Optional[str] = None
    key_file: Optional[str] = None
    key_passphrase: Optional[str] = None
    symmetric_key: Optional[str] = None  # Base64 encoded

    def validate(self) -> bool:
        """Validate authentication configuration."""
        if self.type == AuthType.X509:
            return bool(self.cert_file and self.key_file)
        elif self.type == AuthType.SYMMETRIC_KEY:
            return bool(self.symmetric_key)
        return False


@dataclass
class DPSConfig:
    """Device Provisioning Service configuration."""
    id_scope: str
    provisioning_host: str = "global.azure-devices-provisioning.net"
    registration_id: Optional[str] = None


@dataclass
class Device:
    """Device configuration model."""
    device_id: str
    auth: AuthConfig
    dps_config: Optional[DPSConfig] = None
    mm_per_tip: float = 0.2
    metadata: Dict[str, Any] = field(default_factory=dict)
    created_at: datetime = field(default_factory=datetime.now)

    def to_dict(self) -> Dict[str, Any]:
        """Convert to dictionary for serialization."""
        return {
            "device_id": self.device_id,
            "auth": {
                "type": self.auth.type.value,
                "cert_file": self.auth.cert_file,
                "key_file": self.auth.key_file,
                "key_passphrase": self.auth.key_passphrase,
                "symmetric_key": self.auth.symmetric_key,
            },
            "dps_config": {
                "id_scope": self.dps_config.id_scope,
                "provisioning_host": self.dps_config.provisioning_host,
                "registration_id": self.dps_config.registration_id,
            } if self.dps_config else None,
            "mm_per_tip": self.mm_per_tip,
            "metadata": self.metadata,
            "created_at": self.created_at.isoformat(),
        }

    @classmethod
    def from_dict(cls, data: Dict[str, Any]) -> "Device":
        """Create from dictionary."""
        auth_data = data["auth"]
        auth = AuthConfig(
            type=AuthType(auth_data["type"]),
            cert_file=auth_data.get("cert_file"),
            key_file=auth_data.get("key_file"),
            key_passphrase=auth_data.get("key_passphrase"),
            symmetric_key=auth_data.get("symmetric_key"),
        )

        dps_config = None
        if data.get("dps_config"):
            dps_data = data["dps_config"]
            dps_config = DPSConfig(
                id_scope=dps_data["id_scope"],
                provisioning_host=dps_data.get("provisioning_host", "global.azure-devices-provisioning.net"),
                registration_id=dps_data.get("registration_id"),
            )

        return cls(
            device_id=data["device_id"],
            auth=auth,
            dps_config=dps_config,
            mm_per_tip=data.get("mm_per_tip", 0.2),
            metadata=data.get("metadata", {}),
            created_at=datetime.fromisoformat(data["created_at"]) if "created_at" in data else datetime.now(),
        )


@dataclass
class WeatherData:
    """Weather telemetry data."""
    timestamp: datetime
    temperature: float  # Celsius
    humidity: float  # Percentage
    pressure: float  # hPa
    rain_tips: int  # Number of rain gauge tips
    device_id: str

    def to_dict(self) -> Dict[str, Any]:
        """Convert to dictionary."""
        return {
            "timestamp": self.timestamp.isoformat(),
            "temperature": self.temperature,
            "humidity": self.humidity,
            "pressure": self.pressure,
            "rain_tips": self.rain_tips,
            "device_id": self.device_id,
        }


@dataclass
class SimulationConfig:
    """Simulation configuration."""
    interval_seconds: int = 1800
    jitter_seconds: float = 5.0
    max_messages: Optional[int] = None
    seed: Optional[int] = None
    dry_run: bool = False