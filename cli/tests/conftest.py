# tests/conftest.py
import pytest
from typer.testing import CliRunner
from typing import Dict, Any


class MockStorageSection:
    """Mocks the StorageSection helper for nested data."""

    def __init__(self, parent_data: Dict[str, Any], section_key: str):
        self._data = parent_data
        self.key = section_key
        if self.key not in self._data:
            self._data[self.key] = {}

    def get(self, default: Dict[str, Any] = None) -> Dict[str, Any]:
        return self._data.get(self.key, default or {})

    def set(self, value: Dict[str, Any]) -> None:
        self._data[self.key] = value

    def get_item(self, item_key: str, default: Any = None) -> Any:
        return self.get().get(item_key, default)

    def set_item(self, item_key: str, value: Any) -> None:
        section = self.get()
        section[item_key] = value
        self.set(section)

    def delete_item(self, item_key: str) -> bool:
        section = self.get()
        if item_key in section:
            del section[item_key]
            self.set(section)
            return True
        return False

    def exists(self, item_key: str) -> bool:
        return item_key in self.get()


class MockStorage:
    """Mocks the Storage class to use an in-memory dictionary."""

    def __init__(self, initial_data=None):
        self._data = initial_data or {}
        self.path = "/fake/path/data.json"

    def get(self, key: str, default: Any = None) -> Any:
        return self._data.get(key, default)

    def set(self, key: str, value: Any) -> None:
        self._data[key] = value

    def delete(self, key: str) -> bool:
        if key in self._data:
            del self._data[key]
            return True
        return False

    def section(self, key: str) -> MockStorageSection:
        return MockStorageSection(self._data, key)


@pytest.fixture
def runner():
    """Provides a CliRunner instance for invoking commands."""
    return CliRunner()


@pytest.fixture
def mock_storage(monkeypatch):
    """
    Mocks the `get_data` function to return an in-memory storage object.
    This is the central fixture for controlling application data during tests.
    """
    mock_store = MockStorage()
    # Patch the get_data function in all modules where it's used.
    monkeypatch.setattr("hw_cli.core.storage.get_data", lambda: mock_store)
    monkeypatch.setattr("hw_cli.core.device_manager.get_data", lambda: mock_store)
    monkeypatch.setattr("hw_cli.core.identity.provisioning_cache.get_data", lambda: mock_store)
    monkeypatch.setattr("hw_cli.commands.cache.get_data", lambda: mock_store)
    return mock_store


@pytest.fixture(autouse=True)
def patch_console_utils(monkeypatch):
    """
    Automatically patches all console printing functions for every test
    to avoid rich formatting issues in test output.
    """
    # Using lambda to ignore the message argument and just print a simple string
    monkeypatch.setattr("hw_cli.utils.console.print_success", lambda msg: print(f"SUCCESS: {msg}"))
    monkeypatch.setattr("hw_cli.utils.console.print_info", lambda msg: print(f"INFO: {msg}"))
    monkeypatch.setattr("hw_cli.utils.console.print_warning", lambda msg: print(f"WARNING: {msg}"))
    monkeypatch.setattr("hw_cli.utils.console.print_error", lambda msg: print(f"ERROR: {msg}"))