import json
from pathlib import Path

import pytest
from cryptography.hazmat.primitives import serialization
from cryptography.hazmat.primitives.asymmetric import rsa
from typer.testing import CliRunner

from hw_cli.__main__ import app
from hw_cli.core.provisioning import DeviceProvisioningService, ProvisioningConfig


@pytest.fixture
def test_private_key_pem() -> bytes:
    private_key = rsa.generate_private_key(
        public_exponent=65537,
        key_size=2048,
    )
    return private_key.private_bytes(
        encoding=serialization.Encoding.PEM,
        format=serialization.PrivateFormat.PKCS8,
        encryption_algorithm=serialization.NoEncryption(),
    )


@pytest.fixture
def runner(monkeypatch):
    monkeypatch.setenv("NO_COLOR", "1")
    return CliRunner()


def test_provisioning_service_single_device(test_private_key_pem):
    service = DeviceProvisioningService()
    private_key = serialization.load_pem_private_key(test_private_key_pem, password=None)
    
    device = service.generate_device(private_key)
    
    assert "device_id" in device
    assert "provisioning_jwt" in device
    assert "claim_words" in device
    assert "claim_url" in device
    
    assert device["device_id"].startswith("H1-")
    assert len(device["claim_words"].split()) == 12
    assert "id=" in device["claim_url"]
    assert "k=" in device["claim_url"]


def test_provisioning_service_multiple_devices(test_private_key_pem):
    service = DeviceProvisioningService()
    devices = service.generate_multiple(test_private_key_pem, count=2)
    
    assert len(devices) == 2
    assert devices[0]["device_id"] != devices[1]["device_id"]


def test_provisioning_service_overflow_errors(test_private_key_pem):
    bad_config = ProvisioningConfig(project_start_epoch=-100000000000)
    service = DeviceProvisioningService(bad_config)
    private_key = serialization.load_pem_private_key(test_private_key_pem, password=None)
    
    with pytest.raises(OverflowError, match="Month counter overflow"):
        service.generate_device(private_key)


def test_cli_generate_to_stdout(runner, tmp_path, test_private_key_pem):
    key_file = tmp_path / "private.pem"
    key_file.write_bytes(test_private_key_pem)
    
    result = runner.invoke(
        app,
        ["devices", "generate", "--key", str(key_file), "--count", "2"]
    )
    
    assert result.exit_code == 0
    assert "Generated 2 devices" in result.stdout
    
    json_str = result.stdout[:result.stdout.rfind("]") + 1]
    devices = json.loads(json_str)
    assert len(devices) == 2
    assert devices[0]["device_id"] is not None


def test_cli_generate_to_file(runner, tmp_path, test_private_key_pem):
    key_file = tmp_path / "private.pem"
    key_file.write_bytes(test_private_key_pem)
    out_file = tmp_path / "devices.json"
    
    result = runner.invoke(
        app,
        ["devices", "generate", "--key", str(key_file), "--count", "1", "--out", str(out_file)]
    )
    
    assert result.exit_code == 0
    assert "Wrote 1 devices to" in result.stdout
    
    assert out_file.exists()
    devices = json.loads(out_file.read_text(encoding="utf-8"))
    assert len(devices) == 1
    assert "device_id" in devices[0]
