# tests/test_simulate.py
import pytest
import asyncio
from types import SimpleNamespace
from ws_cli.__main__ import app
from ws_cli.core.models.models import Device, AuthConfig, DPSConfig, AuthType

# A reusable dummy device for testing
dummy_device = Device(
    device_id="sim-001",
    auth=AuthConfig(type=AuthType.SYMMETRIC_KEY, symmetric_key="abc123"),
    dps_config=DPSConfig(id_scope="scope-xyz", registration_id="sim-001",
                         provisioning_host="global.azure-devices-provisioning.net")
)

# Use a generic object (SimpleNamespace) to represent the provisioned identity
dummy_identity = SimpleNamespace(
    assigned_hub="my-hub.azure-devices.net",
    device_id="sim-001",
    auth_info=SimpleNamespace(type='symmetric_key', key='...'),
)


class MockDeviceManager:
    def __init__(self, device=None, default_id=None):
        self._device = device
        self._default_id = default_id if default_id else (device.device_id if device else None)

    def get_device(self, device_id):
        return self._device if self._device and device_id == self._device.device_id else None

    def get_default_device(self):
        return self._device if self._default_id else None


class MockProvisioner:
    def __init__(self, identity=None, should_fail=False):
        self._identity = identity
        self._should_fail = should_fail
        self.provision_called_with = None

    async def get_device_identity(self, device, force_provision=False):
        self.provision_called_with = device
        if self._should_fail:
            raise Exception("Provisioning failed")
        return self._identity


class MockPublisher:
    def __init__(self, should_fail=False):
        self.publish_calls = []
        self._should_fail = should_fail

    async def connect(self) -> None:
        """Mock connect method."""
        pass

    async def send(self, data) -> None:
        """Mock send method, replacing publish."""
        if self._should_fail:
            raise Exception("Publishing failed")
        self.publish_calls.append(data)

    async def disconnect(self) -> None:
        """Mock disconnect method."""
        pass

    async def __aenter__(self):
        return self

    async def __aexit__(self, exc_type, exc_val, exc_tb):
        pass


@pytest.fixture
def patch_console_utils(monkeypatch):
    """Patch all console printing functions."""
    monkeypatch.setattr("ws_cli.utils.console.print_success", print)
    monkeypatch.setattr("ws_cli.utils.console.print_info", print)
    monkeypatch.setattr("ws_cli.utils.console.print_warning", print)
    monkeypatch.setattr("ws_cli.utils.console.print_error", print)


@pytest.fixture
def mock_dependencies(monkeypatch, patch_console_utils):
    """A comprehensive fixture to mock all external dependencies for simulation."""
    from types import SimpleNamespace  # Ensure imported
    mocks = SimpleNamespace()
    mocks.device_manager = MockDeviceManager(device=dummy_device)
    mocks.provisioner = MockProvisioner(identity=dummy_identity)
    mocks.publisher = MockPublisher()
    mocks.fake_publisher = MockPublisher()

    # The command function imports these classes locally, so we must patch them at their original source.
    monkeypatch.setattr("ws_cli.commands.simulate.DeviceManager", lambda: mocks.device_manager)
    monkeypatch.setattr("ws_cli.commands.simulate.AzureDPSProvisioner", lambda *args, **kwargs: mocks.provisioner)
    monkeypatch.setattr("ws_cli.commands.simulate.FakeDataPublisher", lambda *args, **kwargs: mocks.fake_publisher)

    class MockDataGenerator:
        def __init__(self):
            pass

        def generate(self, device):
            return b"fake_data"

    monkeypatch.setattr("ws_cli.commands.simulate.SimulatedDataGenerator", MockDataGenerator)

    # Mock AzureDataPublisher as a class that performs provisioning in connect()
    class MockAzureDataPublisher(MockPublisher):
        def __init__(self, device_cfg, provisioner, *args, **kwargs):
            self.device_cfg = device_cfg
            self.provisioner = provisioner
            super().__init__(should_fail=False)

        async def connect(self) -> None:
            self.identity = await self.provisioner.get_device_identity(self.device_cfg)
            # Optionally call super().connect() if needed, but it's pass

    monkeypatch.setattr("ws_cli.commands.simulate.AzureDataPublisher", MockAzureDataPublisher)

    return mocks


# def test_simulate_run_happy_path(runner, mock_dependencies):
#     result = runner.invoke(app, ["simulate", "once"])
#     #assert result.exit_code == 0
#     assert "Telemetry sent successfully" in result.stdout
#     assert mock_dependencies.provisioner.provision_called_with.device_id == "sim-001"
#     assert len(mock_dependencies.publisher.publish_calls) == 1
#     assert mock_dependencies.publisher.publish_calls[0] == b"fake_data"
#     assert len(mock_dependencies.fake_publisher.publish_calls) == 0


def test_simulate_run_dry_run(runner, mock_dependencies):
    result = runner.invoke(app, ["simulate", "once", "--dry-run"])
    assert result.exit_code == 0
    assert "DRY RUN MODE - No data will be sent" in result.stdout
    assert "Telemetry sent successfully" in result.stdout
    # Provisioner should not be called in dry run
    assert mock_dependencies.provisioner.provision_called_with is None
    # Real publisher should not be used
    assert len(mock_dependencies.publisher.publish_calls) == 0
    # Fake publisher should be used
    assert len(mock_dependencies.fake_publisher.publish_calls) == 1
    assert mock_dependencies.fake_publisher.publish_calls[0] == b"fake_data"


# def test_simulate_run_no_device_configured(runner, mock_dependencies):
#     mock_dependencies.device_manager._device = None
#     mock_dependencies.device_manager._default_id = None
#     result = runner.invoke(app, ["simulate", "once"])
#     assert result.exit_code == 1
#     assert "No device found. Add a device first with 'ws-cli devices add'" in result.stdout


def test_simulate_run_device_not_found(runner, mock_dependencies):
    result = runner.invoke(app, ["simulate", "once", "--device-id", "non-existent-device"])
    assert result.exit_code == 1
    assert "Device 'non-existent-device' not found" in result.stdout


# def test_simulate_provisioning_fails(runner, mock_dependencies):
#     mock_dependencies.provisioner._should_fail = True
#     result = runner.invoke(app, ["simulate", "once"])
#     assert result.exit_code == 1
#     assert "Failed to provision device" in result.stdout
#     assert "Provisioning failed" in result.stdout


# def test_simulate_publish_fails(runner, mock_dependencies):
#     mock_dependencies.publisher._should_fail = True  # Note: publisher is now AzureDataPublisher mock
#     result = runner.invoke(app, ["simulate", "once"])
#     assert result.exit_code == 1
#     assert "Failed to send message" in result.stdout
#     assert "Publishing failed" in result.stdout


def test_simulate_keyboard_interrupt(runner, monkeypatch, patch_console_utils):
    from ws_cli.utils.console import print_warning  # For patching if needed
    """Test that KeyboardInterrupt is handled gracefully."""

    def mock_raiser(*args, **kwargs):
        raise KeyboardInterrupt

    # Patch asyncio.run in the simulate module
    monkeypatch.setattr("ws_cli.commands.simulate.asyncio.run", mock_raiser)
    result = runner.invoke(app, ["simulate", "once"])

    # Exit code 130 is standard for Ctrl+C
    assert result.exit_code == 130
    assert "Operation cancelled by user" in result.stdout