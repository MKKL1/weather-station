import json
import logging
import sys
from dataclasses import asdict, dataclass, field
from pathlib import Path
from typing import Any, Dict, Optional

from hw_cli.core.storage import get_app_dir

logger = logging.getLogger(__name__)


@dataclass
class SimulationDefaults:
    interval_seconds: int = 1800
    jitter_seconds: float = 5.0
    max_messages: Optional[int] = None


@dataclass
class ApiDefaults:
    base_url: str = "https://apim-weather-app-dev.azure-api.net"
    timeout_seconds: int = 30
    token_refresh_buffer: int = 60


@dataclass
class LoggingConfig:
    level: str = "ERROR"
    format: str = "%(asctime)s %(levelname)-8s [%(name)s] %(message)s"
    date_format: str = "%H:%M:%S"


@dataclass
class AppConfig:
    """Application configuration."""
    verbose: bool = False
    simulation: SimulationDefaults = field(default_factory=SimulationDefaults)
    api: ApiDefaults = field(default_factory=ApiDefaults)
    logging: LoggingConfig = field(default_factory=LoggingConfig)

    @classmethod
    def from_dict(cls, data: Dict[str, Any]) -> "AppConfig":
        return cls(
            verbose=data.get("verbose", False),
            simulation=SimulationDefaults(**data.get("simulation", {})),
            api=ApiDefaults(**data.get("api", {})),
            logging=LoggingConfig(**data.get("logging", {})),
        )

    def to_dict(self) -> Dict[str, Any]:
        return {
            "verbose": self.verbose,
            "simulation": asdict(self.simulation),
            "api": asdict(self.api),
            "logging": asdict(self.logging),
        }


class ConfigManager:
    """Stateless manager for configuration I/O."""

    CONFIG_FILE = "config.json"

    def resolve_path(self, path: Optional[Path] = None) -> Path:
        """Resolve config file path from explicit path or default location."""
        if path:
            return path.resolve()
        return get_app_dir() / self.CONFIG_FILE

    def load(self, path: Optional[Path] = None) -> AppConfig:
        """Load configuration from file or return defaults."""
        config_path = self.resolve_path(path)

        if config_path.exists():
            try:
                with config_path.open("r", encoding="utf-8") as f:
                    data = json.load(f)
                    return AppConfig.from_dict(data)
            except json.JSONDecodeError as e:
                # UX FIX: Do not silence syntax errors.
                # Users need to know their config is invalid.
                print(f"[ERROR] Config file is invalid JSON: {config_path}", file=sys.stderr)
                print(f"Details: {e}", file=sys.stderr)
                sys.exit(1)
            except TypeError as e:
                # This usually means the structure of the JSON doesn't match
                # what the dataclasses expect (Validation error).
                logger.warning(f"Config structure mismatch in {config_path}: {e}")
                logger.warning("Falling back to default configuration for missing fields.")
                return AppConfig()

        return AppConfig()

    def save(self, config: AppConfig, path: Optional[Path] = None) -> None:
        """Save configuration to file."""
        save_path = self.resolve_path(path)
        save_path.parent.mkdir(parents=True, exist_ok=True)

        with save_path.open("w", encoding="utf-8") as f:
            json.dump(config.to_dict(), f, indent=2)

    def create_default(self, path: Optional[Path] = None) -> Path:
        """Create a default configuration file if it doesn't exist."""
        target = self.resolve_path(path)
        if not target.exists():
            self.save(AppConfig(), target)
        return target


def load_config(path: Optional[Path] = None) -> AppConfig:
    """Helper function to load configuration."""
    return ConfigManager().load(path)