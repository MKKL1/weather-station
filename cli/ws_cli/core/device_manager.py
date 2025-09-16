from typing import Optional, List
from ws_cli.core.storage import get_data
from ws_cli.core.models.models import Device


class DeviceManager:
    """Manages storage and retrieval of device configurations."""

    def __init__(self):
        self._storage = get_data()
        self._devices = self._storage.section("devices")

    def add_device(self, device: Device) -> None:
        """Add or update a device configuration."""
        self._devices.set_item(device.device_id, device.to_dict())

    def remove_device(self, device_id: str) -> bool:
        """Remove a device configuration."""
        if not self.device_exists(device_id):
            return False

        self._devices.delete_item(device_id)

        # Unset default if the removed device was the default
        if self.get_default_device_id() == device_id:
            self._storage.delete("default_device")

        return True

    def get_device(self, device_id: str) -> Optional[Device]:
        """Get a device by its ID."""
        device_data = self._devices.get_item(device_id)
        if device_data:
            return Device.from_dict(device_data)
        return None

    def device_exists(self, device_id: str) -> bool:
        """Check if a device with the given ID exists."""
        return self._devices.exists(device_id)

    def get_devices(self) -> List[Device]:
        """List all configured devices."""
        devices_dict = self._devices.get()
        return [Device.from_dict(device_data) for device_data in devices_dict.values()]

    def set_default_device(self, device_id: str) -> None:
        """Set the default device."""
        if not self.device_exists(device_id):
            raise ValueError(f"Device '{device_id}' not found.")
        self._storage.set("default_device", device_id)

    def get_default_device_id(self) -> Optional[str]:
        """Get the ID of the default device."""
        return self._storage.get("default_device")

    def get_default_device(self) -> Optional[Device]:
        """Get the default device configuration."""
        device_id = self.get_default_device_id()
        if device_id:
            return self.get_device(device_id)

        # If no default is set, return the first device in the list
        devices = self.get_devices()
        return devices[0] if devices else None

    def get_device_count(self) -> int:
        """Get the total number of configured devices."""
        return len(self._devices.get())

    def clear_all_devices(self) -> None:
        """Remove all device configurations and clear default."""
        self._devices.set({})
        self._storage.delete("default_device")