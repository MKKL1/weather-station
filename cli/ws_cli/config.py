from pathlib import Path
import typer
import json
from typing import Dict, Any

APP_NAME = "ws-cli"
APP_DIR = Path(typer.get_app_dir(APP_NAME))
CONFIG_FILE_PATH = APP_DIR / "config.json"

def ensure_config_dir_exists():
    APP_DIR.mkdir(parents=True, exist_ok=True)


class ConfigManager:
    """Manages loading and saving of the configuration file."""

    def __init__(self):
        ensure_config_dir_exists()
        self._config_path = CONFIG_FILE_PATH

    def load_config(self) -> Dict[str, Any]:
        """Load the configuration file from disk."""
        if not self._config_path.exists():
            return {"devices": {}, "default_device": None}
        try:
            with self._config_path.open("r") as f:
                return json.load(f)
        except (json.JSONDecodeError, IOError):
            # On corruption, start with a fresh config
            return {"devices": {}, "default_device": None}

    def save_config(self, config: Dict[str, Any]) -> None:
        """Save the configuration to disk."""
        with self._config_path.open("w") as f:
            json.dump(config, f, indent=4)

    def get_config_path(self) -> str:
        """Get the path to the configuration file."""
        return str(self._config_path.resolve())