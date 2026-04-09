import argparse
import base64
import json
import os
import struct
import time
import urllib.parse

from cryptography.hazmat.primitives import serialization
from mnemonic import Mnemonic
import jwt as pyjwt

PRODUCT_CODE        = "H"
VER                 = "1"
MACHINE_ID          = 1
PROJECT_START_EPOCH = 1735689600

JWT_ISSUER   = os.getenv("JWT_ISSUER",   "weather-station/provisioning")
JWT_KID      = os.getenv("JWT_KID",      "provisioning-access-token")
JWT_AUDIENCE = os.getenv("JWT_AUDIENCE", "provisioning-api")
CLAIM_BASE_URL = os.getenv("CLAIM_BASE_URL", "https://setup.weather-app.local/claim")

mnemo = Mnemonic("english")


def generate_device(private_key) -> dict:
    months = int((time.time() - PROJECT_START_EPOCH) // (30 * 24 * 3600))
    if months > 2047:
        raise OverflowError("Month counter overflow — update PROJECT_START_EPOCH")

    meta    = struct.pack(">H", (months << 5) | (MACHINE_ID & 0x1F))
    entropy = meta + os.urandom(14)
    words   = mnemo.to_mnemonic(entropy)
    seed    = mnemo.to_seed(words)

    prefix    = base64.b32encode(meta).decode().rstrip("=")
    suffix    = base64.b32encode(seed[:12]).decode().rstrip("=")
    device_id = f"{PRODUCT_CODE}{VER}-{prefix}{suffix}"

    token = pyjwt.encode(
        {"aud": JWT_AUDIENCE, "sub": device_id, "iss": JWT_ISSUER, "typ": "provisioning"},
        private_key,
        algorithm="RS256",
        headers={"alg": "RS256", "typ": "JWT", "kid": JWT_KID},
    )

    seed_b64  = base64.urlsafe_b64encode(seed).rstrip(b"=").decode()
    claim_url = f"{CLAIM_BASE_URL}?{urllib.parse.urlencode({'id': device_id, 'k': seed_b64})}"

    return {"device_id": device_id, "provisioning_jwt": token, "claim_words": words, "claim_url": claim_url}


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--key", required=True)
    parser.add_argument("--out", required=True)
    parser.add_argument("--count", type=int, default=3)
    args = parser.parse_args()

    with open(args.key, "rb") as f:
        private_key = serialization.load_pem_private_key(f.read(), password=None)

    devices = [generate_device(private_key) for _ in range(args.count)]

    with open(args.out, "w") as f:
        json.dump(devices, f, indent=2)

    print(f"Wrote {len(devices)} devices to {args.out}")


if __name__ == "__main__":
    main()
