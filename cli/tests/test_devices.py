# tests/test_devices.py
import pytest
from types import SimpleNamespace
from ws_cli.__main__ import app
from ws_cli.core.models.models import Device, AuthConfig, DPSConfig, AuthType
from datetime import datetime

# A reusable dummy device for testing
dummy_device_1 = Device(
    device_id="sim-001",
    auth=AuthConfig(type=AuthType.SYMMETRIC_KEY, symmetric_key="abc123"),
    dps_config=DPSConfig(id_scope="scope-xyz", registration_id="sim-001", provisioning_host="global.azure-devices-provisioning.net"),
    created_at=datetime.now()
)
dummy_device_2 = Device(
    device_id="sim-002",
    auth=AuthConfig(type=AuthType.SYMMETRIC_KEY, symmetric_key="def456"),
    dps_config=DPSConfig(id_scope="scope-xyz", registration_id="sim-002", provisioning_host="global.azure-devices-provisioning.net"),
    created_at=datetime.now()
)


class MockDeviceManager:
    def __init__(self, devices=None, default_device_id=None):
        self._devices = {d.device_id: d for d in devices} if devices else {}
        self._default_device_id = default_device_id
        self.added_device = None
        self.removed_device_id = None
        self.default_set_to = None

    def get_devices(self):
        return list(self._devices.values())

    def get_device(self, device_id):
        return self._devices.get(device_id)

    def get_default_device_id(self):
        return self._default_device_id

    def device_exists(self, device_id):
        return device_id in self._devices

    def add_device(self, device):
        self.added_device = device
        self._devices[device.device_id] = device

    def set_default_device(self, device_id):
        self.default_set_to = device_id
        self._default_device_id = device_id

    def remove_device(self, device_id):
        self.removed_device_id = device_id
        if device_id in self._devices:
            del self._devices[device_id]
        if self._default_device_id == device_id:
            self._default_device_id = None

    def get_config_path(self):
        return "/fake/path/to/config.json"


@pytest.fixture
def mock_device_manager(monkeypatch):
    """Fixture to mock the DeviceManager with a controllable instance."""
    mock_manager = MockDeviceManager(devices=[], default_device_id=None)
    monkeypatch.setattr("ws_cli.commands.devices.DeviceManager", lambda: mock_manager)
    # Also mock for cache commands that might use it
    monkeypatch.setattr("ws_cli.commands.cache.DeviceManager", lambda: mock_manager)
    return mock_manager


def patch_console_utils(monkeypatch):
    """Patch all console printing functions to avoid rich formatting issues."""
    monkeypatch.setattr("ws_cli.utils.console.print_success", print)
    monkeypatch.setattr("ws_cli.utils.console.print_info", print)
    monkeypatch.setattr("ws_cli.utils.console.print_warning", print)
    monkeypatch.setattr("ws_cli.utils.console.print_error", print)
    # The failing patch for rich.print has been removed.


def test_devices_list_empty(runner, mock_device_manager, monkeypatch):
    patch_console_utils(monkeypatch)
    result = runner.invoke(app, ["devices", "list"])
    assert result.exit_code == 0
    assert "No devices configured" in result.stdout


def test_devices_list_with_devices(runner, mock_device_manager, monkeypatch):
    patch_console_utils(monkeypatch)
    mock_device_manager._devices = {"sim-001": dummy_device_1, "sim-002": dummy_device_2}
    mock_device_manager._default_device_id = "sim-002"

    result = runner.invoke(app, ["devices", "list"])
    assert result.exit_code == 0
    assert "sim-001" in result.stdout
    assert "sim-002" in result.stdout
    # Check for the default marker
    assert result.stdout.count("✓") == 1


def test_devices_add_symmetric_key(runner, mock_device_manager, monkeypatch):
    patch_console_utils(monkeypatch)
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
    assert mock_device_manager.added_device is not None
    assert mock_device_manager.added_device.device_id == "sim-001"
    assert mock_device_manager.added_device.auth.type == AuthType.SYMMETRIC_KEY
    assert mock_device_manager.default_set_to == "sim-001"


def test_devices_add_x509(runner, mock_device_manager, monkeypatch, tmp_path):
    patch_console_utils(monkeypatch)
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
    assert mock_device_manager.added_device is not None
    assert mock_device_manager.added_device.auth.type == AuthType.X509
    assert mock_device_manager.added_device.auth.cert_file == str(cert_file)


def test_devices_add_fails_on_missing_params(runner, mock_device_manager, monkeypatch):
    patch_console_utils(monkeypatch)
    result = runner.invoke(app, [
        "devices", "add", "sim-003",
        "--auth-type", "symmetric_key",
        # Missing --primary-key
        "--dps-id-scope", "scope-xyz",
    ])
    assert result.exit_code == 1
    assert "Symmetric key authentication requires --primary-key" in result.stdout


