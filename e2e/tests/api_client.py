import os

import httpx


class ApiClient:
    """Thin wrapper around the API server"""

    def __init__(self, base_url: str, api_key: str):
        self._base_url = base_url.rstrip("/")
        self._headers = {"X-Api-Key": api_key}

    def claim_device(self, device_id: str, claim_code: str, key: str) -> dict:
        url = f"{self._base_url}/api/v1/devices/{device_id}/claim"
        payload = {"claimCode": claim_code, "key": key}

        with httpx.Client(timeout=15.0) as client:
            response = client.post(url, json=payload, headers=self._headers)
            response.raise_for_status()
            return response.json()

    def get_latest_measurement(self, device_id: str) -> dict:
        url = f"{self._base_url}/api/v1/devices/{device_id}/measurements/latest"

        with httpx.Client(timeout=15.0) as client:
            response = client.get(url, headers=self._headers)
            response.raise_for_status()
            return response.json()


def create_api_client() -> ApiClient:
    return ApiClient(
        base_url=os.environ["API_SERVER_URL"],
        api_key=os.environ["ADMIN_API_KEY"],
    )
