# ws_cli/commands/cache.py
from typing import Optional

import typer

from ws_cli.core.device_manager import DeviceManager
from ws_cli.core.identity.azure_dps_provisioner import AzureDPSProvisioner
from ws_cli.core.identity.provisioning_cache import ProvisioningCache

# This would be imported as a sub-command in the main config.py
cache_app = typer.Typer(help="DPS cache management commands")


@cache_app.command("show")
def show_cache():
    """
    Show current DPS cache status and entries.

    Examples:
        ws-cli cache show
    """
    from ws_cli.core.config import ConfigManager
    from ws_cli.utils.console import print_warning, print_error
    from rich.table import Table
    from rich import print
    import time
    from datetime import datetime

    try:
        config_manager = ConfigManager()
        cache = ProvisioningCache(config_manager)

        # Load cache data directly
        cache_data = cache._load_cache()

        if not cache_data:
            print_warning("No DPS cache entries found")
            return

        # Create table
        table = Table(title="DPS Cache Entries", show_lines=True)
        table.add_column("Device ID", style="cyan", no_wrap=True)
        table.add_column("Assigned Hub", style="dim")
        table.add_column("Auth Type", style="dim")
        table.add_column("Cached At", style="dim")
        table.add_column("Expires", style="yellow")
        table.add_column("Status", style="green")

        current_time = time.time()

        for cache_key, entry in cache_data.items():
            identity = entry.get("identity", {})
            auth_info = identity.get("auth_info", {})

            device_id = entry.get("device_id", "unknown")
            assigned_hub = identity.get("assigned_hub", "unknown")
            auth_type = auth_info.get("type", "unknown")

            cached_at = entry.get("cached_at", 0)
            ttl = entry.get("ttl", 3600)
            expires_at = cached_at + ttl

            cached_time_str = datetime.fromtimestamp(cached_at).strftime("%Y-%m-%d %H:%M:%S")
            expires_time_str = datetime.fromtimestamp(expires_at).strftime("%Y-%m-%d %H:%M:%S")

            # Determine status
            if current_time > expires_at:
                status = "[red]Expired[/red]"
            else:
                remaining = expires_at - current_time
                if remaining < 300:  # Less than 5 minutes
                    status = "[yellow]Expiring Soon[/yellow]"
                else:
                    status = "[green]Valid[/green]"

            table.add_row(
                device_id,
                assigned_hub,
                auth_type,
                cached_time_str,
                expires_time_str,
                status
            )

        print(table)
        print(f"\n[dim]Total entries: {len(cache_data)}[/dim]")
        print(f"[dim]Cache file: {cache._cache_file}[/dim]")

    except Exception as e:
        print_error(f"Failed to show cache: {e}")
        raise typer.Exit(1)


@cache_app.command("clear")
def clear_cache(
        device_id: Optional[str] = typer.Option(
            None,
            "--device-id",
            "-d",
            help="Clear cache for specific device only"
        ),
        force: bool = typer.Option(
            False,
            "--force",
            "-f",
            help="Skip confirmation prompt"
        ),
):
    """
    Clear DPS cache entries.

    Examples:
        ws-cli cache clear
        ws-cli cache clear --device-id sim-001
        ws-cli cache clear --force
    """
    from ws_cli.core.config import ConfigManager
    from ws_cli.utils.console import print_info, print_success, print_error
    from rich.prompt import Confirm

    try:
        config_manager = ConfigManager()
        cache = ProvisioningCache(config_manager)

        if device_id:
            # Clear cache for specific device
            device_manager = DeviceManager()
            device = device_manager.get_device(device_id)

            if not device:
                print_error(f"Device '{device_id}' not found")
                raise typer.Exit(1)

            if not force and not Confirm.ask(f"Clear DPS cache for device '{device_id}'?"):
                print_info("Cancelled")
                raise typer.Exit(0)

            cache.invalidate_device(device)
            print_success(f"✓ Cleared DPS cache for device '{device_id}'")

        else:
            # Clear all cache
            if not force and not Confirm.ask("Clear all DPS cache entries?"):
                print_info("Cancelled")
                raise typer.Exit(0)

            cache.clear_cache()
            print_success("✓ Cleared all DPS cache entries")

    except Exception as e:
        print_error(f"Failed to clear cache: {e}")
        raise typer.Exit(1)


