# ws_cli/commands/cache.py
from typing import Optional
import time
from datetime import datetime

import typer
from rich.table import Table
from rich.prompt import Confirm
from rich import print

from ws_cli.core.storage import get_data
from ws_cli.utils.console import print_info, print_success, print_error, print_warning

cache_app = typer.Typer(help="DPS cache management commands")


@cache_app.command("show")
def show_cache():
    """
    Show current DPS cache status and entries.

    Examples:
        ws-cli cache show
    """
    try:
        storage = get_data()
        cache_section = storage.section("dps_cache")
        cache_data = cache_section.get()

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
            if not isinstance(entry, dict):
                continue

            identity = entry.get("identity", {})
            auth_info = identity.get("auth_info", {})

            device_id = entry.get("device_id", "unknown")
            assigned_hub = identity.get("assigned_hub", "unknown")
            auth_type = auth_info.get("type", "unknown")

            cached_at = entry.get("cached_at") or 0
            ttl = entry.get("ttl") or 3600
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
        print(f"[dim]Cache file: {storage.path.resolve()}[/dim]")

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
    try:
        storage = get_data()
        cache_section = storage.section("dps_cache")

        if device_id:
            # Clear cache for specific device by finding matching entries
            cache_data = cache_section.get()
            keys_to_remove = []

            for cache_key, entry in cache_data.items():
                if isinstance(entry, dict) and entry.get("device_id") == device_id:
                    keys_to_remove.append(cache_key)

            if not keys_to_remove:
                print_warning(f"No cache entries found for device '{device_id}'")
                return

            if not force and not Confirm.ask(f"Clear DPS cache for device '{device_id}'?"):
                print_info("Cancelled")
                raise typer.Exit(0)

            for key in keys_to_remove:
                cache_section.delete_item(key)

            print_success(f"✓ Cleared DPS cache for device '{device_id}'")

        else:
            # Clear all cache
            cache_data = cache_section.get()
            if not cache_data:
                print_info("Cache is already empty")
                return

            if not force and not Confirm.ask("Clear all DPS cache entries?"):
                print_info("Cancelled")
                raise typer.Exit(0)

            cache_section.set({})
            print_success("✓ Cleared all DPS cache entries")

    except Exception as e:
        print_error(f"Failed to clear cache: {e}")
        raise typer.Exit(1)


@cache_app.command("validate")
def validate_cache():
    """
    Validate DPS cache entries and report any issues.

    Examples:
        ws-cli cache validate
    """
    try:
        storage = get_data()
        cache_section = storage.section("dps_cache")
        cache_data = cache_section.get()

        if not cache_data:
            print_info("No cache entries to validate")
            return

        issues = []
        expired = []
        valid = []
        malformed = []

        current_time = time.time()

        for cache_key, entry in cache_data.items():
            if not isinstance(entry, dict):
                malformed.append(cache_key)
                continue

            device_id = entry.get("device_id", "unknown")
            cached_at = entry.get("cached_at", 0)
            ttl = entry.get("ttl", 3600)
            expires_at = cached_at + ttl

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
        print(f"  Malformed entries: {len(malformed)}")
        print(f"  Structural issues: {len(issues)}")

        if expired:
            print_warning(f"Expired devices: {', '.join(expired)}")

        if malformed:
            print_warning(f"Malformed cache keys: {', '.join(malformed[:5])}")
            if len(malformed) > 5:
                print_warning(f"... and {len(malformed) - 5} more")

        if issues:
            print_error("Structural issues found:")
            for issue in issues:
                print(f"  - {issue}")

        if not expired and not malformed and not issues:
            print_success("✓ All cache entries are valid")
        else:
            print_info("Run 'ws-cli cache clear' to remove invalid entries")

    except Exception as e:
        print_error(f"Failed to validate cache: {e}")
        raise typer.Exit(1)


@cache_app.command("clean")
def clean_cache():
    """
    Remove expired and malformed cache entries.

    Examples:
        ws-cli cache clean
    """
    try:
        storage = get_data()
        cache_section = storage.section("dps_cache")
        cache_data = cache_section.get()

        if not cache_data:
            print_info("Cache is empty")
            return

        current_time = time.time()
        keys_to_remove = []

        for cache_key, entry in cache_data.items():
            should_remove = False

            # Remove malformed entries
            if not isinstance(entry, dict):
                should_remove = True
            else:
                # Remove expired entries
                cached_at = entry.get("cached_at", 0)
                ttl = entry.get("ttl", 3600)
                if current_time > cached_at + ttl:
                    should_remove = True

            if should_remove:
                keys_to_remove.append(cache_key)

        if not keys_to_remove:
            print_info("No expired or malformed entries to clean")
            return

        for key in keys_to_remove:
            cache_section.delete_item(key)

        print_success(f"✓ Cleaned {len(keys_to_remove)} expired/malformed cache entries")

    except Exception as e:
        print_error(f"Failed to clean cache: {e}")
        raise typer.Exit(1)