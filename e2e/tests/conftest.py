import json
import os
import subprocess
import sys

import pytest

from tests.api_client import ApiClient, create_api_client
from tests.models import DeviceProvisioning, RegistrationResult


@pytest.fixture
def run_cli():
    """Runs hw cli using active python interpreter."""
    def _run(args: list[str]):
        return subprocess.run(
            [sys.executable, "-m", "hw_cli"] + args,
            capture_output=True,
            text=True,
        )
    return _run


def find_provisioning_key() -> str:
    env_key = os.getenv("WS_PROVISIONING_KEY")
    if env_key and os.path.exists(env_key):
        return env_key

    current_dir = os.path.dirname(os.path.abspath(__file__))
    search_paths = [
        os.path.join(current_dir, "..", "keys", "provisioning-private.pem"),
        os.path.join(current_dir, "..", "..", "keys", "provisioning-private.pem"),
        os.path.join(current_dir, "keys", "provisioning-private.pem"),
    ]
    for p in search_paths:
        if os.path.exists(p):
            return os.path.abspath(p)

    raise FileNotFoundError("Could not find provisioning-private.pem.")


@pytest.fixture
def generate_device(run_cli):

    def _make_device():
        key_path = find_provisioning_key()
        result = run_cli(["-o", "json", "devices", "generate", "--count", "1", "-k", key_path])
        return DeviceProvisioning.from_dict(json.loads(result.stdout)[0])

    yield _make_device


@pytest.fixture(scope="session")
def api_client() -> ApiClient:
    return create_api_client()


@pytest.fixture
def registered_device(run_cli, generate_device) -> tuple[DeviceProvisioning, RegistrationResult]:
    device = generate_device()
    gateway_url = os.environ["GATEWAY_URL"]

    add_result = run_cli(["devices", "add", "-u", gateway_url, "-t", device.provisioning_jwt])
    assert add_result.returncode == 0, f"devices add failed: {add_result.stderr}"

    reg_result = run_cli(["-o", "json", "devices", "register", device.device_id])
    assert reg_result.returncode == 0, f"devices register failed: {reg_result.stderr}"

    registration = RegistrationResult.from_dict(json.loads(reg_result.stdout))
    return device, registration