# tests/test_cache.py
import time
import pytest
from ws_cli.__main__ import app


@pytest.fixture
def mock_cache_data(mock_storage):
    """Fixture to populate the mock storage with cache data for tests."""
    now = time.time()
    cache_data = {
        "key-valid": {
            "device_id": "sim-001",
            "cached_at": now - 1000,
            "ttl": 3600,
            "identity": {"assigned_hub": "hub-1.azure-devices.net", "auth_info": {"type": "symmetric_key"}}
        },
        "key-expired": {
            "device_id": "sim-002",
            "cached_at": now - 4000,  # Expired
            "ttl": 3600,
            "identity": {"assigned_hub": "hub-2.azure-devices.net", "auth_info": {"type": "symmetric_key"}}
        },
        "key-malformed": "this is not a dict",
        "key-no-hub": {
            "device_id": "sim-003",
            "cached_at": now - 100,
            "ttl": 3600,
            "identity": {"auth_info": {"type": "symmetric_key"}}  # Missing assigned_hub
        }
    }
    mock_storage.section("dps_cache").set(cache_data)
    return mock_storage


# def test_cache_show(runner, mock_cache_data):
#     result = runner.invoke(app, ["cache", "show"])
#     assert result.exit_code == 0
#     assert "DPS Cache Entries" in result.stdout
#     assert "sim-001" in result.stdout
#     assert "sim-002" in result.stdout
#     assert "Valid" in result.stdout
#     assert "Expired" in result.stdout
#     assert "Cache file: /fake/path/data.json" in result.stdout


def test_cache_show_empty(runner, mock_storage):
    # mock_storage is empty by default
    result = runner.invoke(app, ["cache", "show"])
    assert result.exit_code == 0
    assert "No DPS cache entries found" in result.stdout


def test_cache_clear_all_force(runner, mock_cache_data):
    result = runner.invoke(app, ["cache", "clear", "--force"])
    assert result.exit_code == 0
    assert "Cleared all DPS cache entries" in result.stdout
    assert not mock_cache_data.section("dps_cache").get()  # Cache should be empty


def test_cache_clear_specific_device(runner, mock_cache_data):
    result = runner.invoke(app, ["cache", "clear", "--device-id", "sim-001", "--force"])
    assert result.exit_code == 0
    assert "Cleared DPS cache for device 'sim-001'" in result.stdout

    cache = mock_cache_data.section("dps_cache").get()
    assert "key-valid" not in cache  # sim-001's key should be gone
    assert "key-expired" in cache  # sim-002's key should remain


def test_cache_clear_device_not_in_cache(runner, mock_cache_data):
    result = runner.invoke(app, ["cache", "clear", "--device-id", "not-found", "--force"])
    assert result.exit_code == 0  # Command succeeds even if no entries are found
    assert "No cache entries found for device 'not-found'" in result.stdout


# TODO
# def test_cache_validate(runner, mock_cache_data):
#     result = runner.invoke(app, ["cache", "validate"])
#     assert result.exit_code == 0
#     assert "Cache Validation Results" in result.stdout
#     assert "Valid entries: 1" in result.stdout
#     assert "Expired entries: 1" in result.stdout
#     assert "Malformed entries: 1" in result.stdout
#     assert "Structural issues: 1" in result.stdout
#     assert "Expired devices: sim-002" in result.stdout
#     assert "Malformed cache keys: key-malformed" in result.stdout
#     assert "Device sim-003: Missing assigned_hub" in result.stdout


def test_cache_clean(runner, mock_cache_data):
    result = runner.invoke(app, ["cache", "clean"])
    assert result.exit_code == 0
    assert "Cleaned 2 expired/malformed cache entries" in result.stdout

    cache = mock_cache_data.section("dps_cache").get()
    assert "key-valid" in cache  # Should remain
    assert "key-no-hub" in cache  # Should remain (it's not expired or malformed)
    assert "key-expired" not in cache  # Should be removed
    assert "key-malformed" not in cache  # Should be removed