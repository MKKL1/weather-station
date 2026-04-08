from typing import List, Optional

from hw_cli.core.models import DeviceConfig
from hw_cli.core.storage import get_data


class DeviceManager:
    def __init__(self, db=None):
        self._db = db or get_data()

    def resolve_device(self, ref: Optional[str] = None) -> Optional[DeviceConfig]:
        """Resolve device by name/ID, or fallback to default if ref is None."""
        if ref:
            return self.get_device_by_name(ref) or self.get_device_by_id(ref)
        return self.get_default_device()

    def add_device(self, device: DeviceConfig) -> None:
        self._db.save_device(device.device_id, device.to_dict())

    def update_device(self, device: DeviceConfig) -> None:
        self._db.save_device(device.device_id, device.to_dict())

    def remove_device(self, device_id: str) -> bool:
        if not self.device_exists_by_id(device_id):
            return False

        deleted = self._db.delete_device(device_id)
        if deleted and self.get_default_device_id() == device_id:
            self._db.delete_setting("default_device")
        return deleted

    def get_device_by_id(self, device_id: str) -> Optional[DeviceConfig]:
        data = self._db.get_device(device_id)
        return DeviceConfig.from_dict(data) if data else None

    def get_device_by_name(self, name: str) -> Optional[DeviceConfig]:
        for device in self.get_devices():
            if device.name == name:
                return device
        return None

    def device_exists_by_id(self, device_id: str) -> bool:
        return self._db.device_exists(device_id)

    def device_exists_by_name(self, name: str) -> bool:
        return self.get_device_by_name(name) is not None

    def get_devices(self) -> List[DeviceConfig]:
        raw_list = self._db.get_all_devices()
        return [DeviceConfig.from_dict(d) for d in raw_list]

    def set_default_device(self, device_id: str) -> None:
        if not self.device_exists_by_id(device_id):
            raise ValueError(f"Device with device_id '{device_id}' not found")
        self._db.set_setting("default_device", device_id)

    def get_default_device_id(self) -> Optional[str]:
        return self._db.get_setting("default_device")

    def get_default_device(self) -> Optional[DeviceConfig]:
        device_id = self.get_default_device_id()
        if device_id:
            return self.get_device_by_id(device_id)

        devices = self.get_devices()
        return devices[0] if devices else None

    def get_storage_path(self) -> str:
        return str(self._db.db_path)
