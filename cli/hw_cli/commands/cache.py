import json
import sys
import time
from datetime import datetime

import typer
from rich.prompt import Confirm

from hw_cli.core.storage import get_data
from hw_cli.core.token_cache import get_token_cache
from hw_cli.utils.console import print_info, print_success, print_warning

app = typer.Typer(help="Token cache management commands", no_args_is_help=True)


@app.command("show")
def show_cache(ctx: typer.Context):
    """Show cached tokens and their status in a simple aligned list."""
    cache = get_token_cache()
    entries = cache.get_all_entries()
    output = ctx.obj["output"]
    quiet = ctx.obj["quiet"]

    if not entries:
        if output == "json":
            print("[]")
        else:
            if not quiet:
                print_warning("No cached tokens", file=sys.stderr)
        return

    if output == "json":
        print(json.dumps(entries, indent=2, default=str))
        return

    now = time.time()

    print(f"{'Device ID':<30}  {'Status':<15}  {'Expires At'}")
    print("-" * 65)

    for device_id, entry in entries.items():
        if not isinstance(entry, dict):
            continue

        expires_at = entry.get("expires_at", 0)
        expires_str = datetime.fromtimestamp(expires_at).strftime("%Y-%m-%d %H:%M:%S")

        if now > expires_at:
            status = "Expired"
        elif now > expires_at - 300:
            status = "Expiring Soon"
        else:
            remaining = int(expires_at - now)
            status = f"Valid ({remaining}s)"

        print(f"{device_id:<30}  {status:<15}  {expires_str}")

    if not quiet:
        stats = cache.get_stats()
        print(
            f"\nTotal: {stats['total']} | Valid: {stats['valid']} | Expired: {stats['expired']}",
            file=sys.stderr,
        )
        print(f"File:  {get_data().db_path}", file=sys.stderr)


@app.command("clear")
def clear_cache(
    ctx: typer.Context,
    device: str = typer.Option(
        None,
        "--device",
        "--name",
        "-n",
        "--device-id",
        "-d",
        help="Clear specific device only",
    ),
    force: bool = typer.Option(False, "--force", "-f", help="Skip confirmation"),
):
    """Clear cached tokens."""
    cache = get_token_cache()

    if device:
        from hw_cli.core.device_manager import DeviceManager

        mgr = DeviceManager()
        device_obj = mgr.resolve_device(device)

        target_id = device_obj.device_id if device_obj else device
        target_name = device_obj.name if device_obj else device

        if not force and not Confirm.ask(f"Clear token for device '{target_name}'?"):
            print_info("Cancelled")
            raise typer.Exit(1)

        if cache.invalidate(target_id):
            print_success(f"Cleared token for '{target_name}'")
        else:
            print_warning(f"No cached token for '{target_name}'")
    else:
        stats = cache.get_stats()
        if stats["total"] == 0:
            print_info("Cache is empty")
            return

        if not force and not Confirm.ask(f"Clear all {stats['total']} cached tokens?"):
            print_info("Cancelled")
            raise typer.Exit(1)

        count = cache.clear_all()
        if not ctx.obj["quiet"]:
            print_success(f"Cleared {count} cached tokens", file=sys.stderr)


@app.command("clean")
def clean_cache(ctx: typer.Context):
    """Remove expired tokens only."""
    cache = get_token_cache()
    removed = cache.cleanup_expired()

    if removed:
        if not ctx.obj["quiet"]:
            print_success(f"Removed {removed} expired tokens", file=sys.stderr)
    else:
        if not ctx.obj["quiet"]:
            print_info("No expired tokens to clean", file=sys.stderr)


@app.command("stats")
def cache_stats(ctx: typer.Context):
    """Show cache statistics."""
    cache = get_token_cache()
    stats = cache.get_stats()
    output = ctx.obj["output"]

    if output == "json":
        print(json.dumps(stats, indent=2))
        return

    print_info("Token Cache Statistics", file=sys.stderr)
    print(f"  Total:   {stats['total']}")
    print(f"  Valid:   {stats['valid']}")
    print(f"  Expired: {stats['expired']}")
    print(f"  File:    {get_data().db_path}")
