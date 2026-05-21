from dataclasses import dataclass
from typing import Any


@dataclass
class DeviceProvisioning:
    device_id: str
    provisioning_jwt: str
    claim_words: str
    claim_url: str

    @classmethod
    def from_dict(cls, data: dict[str, Any]) -> "DeviceProvisioning":
        return cls(
            device_id=data["device_id"],
            provisioning_jwt=data["provisioning_jwt"],
            claim_words=data["claim_words"],
            claim_url=data["claim_url"],
        )


@dataclass
class RegistrationResult:
    claim_code: str

    @classmethod
    def from_dict(cls, data: dict[str, Any]) -> "RegistrationResult":
        return cls(claim_code=data["claim_code"])


@dataclass
class TelemetrySnapshot:
    reading: dict[str, Any]
    timestamp: int

    @classmethod
    def from_dict(cls, data: dict[str, Any]) -> "TelemetrySnapshot":
        return cls(reading=data["reading"], timestamp=data["timestamp"])