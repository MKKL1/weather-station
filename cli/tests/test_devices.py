# tests/test_devices.py
import pytest
from ws_cli.__main__ import app
from ws_cli.core.models.models import Device, AuthConfig, DPSConfig, AuthType
from ws_cli.core.device_manager import DeviceManager
from datetime import datetime

# A reusable dummy device for testing
dummy_device_1 = Device(
    device_id="sim-001",
    auth=AuthConfig(type=AuthType.SYMMETRIC_KEY, symmetric_key="abc123"),
    dps_config=DPSConfig(id_scope="scope-xyz", registration_id="sim-001"),
    created_at=datetime.now()
)
dummy_device_2 = Device(
    device_id="sim-002",
    auth=AuthConfig(type=AuthType.SYMMETRIC_KEY, symmetric_key="def456"),
    dps_config=DPSConfig(id_scope="scope-xyz", registration_id="sim-002"),
    created_at=datetime.now()
)


def test_devices_list_empty(runner, mock_storage):
    result = runner.invoke(app, ["devices", "list"])
    assert result.exit_code == 0
    assert "No devices configured" in result.stdout


def test_devices_list_with_devices(runner, mock_storage):
    # Setup: Directly manipulate the mock storage
    devices_section = mock_storage.section("devices")
    devices_section.set_item("sim-001", dummy_device_1.to_dict())
    devices_section.set_item("sim-002", dummy_device_2.to_dict())
    mock_storage.set("default_device", "sim-002")

    result = runner.invoke(app, ["devices", "list"])
    assert result.exit_code == 0
    assert "sim-001" in result.stdout
    assert "sim-002" in result.stdout
    # Check for the default marker
    assert result.stdout.count("✓") == 1
    assert "sim-002" in [line for line in result.stdout.splitlines() if "✓" in line][0]


def test_devices_add_symmetric_key(runner, mock_storage):
    result = runner.invoke(app, [
        "devices", "add", "sim-001",
        "--auth-type", "symmetric_key",
        "--primary-key", "abc123",
        "--dps-id-scope", "scope-xyz",
        "--set-default"
    ])
    assert result.exit_code == 0
    assert "Device 'sim-001' added successfully" in result.stdout
    assert "Set 'sim-001' as default device" in result.stdout

    # Verify: Check the storage directly
    assert mock_storage.section("devices").get_item("sim-001") is not None
    assert mock_storage.get("default_device") == "sim-001"


def test_devices_add_x509(runner, mock_storage, tmp_path):
    cert_file = tmp_path / "cert.pem"
    key_file = tmp_path / "key.pem"
    cert_file.touch()
    key_file.touch()

    result = runner.invoke(app, [
        "devices", "add", "x509-dev",
        "--auth-type", "x509",
        "--cert-file", str(cert_file),
        "--key-file", str(key_file),
        "--dps-id-scope", "scope-xyz",
    ])

    assert result.exit_code == 0
    assert "Device 'x509-dev' added successfully" in result.stdout

    # Verify
    added_device_data = mock_storage.section("devices").get_item("x509-dev")
    assert added_device_data is not None
    assert added_device_data["auth"]["type"] == "x509"
    assert added_device_data["auth"]["cert_file"] == str(cert_file)


def test_devices_add_fails_on_missing_params(runner, mock_storage):
    result = runner.invoke(app, [
        "devices", "add", "sim-003",
        "--auth-type", "symmetric_key",
        # Missing --primary-key
        "--dps-id-scope", "scope-xyz",
    ])
    assert result.exit_code == 1
    assert "Symmetric key authentication requires --primary-key" in result.stdout


def test_devices_add_existing_device(runner, mock_storage):
    mock_storage.section("devices").set_item("sim-001", dummy_device_1.to_dict())

    # Simulate user answering "n" to the overwrite prompt
    result = runner.invoke(app, [
        "devices", "add", "sim-001",
        "--auth-type", "symmetric_key", "--primary-key", "abc123", "--dps-id-scope", "scope-xyz",
    ], input="n\n")

    assert result.exit_code == 1
    assert "Device 'sim-001' already exists" in result.stdout


def test_devices_remove_by_id_force(runner, mock_storage):
    mock_storage.section("devices").set_item("sim-001", dummy_device_1.to_dict())

    result = runner.invoke(app, ["devices", "remove", "sim-001", "--force"])

    assert result.exit_code == 0
    assert "Device 'sim-001' removed" in result.stdout
    assert mock_storage.section("devices").get_item("sim-001") is None


def test_devices_remove_by_index_with_prompt(runner, mock_storage):
    # The order is not guaranteed with dicts, so we use the real DeviceManager to list them first
    device_manager = DeviceManager()
    device_manager.add_device(dummy_device_1)
    device_manager.add_device(dummy_device_2)

    # Find the device at index 1
    devices = device_manager.get_devices()
    device_to_remove = devices[1]

    result = runner.invoke(app, ["devices", "remove", "1"], input="y\n")

    assert result.exit_code == 0
    assert f"Device '{device_to_remove.device_id}' removed" in result.stdout
    assert mock_storage.section("devices").get_item(device_to_remove.device_id) is None


def test_devices_remove_not_found(runner, mock_storage):
    result = runner.invoke(app, ["devices", "remove", "sim-999", "--force"])
    assert result.exit_code == 1
    assert "Device 'sim-999' not found" in result.stdout


def test_devices_set_default_by_id(runner, mock_storage):
    mock_storage.section("devices").set_item("sim-001", dummy_device_1.to_dict())
    mock_storage.section("devices").set_item("sim-002", dummy_device_2.to_dict())

    result = runner.invoke(app, ["devices", "set-default", "sim-002"])

    assert result.exit_code == 0
    assert "Set 'sim-002' as default device" in result.stdout
    assert mock_storage.get("default_device") == "sim-002"


def test_devices_show_by_id(runner, mock_storage):
    mock_storage.section("devices").set_item("sim-001", dummy_device_1.to_dict())

    result = runner.invoke(app, ["devices", "show", "sim-001"])

    assert result.exit_code == 0
    assert "Device: sim-001" in result.stdout
    assert "Authentication Type:" in result.stdout
    assert "symmetric_key" in result.stdout
    assert "scope-xyz" in result.stdout