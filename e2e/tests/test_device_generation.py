import re

from tests.models import DeviceProvisioning

def test_cli_is_generating_device_from_key(generate_device):
    device_provisioning = generate_device()

    assert isinstance(device_provisioning, DeviceProvisioning)
    assert re.match(r"^H1-[A-Z0-9]{24}$", device_provisioning.device_id)
