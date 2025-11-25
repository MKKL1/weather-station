import base64
import json
import os
import sys
from typing import Optional

import click
import typer
from rich import print as rich_print
from rich.prompt import Confirm, Prompt
from rich.table import Table

from hw_cli.core.config import load_config
from hw_cli.core.device_manager import DeviceManager
from hw_cli.core.models import DeviceConfig
from hw_cli.utils.console import print_error, print_info, print_success, print_warning

app = typer.Typer(help="Device management commands", no_args_is_help=True)


def _get_default_api_url() -> str:
    """Get default API URL from config or fallback."""
    config = load_config()
    return config.api.base_url


def _extract_device_id_from_jwt(token: str) -> Optional[str]:
    """Extract device_id (sub claim) from JWT token without verification."""
    try:
        # JWT format: header.payload.signature
        parts = token.split('.')
        if len(parts) != 3:
            return None

        # Decode payload (add padding if needed)
        payload_b64 = parts[1]
        padding = '=' * (4 - len(payload_b64) % 4)
        payload_json = base64.urlsafe_b64decode(payload_b64 + padding)
        payload = json.loads(payload_json)

        return payload.get('sub')
    except Exception as e:
        print_error(f"Failed to parse JWT token: {e}")
        return None


def _generate_unique_name(base_name: str, mgr: DeviceManager) -> str:
    """Generate unique name by adding numeric suffix if needed."""
    if not mgr.device_exists_by_name(base_name):
        return base_name

    counter = 1
    while True:
        candidate = f"{base_name}-{counter}"
        if not mgr.device_exists_by_name(candidate):
            return candidate
        counter += 1


@app.command("add")
def add_device(
        name: Optional[str] = typer.Option(
            None,
            "--name",
            "-n",
            help="Friendly name for the device (defaults to device_id from token)",
        ),
        provisioning_token: Optional[str] = typer.Option(
            None,
            "--token",
            "-t",
            help="Factory provisioning token (JWT). If not provided, will prompt securely.",
        ),
        api_url: Optional[str] = typer.Option(
            None,
            "--api-url",
            "-u",
            help="API base URL (defaults to config value)",
        ),
        hmac_secret: Optional[str] = typer.Option(
            None,
            "--hmac-secret",
            help="HMAC secret (if already registered)",
        ),
        set_default: bool = typer.Option(False, "--set-default", help="Set as default device"),
):
    """Add a new device configuration using JWT token."""
    mgr = DeviceManager()

    # Secure token input
    if not provisioning_token:
        # Check environment variable first
        provisioning_token = os.getenv("HW_PROVISIONING_TOKEN")
        if not provisioning_token:
            # Prompt securely without echoing
            provisioning_token = Prompt.ask(
                "Provisioning token (JWT)",
                password=True
            )

    if not provisioning_token or not provisioning_token.strip():
        print_error("Provisioning token is required")
        raise typer.Exit(1)

    # Extract device_id from JWT token
    device_id = _extract_device_id_from_jwt(provisioning_token)
    if not device_id:
        print_error("Could not extract device_id from JWT token")
        raise typer.Exit(1)

    # Use device_id as name if not provided
    device_name = name or device_id

    # Check if device with this device_id already exists (by actual ID)
    existing = mgr.get_device_by_id(device_id)
    if existing:
        print_warning(f"Device with device_id '{device_id}' already exists (named '{existing.name}')")
        if not Confirm.ask("Overwrite?"):
            raise typer.Exit(1)
        # Keep the same name when overwriting
        device_name = existing.name
    else:
        # Generate unique name if there's a conflict
        device_name = _generate_unique_name(device_name, mgr)
        if device_name != (name or device_id):
            print_info(f"Name '{name or device_id}' already exists, using '{device_name}' instead")

    # Get API URL from config if not provided
    if not api_url:
        api_url = _get_default_api_url()

    device = DeviceConfig(
        device_id=device_id,
        name=device_name,
        api_base_url=api_url,
        provisioning_token=provisioning_token,
        hmac_secret=hmac_secret,
    )
    mgr.add_device(device)

    if set_default or not mgr.get_default_device_id():
        mgr.set_default_device(device_id)
        print_info(f"Set '{device_name}' as default device")

    print_success(f"Device '{device_name}' added successfully (device_id: {device_id})")


@app.command("list")
def list_devices():
    """List all configured devices."""
    mgr = DeviceManager()
    devices = mgr.get_devices()

    if not devices:
        print_warning("No devices configured. Use 'hw devices add' to add one.")
        return

    default_id = mgr.get_default_device_id()
    table = Table(title="Devices")
    table.add_column("Name", style="cyan")
    table.add_column("Device ID", style="dim")
    table.add_column("API URL", style="dim")
    table.add_column("Registered", style="dim")
    table.add_column("Default", style="green")

    for d in devices:
        table.add_row(
            d.name,
            d.device_id,
            d.api_base_url,
            "✓" if d.is_registered else "✗",
            "✓" if d.device_id == default_id else "",
        )

    rich_print(table)


@app.command("show")
def show_device(device_ref: str = typer.Argument(..., help="Device name or device_id")):
    """Show device details."""
    device = _resolve_device(device_ref)
    if not device:
        raise typer.Exit(1)

    rich_print(f"\n[cyan]Device: {device.name}[/cyan]", file=sys.stderr)
    rich_print(f"Device ID: {device.device_id}", file=sys.stderr)
    rich_print(f"API URL: {device.api_base_url}", file=sys.stderr)
    rich_print(f"Registered: {'Yes' if device.is_registered else 'No'}", file=sys.stderr)
    rich_print(f"mm per tip: {device.mm_per_tip}", file=sys.stderr)
    rich_print(f"Created: {device.created_at.isoformat()}", file=sys.stderr)


@app.command("remove")
def remove_device(
        device_ref: str = typer.Argument(..., help="Device name or device_id"),
        force: bool = typer.Option(False, "--force", "-f", help="Skip confirmation"),
):
    """Remove a device."""
    device = _resolve_device(device_ref)
    if not device:
        raise typer.Exit(1)

    if not force and not Confirm.ask(f"Remove device '{device.name}' (device_id: {device.device_id})?"):
        print_info("Cancelled")
        raise typer.Exit(1)

    DeviceManager().remove_device(device.device_id)
    print_success(f"Device '{device.name}' removed")


@app.command("set-default")
def set_default(device_ref: str = typer.Argument(..., help="Device name or device_id")):
    """Set the default device."""
    device = _resolve_device(device_ref)
    if not device:
        raise typer.Exit(1)

    DeviceManager().set_default_device(device.device_id)
    print_success(f"Set '{device.name}' as default")


def _resolve_device(ref: str) -> Optional[DeviceConfig]:
    """Resolve device by name or device_id."""
    mgr = DeviceManager()

    # Try by name first
    device = mgr.get_device_by_name(ref)
    if device:
        return device

    # Try by device_id
    device = mgr.get_device_by_id(ref)
    if device:
        return device

    print_error(f"Device '{ref}' not found")
    return None