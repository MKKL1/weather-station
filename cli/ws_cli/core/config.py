import json
import logging
from pathlib import Path
from typing import Dict, Any, Optional, Union
from dataclasses import dataclass, asdict, field

from ws_cli.core.storage import APP_DIR

logger = logging.getLogger(__name__)


@dataclass
class SimulationDefaults:
    """Default simulation configuration."""
    interval_seconds: int = 1800
    jitter_seconds: float = 5.0
    max_messages: Optional[int] = None
    seed: Optional[int] = None
    dry_run: bool = False


@dataclass
class DPSDefaults:
    """Default DPS configuration."""
    provisioning_host: str = "global.azure-devices-provisioning.net"
    cache_ttl: int = 3600
    connection_timeout: int = 30
    max_retry_attempts: int = 3


@dataclass
class LoggingConfig:
    """Logging configuration."""
    level: str = "INFO"
    format: str = "%(asctime)s %(levelname)-8s [%(name)s] %(message)s"
    date_format: str = "%Y-%m-%d %H:%M:%S"


@dataclass
class AppConfig:
    """Main application configuration."""
    # Global settings
    verbose: bool = False

    # Default values for various components
    simulation: SimulationDefaults = field(default_factory=SimulationDefaults)
    dps: DPSDefaults = field(default_factory=DPSDefaults)
    logging: LoggingConfig = field(default_factory=LoggingConfig)

    # Custom user settings
    custom: Dict[str, Any] = field(default_factory=dict)

    @classmethod
    def from_dict(cls, data: Dict[str, Any]) -> "AppConfig":
        """Create config from dictionary."""
        # Handle nested dataclass conversion
        simulation_data = data.get("simulation", {})
        dps_data = data.get("dps", {})
        logging_data = data.get("logging", {})

        return cls(
            verbose=data.get("verbose", False),
            simulation=SimulationDefaults(**simulation_data),
            dps=DPSDefaults(**dps_data),
            logging=LoggingConfig(**logging_data),
            custom=data.get("custom", {})
        )

    def to_dict(self) -> Dict[str, Any]:
        """Convert to dictionary for serialization."""
        return {
            "verbose": self.verbose,
            "simulation": asdict(self.simulation),
            "dps": asdict(self.dps),
            "logging": asdict(self.logging),
            "custom": self.custom
        }

    def update_from_dict(self, updates: Dict[str, Any]) -> None:
        """Update configuration from dictionary (for partial updates)."""
        if "verbose" in updates:
            self.verbose = updates["verbose"]

        if "simulation" in updates:
            sim_updates = updates["simulation"]
            for key, value in sim_updates.items():
                if hasattr(self.simulation, key):
                    setattr(self.simulation, key, value)

        if "dps" in updates:
            dps_updates = updates["dps"]
            for key, value in dps_updates.items():
                if hasattr(self.dps, key):
                    setattr(self.dps, key, value)

        if "logging" in updates:
            log_updates = updates["logging"]
            for key, value in log_updates.items():
                if hasattr(self.logging, key):
                    setattr(self.logging, key, value)

        if "custom" in updates:
            self.custom.update(updates["custom"])


class ConfigManager:
    """Manages application configuration loading and saving."""

    DEFAULT_CONFIG_NAME = "config.json"

    def __init__(self):
        self._config: Optional[AppConfig] = None
        self._config_path: Optional[Path] = None

    def load_config(self, config_path: Optional[Union[str, Path]] = None) -> AppConfig:
        """
        Load configuration from file.

        Args:
            config_path: Optional path to config file. If None, uses default config.json

        Returns:
            AppConfig instance
        """
        if config_path:
            # Use explicitly provided config file
            self._config_path = Path(config_path).resolve()
            if not self._config_path.exists():
                logger.warning(f"Config file not found: {self._config_path}")
                logger.info("Using default configuration")
                self._config = AppConfig()
                return self._config
        else:
            # Use default config.json in app directory
            self._config_path = APP_DIR / self.DEFAULT_CONFIG_NAME

        if self._config_path.exists():
            try:
                with self._config_path.open("r", encoding="utf-8") as f:
                    config_data = json.load(f)

                self._config = AppConfig.from_dict(config_data)
                logger.debug(f"Loaded configuration from: {self._config_path}")

            except (json.JSONDecodeError, KeyError, TypeError) as e:
                logger.error(f"Failed to load config from {self._config_path}: {e}")
                logger.info("Using default configuration")
                self._config = AppConfig()
        else:
            logger.debug(f"Config file not found: {self._config_path}")
            logger.debug("Using default configuration")
            self._config = AppConfig()

        return self._config

    def save_config(self, config_path: Optional[Union[str, Path]] = None) -> None:
        """
        Save current configuration to file.

        Args:
            config_path: Optional path to save config. If None, uses loaded path or default.
        """
        if not self._config:
            raise ValueError("No configuration loaded")

        save_path = Path(config_path) if config_path else self._config_path
        if not save_path:
            save_path = APP_DIR / self.DEFAULT_CONFIG_NAME

        # Ensure directory exists
        save_path.parent.mkdir(parents=True, exist_ok=True)

        try:
            with save_path.open("w", encoding="utf-8") as f:
                json.dump(self._config.to_dict(), f, indent=2)

            logger.info(f"Configuration saved to: {save_path}")
        except IOError as e:
            logger.error(f"Failed to save config to {save_path}: {e}")
            raise

    def get_config(self) -> AppConfig:
        """Get current configuration (loads default if none loaded)."""
        if self._config is None:
            return self.load_config()
        return self._config

    def create_default_config(self, path: Optional[Union[str, Path]] = None) -> Path:
        """
        Create a default configuration file.

        Args:
            path: Optional path for config file. If None, uses default location.

        Returns:
            Path to created config file
        """
        config_path = Path(path) if path else APP_DIR / self.DEFAULT_CONFIG_NAME

        # Create default config
        default_config = AppConfig()

        # Ensure directory exists
        config_path.parent.mkdir(parents=True, exist_ok=True)

        with config_path.open("w", encoding="utf-8") as f:
            json.dump(default_config.to_dict(), f, indent=2)

        logger.info(f"Created default configuration: {config_path}")
        return config_path

    @property
    def config_path(self) -> Optional[Path]:
        """Get the path of the currently loaded config file."""
        return self._config_path


# Global config manager instance
_config_manager: Optional[ConfigManager] = None


def get_config_manager() -> ConfigManager:
    """Get global configuration manager instance."""
    global _config_manager
    if _config_manager is None:
        _config_manager = ConfigManager()
    return _config_manager


def get_config() -> AppConfig:
    """Get current application configuration."""
    return get_config_manager().get_config()