@cache_app.command("refresh")
def refresh_cache(
        device_id: Optional[str] = typer.Option(
            None,
            "--device-id",
            "-d",
            help="Refresh cache for specific device (default: all devices)"
        ),
        ttl: int = typer.Option(
            3600,
            "--ttl",
            help="Cache TTL in seconds (default: 3600 = 1 hour)"
        ),
):
    """
    Force refresh DPS cache entries by re-provisioning.

    Examples:
        ws-cli cache refresh
        ws-cli cache refresh --device-id sim-001
        ws-cli cache refresh --ttl 86400  # 24 hours
    """
    import asyncio
    from ws_cli.core.config import ConfigManager
    from ws_cli.utils.console import print_info, print_success, print_error, print_warning
    from rich.progress import Progress, SpinnerColumn, TextColumn

    async def refresh_device_cache(provisioner, device, progress_bar=None, task_id=None):
        """Refresh cache for a single device."""
        if progress_bar and task_id is not None:
            progress_bar.update(task_id, description=f"Refreshing {device.device_id}...")

        try:
            # Force provision to refresh cache
            identity = await provisioner.get_device_identity(device, force_provision=True)

            # Cache with custom TTL
            provisioner.cache.cache_identity(device, identity, ttl=ttl)

            return True, None
        except Exception as e:
            return False, str(e)

    async def refresh_all():
        config_manager = ConfigManager()
        provisioner = AzureDPSProvisioner(config_manager)
        device_manager = DeviceManager()

        if device_id:
            # Refresh specific device
            device = device_manager.get_device(device_id)
            if not device:
                print_error(f"Device '{device_id}' not found")
                return False

            devices_to_refresh = [device]
        else:
            # Refresh all devices
            devices_to_refresh = device_manager.get_devices()
            if not devices_to_refresh:
                print_warning("No devices configured")
                return False

        print_info(f"Refreshing DPS cache for {len(devices_to_refresh)} device(s) with TTL {ttl}s")

        with Progress(
                SpinnerColumn(),
                TextColumn("[progress.description]{task.description}"),
                transient=True,
        ) as progress:

            success_count = 0
            for device in devices_to_refresh:
                task = progress.add_task(description=f"Refreshing {device.device_id}...", total=None)

                success, error = await refresh_device_cache(provisioner, device, progress, task)

                if success:
                    success_count += 1
                else:
                    print_error(f"Failed to refresh cache for {device.device_id}: {error}")

        if success_count == len(devices_to_refresh):
            print_success(f"✓ Successfully refreshed cache for all {success_count} device(s)")
        else:
            failed_count = len(devices_to_refresh) - success_count
            print_warning(f"Refreshed {success_count}/{len(devices_to_refresh)} devices ({failed_count} failed)")

        return success_count > 0

    try:
        success = asyncio.run(refresh_all())
        if not success:
            raise typer.Exit(1)

    except KeyboardInterrupt:
        print_warning("\nRefresh cancelled by user")
        raise typer.Exit(130)
    except Exception as e:
        print_error(f"Failed to refresh cache: {e}")
        raise typer.Exit(1)


@cache_app.command("validate")
def validate_cache():
    """
    Validate DPS cache entries and report any issues.

    Examples:
        ws-cli cache validate
    """
    from ws_cli.core.config import ConfigManager
    from ws_cli.utils.console import print_info, print_success, print_error, print_warning
    import time

    try:
        config_manager = ConfigManager()
        cache = ProvisioningCache(config_manager)
        device_manager = DeviceManager()

        # Load cache and devices
        cache_data = cache._load_cache()
        devices = {d.device_id: d for d in device_manager.get_devices()}

        if not cache_data:
            print_info("No cache entries to validate")
            return

        issues = []
        expired = []
        valid = []
        orphaned = []

        current_time = time.time()

        for cache_key, entry in cache_data.items():
            device_id = entry.get("device_id", "unknown")
            cached_at = entry.get("cached_at", 0)
            ttl = entry.get("ttl", 3600)
            expires_at = cached_at + ttl

            # Check if device still exists
            if device_id not in devices:
                orphaned.append(device_id)
                continue

            # Check if expired
            if current_time > expires_at:
                expired.append(device_id)
            else:
                valid.append(device_id)

            # Validate identity structure
            identity = entry.get("identity", {})
            if not identity.get("assigned_hub"):
                issues.append(f"Device {device_id}: Missing assigned_hub")
            if not identity.get("auth_info"):
                issues.append(f"Device {device_id}: Missing auth_info")

        # Print results
        print_info("Cache Validation Results:")
        print(f"  Valid entries: {len(valid)}")
        print(f"  Expired entries: {len(expired)}")
        print(f"  Orphaned entries: {len(orphaned)} (device no longer exists)")
        print(f"  Structural issues: {len(issues)}")

        if expired:
            print_warning(f"Expired devices: {', '.join(expired)}")

        if orphaned:
            print_warning(f"Orphaned cache entries: {', '.join(orphaned)}")

        if issues:
            print_error("Structural issues found:")
            for issue in issues:
                print(f"  - {issue}")

        if not expired and not orphaned and not issues:
            print_success("✓ All cache entries are valid")
        else:
            print_info("Run 'ws-cli cache clear' to remove invalid entries")
            print_info("Run 'ws-cli cache refresh' to update expired entries")

    except Exception as e:
        print_error(f"Failed to validate cache: {e}")
        raise typer.Exit(1)