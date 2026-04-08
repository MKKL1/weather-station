import base64
import json
import os
import sys
from typing import Optional

import typer
from rich import print as rich_print
from rich.prompt import Confirm, Prompt

from hw_cli.core.api.client import WeatherIoTClient
from hw_cli.core.device_manager import DeviceManager
from hw_cli.core.models import DeviceConfig
from hw_cli.utils.console import (
    print_error,
    print_info,
    print_success,
    print_table_header,
    print_table_row,
    print_warning,
)

app = typer.Typer(help="Device management commands", no_args_is_help=True)


def _extract_device_id_from_jwt(token: str) -> Optional[str]:
    try:
        parts = token.split(".")
        if len(parts) != 3:
            return None

        payload_b64 = parts[1]
        padding = "=" * (4 - len(payload_b64) % 4)
        payload_json = base64.urlsafe_b64decode(payload_b64 + padding)
        payload = json.loads(payload_json)

        return payload.get("sub")
    except Exception as e:
        print_error(f"Failed to parse JWT: {e}")
        return None


def _generate_unique_name(base_name: str, mgr: DeviceManager) -> str:
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
    ctx: typer.Context,
    name: Optional[str] = typer.Option(None, "--name", "-n", help="Friendly name"),
    provisioning_token: Optional[str] = typer.Option(
        None, "--token", "-t", help="Provisioning token (JWT)"
    ),
    api_url: Optional[str] = typer.Option(None, "--api-url", "-u", help="API base URL"),
    hmac_secret: Optional[str] = typer.Option(
        None, "--hmac-secret", help="HMAC secret"
    ),
    set_default: bool = typer.Option(
        False, "--set-default", help="Set as default device"
    ),
):
    """Add a new device configuration."""
    mgr = DeviceManager()

    if not provisioning_token:
        provisioning_token = os.getenv("HW_PROVISIONING_TOKEN")
        if not provisioning_token:
            provisioning_token = Prompt.ask("Provisioning token (JWT)", password=True)

    if not provisioning_token or not provisioning_token.strip():
        print_error("Provisioning token is required")
        raise typer.Exit(1)

    device_id = _extract_device_id_from_jwt(provisioning_token)
    if not device_id:
        print_error("Could not extract device_id from JWT")
        raise typer.Exit(1)

    device_name = name or device_id
    existing = mgr.get_device_by_id(device_id)

    if existing:
        print_warning(f"Device '{device_id}' already exists (named '{existing.name}')")
        if not Confirm.ask("Overwrite?"):
            raise typer.Exit(1)
        device_name = existing.name
    else:
        device_name = _generate_unique_name(device_name, mgr)
        if device_name != (name or device_id):
            print_info(f"Using unique name '{device_name}'")

    if not api_url:
        api_url = ctx.obj["config"].api.base_url

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
        if not ctx.obj["quiet"]:
            print_info(f"Default device set: {device_name}", file=sys.stderr)

    if not ctx.obj["quiet"]:
        print_success(f"Device '{device_name}' added", file=sys.stderr)


@app.command("register")
def register_device(
    ctx: typer.Context,
    device_ref: Optional[str] = typer.Argument(None, help="Device name or device_id"),
    raw: bool = typer.Option(
        False, "--raw", help="Output only claim code, suppress all status"
    ),
):
    """Register device and get claim code."""

    async def run():
        mgr = DeviceManager()
        device = mgr.resolve_device(device_ref)
        output_format = ctx.obj["output"]
        quiet = ctx.obj["quiet"] or raw

        if not device:
            print_error("Device not found")
            raise typer.Exit(1)

        try:
            async with WeatherIoTClient(device) as client:
                use_spinner = (output_format == "text") and (not quiet)

                if use_spinner:
                    import rich.console
                    from rich.progress import Progress, SpinnerColumn, TextColumn

                    with Progress(
                        SpinnerColumn(),
                        TextColumn("{task.description}"),
                        transient=True,
                        console=rich.console.Console(stderr=True),
                    ) as progress:
                        progress.add_task(f"Registering {device.name}...", total=None)
                        secret = await client.register()

                        device.hmac_secret = secret
                        mgr.update_device(device)
                        client.update_device(device)

                        progress.add_task("Requesting claim code...", total=None)
                        code = await client.get_claim_code()
                else:
                    if not quiet:
                        print_info(f"Registering {device.name}...", file=sys.stderr)
                    secret = await client.register()

                    device.hmac_secret = secret
                    mgr.update_device(device)
                    client.update_device(device)

                    if not quiet:
                        print_info("Requesting claim code...", file=sys.stderr)
                    code = await client.get_claim_code()

                if not quiet:
                    print_success("Registration successful", file=sys.stderr)
                    print_info(
                        "Enter this code in the server app UI to claim ownership.",
                        file=sys.stderr,
                    )

                if output_format == "json":
                    import dataclasses

                    print(json.dumps({"claim_code": code}))
                else:
                    print(code)

        except Exception as e:
            print_error(f"Registration failed: {e}")
            raise typer.Exit(1)

    import asyncio

    asyncio.run(run())


