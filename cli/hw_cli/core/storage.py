# storage.py - Single file for all storage needs
import json
import yaml
import os
from pathlib import Path
from typing import Dict, Any, Optional
import typer

# Allow override via environment variable
def get_app_dir() -> Path:
    """Get application directory from env or use default user app dir."""
    env_path = os.getenv("HW_CLI_DATA_DIR")
    
    if env_path:
        return Path(env_path).expanduser().resolve()
    
    # Default to user app directory
    return Path(typer.get_app_dir("ws-cli", roaming=False)).expanduser()

APP_DIR = get_app_dir()


def ensure_app_dir():
    """Ensure app directory exists."""
    APP_DIR.mkdir(parents=True, exist_ok=True)


class Storage:
    """Simple file-based storage with auto-save."""

    def __init__(self, filename: str, file_format: str = "json"):
        ensure_app_dir()
        self.path = APP_DIR / filename
        self.format = file_format
        self._data: Optional[Dict[str, Any]] = None

    def _load(self) -> Dict[str, Any]:
        """Load data from file."""
        if not self.path.exists():
            return {}

        try:
            with self.path.open("r", encoding="utf-8") as f:
                if self.format == "json":
                    return json.load(f) or {}
                elif self.format == "yaml":
                    return yaml.safe_load(f) or {}
        except (json.JSONDecodeError, yaml.YAMLError, IOError):
            return {}

    def _save(self) -> None:
        """Save data to file."""
        if self._data is None:
            return

        with self.path.open("w", encoding="utf-8") as f:
            if self.format == "json":
                json.dump(self._data, f, indent=2)
            elif self.format == "yaml":
                yaml.safe_dump(self._data, f, default_flow_style=False)

    @property
    def data(self) -> Dict[str, Any]:
        """Get all data, loading if necessary."""
        if self._data is None:
            self._data = self._load()
        return self._data

    def get(self, key: str, default: Any = None) -> Any:
        """Get a value."""
        return self.data.get(key, default)

    def set(self, key: str, value: Any) -> None:
        """Set a value and save."""
        self.data[key] = value
        self._save()

    def delete(self, key: str) -> bool:
        """Delete a key and save."""
        if key in self.data:
            del self.data[key]
            self._save()
            return True
        return False

    def update(self, updates: Dict[str, Any]) -> None:
        """Update multiple values and save."""
        self.data.update(updates)
        self._save()

    def section(self, key: str) -> "StorageSection":
        """Get a section helper for nested data."""
        return StorageSection(self, key)


class StorageSection:
    """Helper for working with nested dictionary sections."""

    def __init__(self, storage: Storage, section_key: str):
        self.storage = storage
        self.key = section_key

    def get(self, default: Dict[str, Any] = None) -> Dict[str, Any]:
        """Get the section data."""
        section = self.storage.get(self.key, default or {})
        return section if isinstance(section, dict) else {}

    def set(self, value: Dict[str, Any]) -> None:
        """Set the entire section."""
        self.storage.set(self.key, value)

    def get_item(self, item_key: str, default: Any = None) -> Any:
        """Get an item from the section."""
        return self.get().get(item_key, default)

    def set_item(self, item_key: str, value: Any) -> None:
        """Set an item in the section."""
        section = self.get()
        section[item_key] = value
        self.set(section)

    def delete_item(self, item_key: str) -> bool:
        """Delete an item from the section."""
        section = self.get()
        if item_key in section:
            del section[item_key]
            self.set(section)
            return True
        return False

    def exists(self, item_key: str) -> bool:
        """Check if an item exists in the section."""
        return item_key in self.get()


_config_instance: Optional[Storage] = None
_data_instance: Optional[Storage] = None

def get_config() -> Storage:
    """Get config storage (YAML) - singleton."""
    global _config_instance
    if _config_instance is None:
        _config_instance = Storage("config.yaml", "yaml")
    return _config_instance

def get_data() -> Storage:
    """Get data storage (JSON) - singleton."""
    global _data_instance
    if _data_instance is None:
        _data_instance = Storage("data.json", "json")
    return _data_instance