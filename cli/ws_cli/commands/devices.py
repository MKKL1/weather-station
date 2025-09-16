from pathlib import Path
from typing import Optional

import typer


from ws_cli.core.device_manager import DeviceManager
from ws_cli.core.models.models import AuthConfig, DPSConfig, Device, AuthType

app = typer.Typer(help="Device management commands")


@app.command("add")
def add_device(
        device_id: str = typer.Argument(
            ...,
            help="Unique device identifier",
        ),
        auth_type: AuthType = typer.Option(
            ...,
            "--auth-type",
            "-t",
            help="Authentication type",
            case_sensitive=False,
        ),
        cert_file: Optional[Path] = typer.Option(
            None,
            "--cert-file",
            help="X.509 certificate file path",
            exists=True,
            file_okay=True,
            dir_okay=False,
        ),
        key_file: Optional[Path] = typer.Option(
            None,
            "--key-file",
            help="X.509 private key file path",
            exists=True,
            file_okay=True,
            dir_okay=False,
        ),
        key_passphrase: Optional[str] = typer.Option(
            None,
            "--key-passphrase",
            help="Private key passphrase (if encrypted)",
            hide_input=True,
        ),
        primary_key: Optional[str] = typer.Option(
            None,
            "--primary-key",
            "-k",
            help="Symmetric primary key (base64 encoded)",
            hide_input=True,
        ),
        dps_id_scope: str = typer.Option(
            ...,
            "--dps-id-scope",
            help="Azure DPS ID Scope (if provisioning via DPS)",
        ),
        dps_provisioning_host: str = typer.Option(
            "global.azure-devices-provisioning.net",
            "--dps-provisioning-host",
            help="DPS provisioning host (defaults to Azure global endpoint)",
        ),
        dps_registration_id: Optional[str] = typer.Option(
            None,
            "--dps-registration-id",
            help="DPS registration id (defaults to device id)",
        ),
        set_default: bool = typer.Option(
            False,
            "--set-default",
            help="Set as default device",
        ),
):
    """
    Add a new device configuration.

    Examples:
        # X.509 authentication
        ws-cli devices add sim-001 --auth-type x509 --cert-file /secrets/cert.pem --key-file /secrets/key.pem

        # Symmetric key authentication
        ws-cli devices add sim-002 --auth-type symmetric_key --primary-key "AbCd...=="

        # Symmetric key + DPS
        ws-cli devices add dev-device-92fc2ca1 --auth-type symmetric_key --primary-key "h9...=" --dps-id-scope "0ne00FD4B37"
    """
    from ws_cli.utils.console import print_info, print_success, print_error, print_warning
    from rich.prompt import Confirm

    try:
        device_manager = DeviceManager()

        # Check if device already exists
        if device_manager.device_exists(device_id):
            print_error(f"Device '{device_id}' already exists")
            if Confirm.ask("Do you want to update it?"):
                # TODO: Implement update logic
                print_warning("Update functionality coming soon")
            raise typer.Exit(1)

        # Validate authentication configuration
        if auth_type == AuthType.X509:
            if not cert_file or not key_file:
                print_error("X.509 authentication requires --cert-file and --key-file")
                raise typer.Exit(1)
            auth_config = AuthConfig(
                type=auth_type,
                cert_file=str(cert_file),
                key_file=str(key_file),
                key_passphrase=key_passphrase,
            )
        elif auth_type == AuthType.SYMMETRIC_KEY:
            if not primary_key:
                print_error("Symmetric key authentication requires --primary-key")
                raise typer.Exit(1)
            auth_config = AuthConfig(
                type=auth_type,
                symmetric_key=primary_key,
            )
        else:
            print_error(f"Unsupported auth type: {auth_type}")
            raise typer.Exit(1)

        if auth_type != AuthType.SYMMETRIC_KEY:
            print_warning("DPS configuration provided — typically used with symmetric_key attestation. Proceeding, but verify your DPS attestation type.")

        reg_id = dps_registration_id or device_id
        dps_config = DPSConfig(
            id_scope=dps_id_scope,
            provisioning_host=dps_provisioning_host,
            registration_id=reg_id,
        )

        device = Device(
            device_id=device_id,
            auth=auth_config,
            dps_config=dps_config,
            # TODO: Add more device configuration
        )

        # Save device
        device_manager.add_device(device)

        if set_default or not device_manager.get_default_device_id():
            device_manager.set_default_device(device_id)
            print_info(f"Set '{device_id}' as default device")

        print_success(f"✓ Device '{device_id}' added successfully")

    except Exception as e:
        print_error(f"Failed to add device: {e}")
        raise typer.Exit(1)


@app.command("list")
def list_devices(
        verbose: bool = typer.Option(
            False,
            "--verbose",
            "-v",
            help="Show detailed information",
        ),
):
    """
    List all configured devices.

    Examples:
        ws-cli devices list
        ws-cli devices list --verbose
    """
    from ws_cli.utils.console import print_error, print_warning
    from rich.table import Table
    from rich import print

    try:
        device_manager = DeviceManager()
        devices = device_manager.get_devices()

        if not devices:
            print_warning("No devices configured. Add a device with 'ws-cli devices add'")
            return

        # Create table
        table = Table(title="Configured Devices", show_lines=verbose)
        table.add_column("ID", style="cyan", no_wrap=True)
        table.add_column("Device ID", style="dim")
        table.add_column("Auth Type", style="dim")
        table.add_column("DPS Scope", style="dim")
        table.add_column("Default", style="green")

        if verbose:
            table.add_column("Created", style="dim")
            table.add_column("Config", style="dim")

        default_device_id = device_manager.get_default_device_id()

        for idx, device in enumerate(devices):
            is_default = "✓" if device.device_id == default_device_id else ""

            row = [
                str(idx),
                device.device_id,
                device.auth.type.value,
                device.dps_config.id_scope,
                is_default,
            ]

            if verbose:
                # TODO: Add created timestamp and config details
                row.extend([device.created_at.isoformat(), "..."])

            table.add_row(*row)

        print(table)

        if verbose:
            print(f"\n[dim]Total devices: {len(devices)}[/dim]")
            print(f"[dim]Config location: {device_manager.get_config_path()}[/dim]")

    except Exception as e:
        print_error(f"Failed to list devices: {e}")
        raise typer.Exit(1)


