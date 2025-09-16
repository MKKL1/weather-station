from typing import Optional, List

from ws_cli.core.config import ConfigManager, ConfigSection
from ws_cli.core.models.models import Device


class DeviceManager:
    """Manages storage and retrieval of device configurations using ConfigManager."""

    # Configuration keys
    DEVICES_KEY = "devices"
    DEFAULT_DEVICE_KEY = "default_device"

    def __init__(self, config_manager: Optional[ConfigManager] = None):
        self._config_manager = config_manager or ConfigManager()
        self._devices_section = ConfigSection(self._config_manager, self.DEVICES_KEY, {})

    def add_device(self, device: Device) -> None:
        """Add or update a device configuration."""
        self._devices_section.set_item(device.device_id, device.to_dict())

    def remove_device(self, device_id: str) -> bool:
        """Remove a device configuration."""
        if self.device_exists(device_id):
            self._devices_section.delete_item(device_id)

            # Unset default if the removed device was the default
            if self.get_default_device_id() == device_id:
                self._config_manager.delete(self.DEFAULT_DEVICE_KEY)

            return True
        return False

    def get_device(self, device_id: str) -> Optional[Device]:
        """Get a device by its ID."""
        device_data = self._devices_section.get_item(device_id)
        if device_data:
            return Device.from_dict(device_data)
        return None

    def device_exists(self, device_id: str) -> bool:
        """Check if a device with the given ID exists."""
        return self._devices_section.exists(device_id)

    def get_devices(self) -> List[Device]:
        """List all configured devices."""
        devices_dict = self._devices_section.get()
        return [Device.from_dict(device_data) for device_data in devices_dict.values()]

    def set_default_device(self, device_id: str) -> None:
        """Set the default device."""
        if self.device_exists(device_id):
            self._config_manager.set(self.DEFAULT_DEVICE_KEY, device_id)
        else:
            raise ValueError(f"Device '{device_id}' not found.")

    def get_default_device_id(self) -> Optional[str]:
        """Get the ID of the default device."""
        return self._config_manager.get(self.DEFAULT_DEVICE_KEY)

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

    def get_device_count(self) -> int:
        """Get the total number of configured devices."""
        devices_dict = self._devices_section.get()
        return len(devices_dict)

    def clear_all_devices(self) -> None:
        """Remove all device configurations and clear default."""
        self._devices_section.set({})
        self._config_manager.delete(self.DEFAULT_DEVICE_KEY)

    def update_device(self, device_id: str, updates: dict) -> bool:
        """Update specific fields of a device configuration."""
        device = self.get_device(device_id)
        if not device:
            return False

        # Convert to dict, apply updates, convert back
        device_dict = device.to_dict()
        device_dict.update(updates)

        # Validate by recreating the device object
        updated_device = Device.from_dict(device_dict)
        self.add_device(updated_device)
        return True

    def get_devices_by_auth_type(self, auth_type: str) -> List[Device]:
        """Get all devices using a specific authentication type."""
        return [device for device in self.get_devices()
                if device.auth.type.value == auth_type]