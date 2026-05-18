import base64
import os
import struct
import time
import urllib.parse
from dataclasses import dataclass
from typing import Any, Dict, List, Optional

from cryptography.hazmat.primitives import serialization
from mnemonic import Mnemonic
import jwt as pyjwt


@dataclass
class ProvisioningConfig:
    product_code: str = "H"
    version: str = "1"
    machine_id: int = 1
    project_start_epoch: int = 1735689600
    jwt_issuer: str = "weather-station/provisioning"
    jwt_kid: str = "provisioning-access-token"
    jwt_audience: str = "provisioning-api"
    claim_base_url: str = "https://setup.weather-app.local/claim"


class DeviceProvisioningService:
    def __init__(self, config: Optional[ProvisioningConfig] = None):
        self.config = config or ProvisioningConfig()
        self.mnemo = Mnemonic("english")

    def generate_device(self, private_key: Any) -> Dict[str, str]:
        months = int((time.time() - self.config.project_start_epoch) // (30 * 24 * 3600))
        if months > 2047:
            raise OverflowError("Month counter overflow — update PROJECT_START_EPOCH")
        if months < 0:
            raise ValueError(
                "Current time is prior to PROJECT_START_EPOCH. Verify epoch or system time."
            )

        meta_val = (months << 5) | (self.config.machine_id & 0x1F)
        meta = struct.pack(">H", meta_val)

        entropy = meta + os.urandom(14)
        words = self.mnemo.to_mnemonic(entropy)
        seed = self.mnemo.to_seed(words)

        prefix = base64.b32encode(meta).decode().rstrip("=")
        suffix = base64.b32encode(seed[:12]).decode().rstrip("=")
        device_id = f"{self.config.product_code}{self.config.version}-{prefix}{suffix}"

        token = pyjwt.encode(
            {
                "aud": self.config.jwt_audience,
                "sub": device_id,
                "iss": self.config.jwt_issuer,
                "typ": "provisioning",
            },
            private_key,
            algorithm="RS256",
            headers={"alg": "RS256", "typ": "JWT", "kid": self.config.jwt_kid},
        )

        seed_b64 = base64.urlsafe_b64encode(seed).rstrip(b"=").decode()
        query_params = {"id": device_id, "k": seed_b64}
        claim_url = f"{self.config.claim_base_url}?{urllib.parse.urlencode(query_params)}"

        return {
            "device_id": device_id,
            "provisioning_jwt": token,
            "claim_words": words,
            "claim_url": claim_url,
        }

    def generate_multiple(
        self, private_key_pem: bytes, count: int
    ) -> List[Dict[str, str]]:
        if count < 1:
            raise ValueError("Count must be at least 1")

        private_key = serialization.load_pem_private_key(private_key_pem, password=None)
        return [self.generate_device(private_key) for _ in range(count)]
