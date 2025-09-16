from pathlib import Path
import typer
import json
from typing import Dict, Any, Optional, TypeVar, Generic

APP_NAME = "ws-cli"
APP_DIR = Path(typer.get_app_dir(APP_NAME))
CONFIG_FILE_PATH = APP_DIR / "config.json"

T = TypeVar('T')


def ensure_config_dir_exists():
    APP_DIR.mkdir(parents=True, exist_ok=True)


class ConfigManager:
    """Generic configuration manager with key-value storage."""

    def __init__(self):
        ensure_config_dir_exists()
        self._config_path = CONFIG_FILE_PATH
        self._config: Optional[Dict[str, Any]] = None

    def _load_config(self) -> Dict[str, Any]:
        """Load the configuration file from disk."""
        if not self._config_path.exists():
            return {}
        try:
            with self._config_path.open("r") as f:
                return json.load(f)
        except (json.JSONDecodeError, IOError):
            # On corruption, start with a fresh config
            return {}

    def _save_config(self, config: Dict[str, Any]) -> None:
        """Save the configuration to disk."""
        with self._config_path.open("w") as f:
            json.dump(config, f, indent=4)

    def get_config_path(self) -> str:
        """Get the path to the configuration file."""
        return str(self._config_path.resolve())

    def get(self, key: str, default: Any = None) -> Any:
        """Get a value from the configuration."""
        if self._config is None:
            self._config = self._load_config()
        return self._config.get(key, default)

    def set(self, key: str, value: Any) -> None:
        """Set a value in the configuration and save to disk."""
        if self._config is None:
            self._config = self._load_config()
        self._config[key] = value
        self._save_config(self._config)

    def delete(self, key: str) -> bool:
        """Delete a key from the configuration and save to disk."""
        if self._config is None:
            self._config = self._load_config()
        if key in self._config:
            del self._config[key]
            self._save_config(self._config)
            return True
        return False

    def update(self, updates: Dict[str, Any]) -> None:
        """Update multiple values in the configuration and save to disk."""
        if self._config is None:
            self._config = self._load_config()
        self._config.update(updates)
        self._save_config(self._config)

    def get_all(self) -> Dict[str, Any]:
        """Get the entire configuration dictionary."""
        if self._config is None:
            self._config = self._load_config()
        return self._config.copy()

    def reload(self) -> None:
        """Force reload configuration from disk."""
        self._config = None

    def exists(self, key: str) -> bool:
        """Check if a key exists in the configuration."""
        if self._config is None:
            self._config = self._load_config()
        return key in self._config


class ConfigSection:
    """Helper class for managing a specific section of the configuration."""

    def __init__(self, config_manager: ConfigManager, section_key: str, default_value: Any = None):
        self.config_manager = config_manager
        self.section_key = section_key
        self.default_value = default_value or {}

    def get(self) -> Any:
        """Get the entire section."""
        return self.config_manager.get(self.section_key, self.default_value)

    def set(self, value: Any) -> None:
        """Set the entire section."""
        self.config_manager.set(self.section_key, value)

    def update(self, updates: Dict[str, Any]) -> None:
        """Update part of the section."""
        current = self.get()
        if isinstance(current, dict) and isinstance(updates, dict):
            current.update(updates)
            self.set(current)
        else:
            raise ValueError(f"Cannot update non-dict section '{self.section_key}'")

    def get_item(self, item_key: str, default: Any = None) -> Any:
        """Get a specific item from the section."""
        section = self.get()
        if isinstance(section, dict):
            return section.get(item_key, default)
        raise ValueError(f"Section '{self.section_key}' is not a dict")

    def set_item(self, item_key: str, value: Any) -> None:
        """Set a specific item in the section."""
        section = self.get()
        if not isinstance(section, dict):
            section = {}
        section[item_key] = value
        self.set(section)

    def delete_item(self, item_key: str) -> bool:
        """Delete a specific item from the section."""
        section = self.get()
        if isinstance(section, dict) and item_key in section:
            del section[item_key]
            self.set(section)
            return True
        return False

    def exists(self, item_key: str) -> bool:
        """Check if an item exists in the section."""
        section = self.get()
        return isinstance(section, dict) and item_key in section