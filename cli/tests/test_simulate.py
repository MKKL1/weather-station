# tests/test_simulate.py
import pytest
from types import SimpleNamespace
from ws_cli.__main__ import app
from ws_cli.core.models.models import Device, AuthConfig, DPSConfig, AuthType, WeatherData, Histogram, DeviceInfo

# Reusable dummy device for testing
dummy_device = Device(
    device_id="sim-001",
    auth=AuthConfig(type=AuthType.SYMMETRIC_KEY, symmetric_key="abc123"),
    dps_config=DPSConfig(id_scope="scope-xyz", registration_id="sim-001")
)

# A fake telemetry object for the mock generator
fake_telemetry = WeatherData(
    created_at=1234567890,
    temperature=21.5,
    pressure=1012.5,
    humidity=55.2,
    tips=Histogram(data=b'\x00\x00', count=16, interval_duration=120, start_time=1234560000),
    info=DeviceInfo(id="sim-001", mm_per_tip=0.2, instance_id=1234)
)


class MockPublisher:
    """A mock publisher to intercept calls and simulate success or failure."""

    def __init__(self, should_fail=False, should_timeout=False):
        self.connect_called = 0
        self.send_called_with = None
        self.disconnect_called = 0
        self.should_fail = should_fail
        self.should_timeout = should_timeout
        self.invalidate_called = 0

    async def connect(self):
        self.connect_called += 1
        if self.should_timeout: raise TimeoutError("Connection timed out")
        if self.should_fail: raise ConnectionError("Failed to connect")

    async def send(self, data):
        self.send_called_with = data
        if self.should_timeout: raise TimeoutError("Send timed out")
        if self.should_fail: raise ValueError("Failed to send")

    async def disconnect(self):
        self.disconnect_called += 1

    async def invalidate_cache_and_reconnect(self):
        self.invalidate_called += 1
        await self.connect()


@pytest.fixture
def mock_simulation_deps(monkeypatch, mock_storage):
    """Mocks all dependencies for the 'simulate' commands."""
    # 1. Setup mock storage with a default device
    mock_storage.section("devices").set_item("sim-001", dummy_device.to_dict())
    mock_storage.set("default_device", "sim-001")

    # 2. Mock the data generator to return predictable data
    monkeypatch.setattr(
        "ws_cli.commands.simulate.SimulatedDataGenerator.generate",
        lambda self, device: fake_telemetry
    )

    # 3. Create a controllable mock publisher instance
    mock_pub = MockPublisher()

    # 4. Mock the publisher factory to always return our mock publisher
    monkeypatch.setattr(
        "ws_cli.commands.simulate.create_publisher",
        lambda device, dry_run, force_provision=False, progress=None, task_id=None: mock_pub
    )

    return SimpleNamespace(
        storage=mock_storage,
        publisher=mock_pub
    )


def test_simulate_once_happy_path(runner, mock_simulation_deps):
    result = runner.invoke(app, ["simulate", "once"])

    assert result.exit_code == 0
    assert "Telemetry sent successfully" in result.stdout
    assert mock_simulation_deps.publisher.connect_called == 1
    assert mock_simulation_deps.publisher.send_called_with == fake_telemetry
    assert mock_simulation_deps.publisher.disconnect_called == 1


def test_simulate_once_dry_run(runner, mock_simulation_deps):
    result = runner.invoke(app, ["simulate", "once", "--dry-run"])

    assert result.exit_code == 0
    assert "DRY RUN MODE" in result.stdout
    # The factory should still be called, returning our mock
    assert mock_simulation_deps.publisher.connect_called == 1
    assert mock_simulation_deps.publisher.send_called_with is not None


def test_simulate_once_no_default_device(runner, mock_storage):
    # mock_storage is empty, so no devices are configured
    result = runner.invoke(app, ["simulate", "once"])
    assert result.exit_code == 1
    assert "No default device is set" in result.stdout


def test_simulate_once_device_not_found(runner, mock_simulation_deps):
    result = runner.invoke(app, ["simulate", "once", "--device-id", "not-found"])
    assert result.exit_code == 1
    assert "Device 'not-found' not found" in result.stdout


def test_simulate_once_connection_fails(runner, mock_simulation_deps):
    mock_simulation_deps.publisher.should_fail = True
    result = runner.invoke(app, ["simulate", "once"])
    assert result.exit_code == 1
    assert "Failed to send telemetry" in result.stdout
    assert "Failed to connect" in result.stdout  # The specific error from the mock


def test_simulate_once_send_fails_with_retry(runner, mock_simulation_deps):
    # Make the first send fail, but the reconnect/retry succeed
    mock_simulation_deps.publisher.should_fail = True
    result = runner.invoke(app, ["simulate", "once"])

    assert result.exit_code == 1  # The command still fails overall
    assert "Transmission failed" in result.stdout
    assert "attempting cache invalidation and retry" in result.stdout

    # Check that the invalidation logic was called
    assert mock_simulation_deps.publisher.invalidate_called == 1


def test_simulate_keyboard_interrupt(runner, monkeypatch, mock_simulation_deps):
    """Test that KeyboardInterrupt is handled gracefully."""

    def mock_raiser(*args, **kwargs):
        raise KeyboardInterrupt

    monkeypatch.setattr("ws_cli.commands.simulate.asyncio.run", mock_raiser)
    result = runner.invoke(app, ["simulate", "once"])

    assert result.exit_code == 130  # Standard exit code for Ctrl+C
    assert "Operation cancelled by user" in result.stdout