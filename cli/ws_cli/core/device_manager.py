from typing import Optional, List

from ws_cli.config import ConfigManager
from ws_cli.models import Device


class DeviceManager:
    """Manages storage and retrieval of device configurations."""

    def __init__(self):
        self._config_manager = ConfigManager()
        self._config = self._config_manager.load_config()
        self._devices = self._config.get("devices", {})

    def _save(self) -> None:
        """Save the current configuration to disk."""
        self._config["devices"] = self._devices
        self._config_manager.save_config(self._config)

    def add_device(self, device: Device) -> None:
        """Add or update a device configuration."""
        self._devices[device.device_id] = device.to_dict()
        self._save()

    def remove_device(self, device_id: str) -> bool:
        """Remove a device configuration."""
        if device_id in self._devices:
            del self._devices[device_id]
            # Unset default if the removed device was the default
            if self._config["default_device"] == device_id:
                self._config["default_device"] = None
            self._save()
            return True
        return False

    def get_device(self, device_id: str) -> Optional[Device]:
        """Get a device by its ID."""
        device_data = self._devices.get(device_id)
        if device_data:
            return Device.from_dict(device_data)
        return None

    def device_exists(self, device_id: str) -> bool:
        """Check if a device with the given ID exists."""
        return device_id in self._devices

    def get_devices(self) -> List[Device]:
        """List all configured devices."""
        return [Device.from_dict(d) for d in self._devices.values()]

    def set_default_device(self, device_id: str) -> None:
        """Set the default device."""
        if self.device_exists(device_id):
            self._config["default_device"] = device_id
            self._save()
        else:
            raise ValueError(f"Device '{device_id}' not found.")

    def get_default_device_id(self) -> Optional[str]:
        """Get the ID of the default device."""
        return self._config.get("default_device")

    def get_default_device(self) -> Optional[Device]:
        """Get the default device configuration."""
        device_id = self.get_default_device_id()
        if device_id:
            return self.get_device(device_id)
        # If no default is set, return the first device in the list
        devices = self.get_devices()
        return devices[0] if devices else None

    def get_config_path(self) -> str:
        """Get the path to the configuration file."""
        return self._config_manager.get_config_path()