@app.command("list")
def list_devices(
    ctx: typer.Context,
):
    """List all configured devices."""
    mgr = DeviceManager()
    devices = mgr.get_devices()
    output = ctx.obj["output"]
    quiet = ctx.obj["quiet"]

    if not devices:
        if output == "json":
            print("[]")
        else:
            if not quiet:
                print_warning("No devices configured")
        return

    if output == "json":
        import dataclasses

        data = [dataclasses.asdict(d) for d in devices]
        for entry in data:
            entry.pop("provisioning_token", None)
            entry.pop("hmac_secret", None)
        print(json.dumps(data, indent=2, default=str))
        return

    default_id = mgr.get_default_device_id()

    cols = [("Name", 18), ("Device ID", 32), ("Registered", 12), ("Default", 7)]
    print_table_header(cols)

    for d in devices:
        row = [
            d.name,
            d.device_id,
            "Yes" if d.is_registered else "No",
            "*" if d.device_id == default_id else "",
        ]
        print_table_row(row, [c[1] for c in cols])

    if not quiet:
        print_info(f"\nFound {len(devices)} devices", file=sys.stderr)


@app.command("show")
def show_device(
    ctx: typer.Context,
    device_ref: str = typer.Argument(..., help="Device name or device_id"),
):
    """Show device details."""
    device = DeviceManager().resolve_device(device_ref)
    output = ctx.obj["output"]

    if not device:
        print_error(f"Device '{device_ref}' not found")
        raise typer.Exit(1)

    if output == "json":
        import dataclasses

        data = dataclasses.asdict(device)
        data.pop("provisioning_token", None)
        data.pop("hmac_secret", None)
        print(json.dumps(data, indent=2, default=str))
        return

    print(f"{'Name:':<14} {device.name}")
    print(f"{'Device ID:':<14} {device.device_id}")
    print(f"{'API URL:':<14} {device.api_base_url}")
    print(f"{'Registered:':<14} {'Yes' if device.is_registered else 'No'}")
    print(f"{'Created:':<14} {device.created_at.isoformat()}")


@app.command("remove")
def remove_device(
    ctx: typer.Context,
    device_ref: str = typer.Argument(..., help="Device name or device_id"),
    force: bool = typer.Option(False, "--force", "-f", help="Skip confirmation"),
):
    """Remove a device."""
    mgr = DeviceManager()
    device = mgr.resolve_device(device_ref)
    if not device:
        print_error(f"Device '{device_ref}' not found")
        raise typer.Exit(1)

    if not force and not Confirm.ask(f"Remove device '{device.name}'?"):
        if not ctx.obj["quiet"]:
            print_info("Cancelled", file=sys.stderr)
        raise typer.Exit(1)

    mgr.remove_device(device.device_id)
    if not ctx.obj["quiet"]:
        print_success(f"Device removed", file=sys.stderr)


@app.command("set-default")
def set_default(
    ctx: typer.Context,
    device_ref: str = typer.Argument(..., help="Device name or device_id"),
):
    """Set the default device."""
    mgr = DeviceManager()
    device = mgr.resolve_device(device_ref)
    if not device:
        print_error(f"Device '{device_ref}' not found")
        raise typer.Exit(1)

    mgr.set_default_device(device.device_id)
    if not ctx.obj["quiet"]:
        print_success(f"Default set to {device.name}", file=sys.stderr)
