# tests/test_cache.py
import pytest
import time
from types import SimpleNamespace
from ws_cli.__main__ import app
from tests.test_devices import MockDeviceManager, dummy_device_1, dummy_device_2, patch_console_utils


class MockProvisioningCache:
    def __init__(self, cache_data=None):
        self._cache = cache_data or {}
        self.cleared = False
        self.invalidated_device = None

    def _load_cache(self):
        return self._cache

    def clear_cache(self):
        self.cleared = True
        self._cache = {}

    def invalidate_device(self, device):
        self.invalidated_device = device.device_id
        cache_key = self._get_cache_key(device)
        if cache_key in self._cache:
            del self._cache[cache_key]

    def _get_cache_key(self, device):
        return f"{device.dps_config.id_scope}-{device.device_id}"

    @property
    def _cache_file(self):
        return "/fake/path/to/dps_cache.json"


@pytest.fixture
def mock_cache_and_deps(monkeypatch):
    """Fixture to mock ProvisioningCache and its dependencies."""
    mocks = SimpleNamespace()

    # Setup some cache data
    now = time.time()
    mocks.cache_data = {
        "scope-xyz-sim-001": {
            "device_id": "sim-001",
            "cached_at": now - 1000,
            "ttl": 3600,
            "identity": {
                "assigned_hub": "hub-1.azure-devices.net",
                "auth_info": {"type": "symmetric_key"}
            }
        },
        "scope-xyz-sim-002": {
            "device_id": "sim-002",
            "cached_at": now - 4000,  # Expired
            "ttl": 3600,
            "identity": {
                "assigned_hub": "hub-2.azure-devices.net",
                "auth_info": {"type": "symmetric_key"}
            }
        },
        "scope-xyz-orphan-003": {  # Orphaned entry
            "device_id": "orphan-003",
            "cached_at": now - 100,
            "ttl": 3600,
            "identity": {
                "assigned_hub": "hub-3.azure-devices.net",
                "auth_info": {"type": "symmetric_key"}
            }
        }
    }

    mocks.mock_cache = MockProvisioningCache(cache_data=mocks.cache_data)
    mocks.mock_devices = MockDeviceManager(devices=[dummy_device_1, dummy_device_2])

    monkeypatch.setattr("ws_cli.commands.cache.ProvisioningCache", lambda config_manager: mocks.mock_cache)
    monkeypatch.setattr("ws_cli.commands.cache.DeviceManager", lambda: mocks.mock_devices)
    patch_console_utils(monkeypatch)


def test_cache_show(runner, mock_cache_and_deps):
    result = runner.invoke(app, ["cache", "show"])
    assert result.exit_code == 0
    assert "DPS Cache Entries" in result.stdout
    assert "sim-001" in result.stdout
    assert "sim-002" in result.stdout
    assert "Valid" in result.stdout
    assert "Expired" in result.stdout
    assert "Cache file: /fake/path/to/dps_cache.json" in result.stdout


def test_cache_show_empty(runner):
    mock_cache = MockProvisioningCache(cache_data={})
    mock_devices = MockDeviceManager(devices=[dummy_device_1, dummy_device_2])
    monkeypatch = pytest.MonkeyPatch()
    monkeypatch.setattr("ws_cli.commands.cache.ProvisioningCache", lambda config_manager: mock_cache)
    monkeypatch.setattr("ws_cli.commands.cache.DeviceManager", lambda: mock_devices)
    patch_console_utils(monkeypatch)
    result = runner.invoke(app, ["cache", "show"])
    assert result.exit_code == 0
    assert "No DPS cache entries found" in result.stdout


def test_cache_clear_all_force(runner):
    mock_cache = MockProvisioningCache()
    monkeypatch = pytest.MonkeyPatch()
    monkeypatch.setattr("ws_cli.commands.cache.ProvisioningCache", lambda config_manager: mock_cache)
    patch_console_utils(monkeypatch)
    result = runner.invoke(app, ["cache", "clear", "--force"])
    assert result.exit_code == 0
    assert "Cleared all DPS cache entries" in result.stdout
    assert mock_cache.cleared is True


def test_cache_clear_specific_device(runner):
    mock_cache = MockProvisioningCache(cache_data={"scope-xyz-sim-001": {}})
    mock_devices = MockDeviceManager(devices=[dummy_device_1])
    monkeypatch = pytest.MonkeyPatch()
    monkeypatch.setattr("ws_cli.commands.cache.ProvisioningCache", lambda config_manager: mock_cache)
    monkeypatch.setattr("ws_cli.commands.cache.DeviceManager", lambda: mock_devices)
    patch_console_utils(monkeypatch)
    result = runner.invoke(app, ["cache", "clear", "--device-id", "sim-001", "--force"])
    assert result.exit_code == 0
    assert "Cleared DPS cache for device 'sim-001'" in result.stdout
    assert mock_cache.invalidated_device == "sim-001"


def test_cache_clear_device_not_found(runner):
    mock_devices = MockDeviceManager(devices=[])
    monkeypatch = pytest.MonkeyPatch()
    monkeypatch.setattr("ws_cli.commands.cache.DeviceManager", lambda: mock_devices)
    patch_console_utils(monkeypatch)
    result = runner.invoke(app, ["cache", "clear", "--device-id", "not-found", "--force"])
    assert result.exit_code == 1
    assert "Device 'not-found' not found" in result.stdout


def test_cache_validate(runner, mock_cache_and_deps):
    result = runner.invoke(app, ["cache", "validate"])
    assert result.exit_code == 0
    assert "Cache Validation Results" in result.stdout
    assert "Valid entries: 1" in result.stdout
    assert "Expired entries: 1" in result.stdout
    assert "Orphaned entries: 1" in result.stdout
    assert "Expired devices: sim-002" in result.stdout
    assert "Orphaned cache entries: orphan-003" in result.stdout


def test_cache_validate_all_valid(runner):
    now = time.time()
    valid_cache_data = {
        "scope-xyz-sim-001": {
            "device_id": "sim-001",
            "cached_at": now,
            "ttl": 3600,
            "identity": {"assigned_hub": "h1", "auth_info": {"type": "symmetric_key"}}
        }
    }
    mock_cache = MockProvisioningCache(cache_data=valid_cache_data)
    mock_devices = MockDeviceManager(devices=[dummy_device_1])
    monkeypatch = pytest.MonkeyPatch()
    monkeypatch.setattr("ws_cli.commands.cache.ProvisioningCache", lambda config_manager: mock_cache)
    monkeypatch.setattr("ws_cli.commands.cache.DeviceManager", lambda: mock_devices)
    patch_console_utils(monkeypatch)
    result = runner.invoke(app, ["cache", "validate"])
    assert result.exit_code == 0
    assert "All cache entries are valid" in result.stdout