def test_devices_add_existing_device(runner, mock_device_manager, monkeypatch):
    patch_console_utils(monkeypatch)
    mock_device_manager._devices = {"sim-001": dummy_device_1}

    result = runner.invoke(app, [
        "devices", "add", "sim-001",
        "--auth-type", "symmetric_key",
        "--primary-key", "abc123",
        "--dps-id-scope", "scope-xyz",
    ], input="n\n") # Do not update

    assert result.exit_code == 1
    assert "Device 'sim-001' already exists" in result.stdout


def test_devices_remove_by_id_force(runner, mock_device_manager, monkeypatch):
    patch_console_utils(monkeypatch)
    mock_device_manager._devices = {"sim-001": dummy_device_1}
    result = runner.invoke(app, ["devices", "remove", "sim-001", "--force"])
    assert result.exit_code == 0
    assert "Device 'sim-001' removed" in result.stdout
    assert mock_device_manager.removed_device_id == "sim-001"


def test_devices_remove_by_index_with_prompt(runner, mock_device_manager, monkeypatch):
    patch_console_utils(monkeypatch)
    mock_device_manager._devices = {"sim-001": dummy_device_1, "sim-002": dummy_device_2}
    result = runner.invoke(app, ["devices", "remove", "1"], input="y\n") # Corresponds to sim-002
    assert result.exit_code == 0
    assert "Device 'sim-002' removed" in result.stdout
    assert mock_device_manager.removed_device_id == "sim-002"


def test_devices_remove_cancelled(runner, mock_device_manager, monkeypatch):
    patch_console_utils(monkeypatch)
    mock_device_manager._devices = {"sim-001": dummy_device_1}
    result = runner.invoke(app, ["devices", "remove", "sim-001"], input="n\n")
    assert result.exit_code == 0
    assert "Cancelled" in result.stdout
    assert mock_device_manager.removed_device_id is None


def test_devices_remove_not_found(runner, mock_device_manager, monkeypatch):
    patch_console_utils(monkeypatch)
    result = runner.invoke(app, ["devices", "remove", "sim-999", "--force"])
    assert result.exit_code == 1
    assert "Device 'sim-999' not found" in result.stdout


def test_devices_set_default_by_id(runner, mock_device_manager, monkeypatch):
    patch_console_utils(monkeypatch)
    mock_device_manager._devices = {"sim-001": dummy_device_1, "sim-002": dummy_device_2}
    result = runner.invoke(app, ["devices", "set-default", "sim-002"])
    assert result.exit_code == 0
    assert "Set 'sim-002' as default device" in result.stdout
    assert mock_device_manager.default_set_to == "sim-002"


def test_devices_set_default_by_index(runner, mock_device_manager, monkeypatch):
    patch_console_utils(monkeypatch)
    mock_device_manager._devices = {"sim-001": dummy_device_1, "sim-002": dummy_device_2}
    result = runner.invoke(app, ["devices", "set-default", "0"])
    assert result.exit_code == 0
    assert "Set 'sim-001' as default device" in result.stdout
    assert mock_device_manager.default_set_to == "sim-001"


def test_devices_set_default_invalid_index(runner, mock_device_manager, monkeypatch):
    patch_console_utils(monkeypatch)
    mock_device_manager._devices = {"sim-001": dummy_device_1}
    result = runner.invoke(app, ["devices", "set-default", "5"])
    assert result.exit_code == 1
    assert "Invalid device index: 5" in result.stdout


def test_devices_show_by_id(runner, mock_device_manager, monkeypatch):
    patch_console_utils(monkeypatch)
    mock_device_manager._devices = {"sim-001": dummy_device_1}
    result = runner.invoke(app, ["devices", "show", "sim-001"])
    assert result.exit_code == 0
    assert "Device: sim-001" in result.stdout
    assert "Authentication Type:" in result.stdout
    assert "symmetric_key" in result.stdout
    assert "scope-xyz" in result.stdout


def test_devices_show_by_index(runner, mock_device_manager, monkeypatch):
    patch_console_utils(monkeypatch)
    mock_device_manager._devices = {"sim-001": dummy_device_1}
    result = runner.invoke(app, ["devices", "show", "0"])
    assert result.exit_code == 0
    assert "Device: sim-001" in result.stdout


def test_devices_show_not_found(runner, mock_device_manager, monkeypatch):
    patch_console_utils(monkeypatch)
    result = runner.invoke(app, ["devices", "show", "sim-999"])
    assert result.exit_code == 1
    assert "Device 'sim-999' not found" in result.stdout