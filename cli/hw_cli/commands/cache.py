import sys
import time
from datetime import datetime

import typer
from rich import print as rich_print
from rich.prompt import Confirm
from rich.table import Table

from hw_cli.core.storage import get_data
from hw_cli.core.token_cache import get_token_cache
from hw_cli.utils.console import print_info, print_success, print_warning

app = typer.Typer(help="Token cache management commands", no_args_is_help=True)


@app.command("show")
def show_cache():
    """Show cached tokens and their status."""
    cache = get_token_cache()
    entries = cache.get_all_entries()

    if not entries:
        print_warning("No cached tokens")
        return

    now = time.time()
    table = Table(title="Cached Tokens")
    table.add_column("Device ID", style="cyan")
    table.add_column("Cached At", style="dim")
    table.add_column("Expires At", style="dim")
    table.add_column("Status", style="green")

    for device_id, entry in entries.items():
        if not isinstance(entry, dict):
            continue

        cached_at = entry.get("cached_at", 0)
        expires_at = entry.get("expires_at", 0)

        cached_str = datetime.fromtimestamp(cached_at).strftime("%Y-%m-%d %H:%M:%S")
        expires_str = datetime.fromtimestamp(expires_at).strftime("%Y-%m-%d %H:%M:%S")

        if now > expires_at:
            status = "[red]Expired[/red]"
        elif now > expires_at - 300:
            status = "[yellow]Expiring Soon[/yellow]"
        else:
            remaining = int(expires_at - now)
            status = f"[green]Valid ({remaining}s)[/green]"

        table.add_row(device_id, cached_str, expires_str, status)

    rich_print(table)
    stats = cache.get_stats()

    rich_print(f"\n[dim]Total: {stats['total']}, Valid: {stats['valid']}, Expired: {stats['expired']}[/dim]", file=sys.stderr)
    # FIX: Updated to use .db_path from the new Database class
    rich_print(f"[dim]Cache file: {get_data().db_path}[/dim]", file=sys.stderr)


@app.command("clear")
def clear_cache(
    device_id: str = typer.Option(None, "--device-id", "-d", help="Clear specific device only"),
    force: bool = typer.Option(False, "--force", "-f", help="Skip confirmation"),
):
    """Clear cached tokens."""
    cache = get_token_cache()

    if device_id:
        if not force and not Confirm.ask(f"Clear token for device '{device_id}'?"):
            print_info("Cancelled")
            raise typer.Exit(1)

        if cache.invalidate(device_id):
            print_success(f"Cleared token for '{device_id}'")
        else:
            print_warning(f"No cached token for '{device_id}'")
    else:
        stats = cache.get_stats()
        if stats["total"] == 0:
            print_info("Cache is empty")
            return

        if not force and not Confirm.ask(f"Clear all {stats['total']} cached tokens?"):
            print_info("Cancelled")
            raise typer.Exit(1)

        count = cache.clear_all()
        print_success(f"Cleared {count} cached tokens")


@app.command("clean")
def clean_cache():
    """Remove expired tokens only."""
    cache = get_token_cache()
    removed = cache.cleanup_expired()

    if removed:
        print_success(f"Removed {removed} expired tokens")
    else:
        print_info("No expired tokens to clean")


@app.command("stats")
def cache_stats():
    """Show cache statistics."""
    cache = get_token_cache()
    stats = cache.get_stats()

    rich_print("[cyan]Token Cache Statistics[/cyan]", file=sys.stderr)
    rich_print(f"  Total entries: {stats['total']}", file=sys.stderr)
    rich_print(f"  Valid: {stats['valid']}", file=sys.stderr)
    rich_print(f"  Expired: {stats['expired']}", file=sys.stderr)
    rich_print(f"  Cache file: {get_data().db_path}", file=sys.stderr)