@app.command("set-default")
def set_default_device(
        device_ref: str = typer.Argument(
            ...,
            help="Device ID or index from 'devices list'",
            autocompletion=lambda: ["sim-001", "sim-002", "0", "1"],  # TODO: Dynamic
        ),
):
    """
    Set the default device for simulations.

    Examples:
        ws-cli devices set-default sim-001
        ws-cli devices set-default 0
    """
    from ws_cli.utils.console import print_info, print_success, print_error

    try:
        device_manager = DeviceManager()

        # Try to interpret as index first
        try:
            idx = int(device_ref)
            devices = device_manager.get_devices()
            if 0 <= idx < len(devices):
                device_id = devices[idx].device_id
            else:
                print_error(f"Invalid device index: {idx}")
                raise typer.Exit(1)
        except ValueError:
            # Not an index, treat as device ID
            device_id = device_ref

        # Check if device exists
        if not device_manager.device_exists(device_id):
            print_error(f"Device '{device_id}' not found")
            print_info("Run 'ws-cli devices list' to see available devices")
            raise typer.Exit(1)

        # Set as default
        device_manager.set_default_device(device_id)
        print_success(f"✓ Set '{device_id}' as default device")

    except Exception as e:
        print_error(f"Failed to set default device: {e}")
        raise typer.Exit(1)


@app.command("remove")
def remove_device(
        device_ref: str = typer.Argument(
            ...,
            help="Device ID or index from 'devices list'",
        ),
        force: bool = typer.Option(
            False,
            "--force",
            "-f",
            help="Skip confirmation prompt",
        ),
):
    """
    Remove a device configuration.

    Examples:
        ws-cli devices remove sim-001
        ws-cli devices remove 0 --force
    """
    from ws_cli.utils.console import print_info, print_success, print_error
    from rich.prompt import Confirm

    try:
        device_manager = DeviceManager()

        # Try to interpret as index first
        try:
            idx = int(device_ref)
            devices = device_manager.get_devices()
            if 0 <= idx < len(devices):
                device_id = devices[idx].device_id
            else:
                print_error(f"Invalid device index: {idx}")
                raise typer.Exit(1)
        except ValueError:
            # Not an index, treat as device ID
            device_id = device_ref

        # Check if device exists
        if not device_manager.device_exists(device_id):
            print_error(f"Device '{device_id}' not found")
            raise typer.Exit(1)

        # Confirm removal
        if not force:
            if not Confirm.ask(f"Remove device '{device_id}'?"):
                print_info("Cancelled")
                raise typer.Exit(0)

        # Remove device
        device_manager.remove_device(device_id)
        print_success(f"✓ Device '{device_id}' removed")

    except Exception as e:
        print_error(f"Failed to remove device: {e}")
        raise typer.Exit(1)


@app.command("show")
def show_device(
        device_ref: str = typer.Argument(
            ...,
            help="Device ID or index from 'devices list'",
        ),
):
    """
    Show detailed information about a device.

    Examples:
        ws-cli devices show sim-001
        ws-cli devices show 0
    """
    from ws_cli.utils.console import print_error
    from rich import print

    try:
        device_manager = DeviceManager()

        # Try to interpret as index first
        try:
            idx = int(device_ref)
            devices = device_manager.get_devices()
            if 0 <= idx < len(devices):
                device = devices[idx]
            else:
                print_error(f"Invalid device index: {idx}")
                raise typer.Exit(1)
        except ValueError:
            # Not an index, treat as device ID
            device = device_manager.get_device(device_ref)
            if not device:
                print_error(f"Device '{device_ref}' not found")
                raise typer.Exit(1)

        # Display device information
        print(f"\n[bold cyan]Device: {device.device_id}[/bold cyan]")
        print(f"[yellow]Authentication Type:[/yellow] {device.auth.type.value}")

        if device.auth.type == AuthType.X509:
            print(f"[yellow]Certificate:[/yellow] {device.auth.cert_file}")
            print(f"[yellow]Private Key:[/yellow] {device.auth.key_file}")
        elif device.auth.type == AuthType.SYMMETRIC_KEY:
            print(f"[yellow]Symmetric Key:[/yellow] [dim]<hidden>[/dim]")

        # DPS info
        if device.dps_config:
            print(f"\n[bold blue]DPS Configuration[/bold blue]")
            print(f"[yellow]ID Scope:[/yellow] {device.dps_config.id_scope}")
            print(f"[yellow]Provisioning Host:[/yellow] {device.dps_config.provisioning_host}")
            print(f"[yellow]Registration ID:[/yellow] {device.dps_config.registration_id}")

        # TODO: Add more device details
        print("\n[dim]Additional configuration coming soon...[/dim]")

    except Exception as e:
        print_error(f"Failed to show device: {e}")
        raise typer.Exit(1)