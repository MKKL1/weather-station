import json
import os
import subprocess
import sys
from pathlib import Path
from typing import Optional

import typer
from rich import print as rich_print
from rich.prompt import Confirm

from hw_cli.core.config import ConfigManager
from hw_cli.core.storage import get_app_dir, get_data
from hw_cli.utils.console import print_error, print_info, print_success

app = typer.Typer(help="Configuration management commands", no_args_is_help=True)


@app.command("show")
def show_config():
    """Show current configuration."""
    mgr = ConfigManager()
    config = mgr.load()

    cfg_path = mgr.resolve_path()
    exists = cfg_path.exists()

    rich_print("[cyan]Configuration[/cyan]", file=sys.stderr)
    rich_print(f"  File: {cfg_path}", file=sys.stderr)
    rich_print(f"  Status: {'loaded' if exists else 'using defaults'}", file=sys.stderr)
    rich_print(file=sys.stderr)

    # Print config to stdout (this is data output)
    print(json.dumps(config.to_dict(), indent=2))


@app.command("create")
def create_config(
        path: Optional[Path] = typer.Option(None, "--path", "-p", help="Config file path"),
        force: bool = typer.Option(False, "--force", "-f", help="Overwrite existing"),
):
    """Create a default configuration file."""
    mgr = ConfigManager()
    cfg_path = mgr.resolve_path(path)

    if cfg_path.exists() and not force:
        print_error(f"Config exists: {cfg_path}")
        if not Confirm.ask("Overwrite?"):
            print_info("Cancelled")
            raise typer.Exit(1)

    created = mgr.create_default(cfg_path)
    print_success(f"Created config: {created}")


@app.command("edit")
def edit_config():
    """Open config file in default editor."""
    mgr = ConfigManager()
    cfg_path = mgr.resolve_path()

    if not cfg_path.exists():
        print_error("No config file. Use 'hw config create' first.")
        raise typer.Exit(1)

    path_str = str(cfg_path.resolve())

    try:
        if os.name == "nt":
            os.startfile(path_str)
        elif sys.platform == "darwin":
            subprocess.call(["open", path_str])
        else:
            subprocess.call(["xdg-open", path_str])
    except Exception as e:
        print_error(f"Failed to open editor: {e}")
        print_info(f"Edit manually: {path_str}")


@app.command("path")
def config_path():
    """Show storage paths and file status."""
    mgr = ConfigManager()

    app_dir = get_app_dir()
    config_file = mgr.resolve_path()
    # FIX: Updated to use .db_path from the new Database class
    data_file = get_data().db_path

    rich_print(f"[bold cyan]Storage Directory:[/bold cyan] {app_dir}", file=sys.stderr)
    rich_print(f"  Exists: {'[green]Yes[/green]' if app_dir.exists() else '[red]No[/red]'}", file=sys.stderr)

    rich_print(f"\n[bold cyan]Configuration File:[/bold cyan] {config_file}", file=sys.stderr)
    rich_print(
        f"  Status: {'[green]Found[/green]' if config_file.exists() else '[yellow]Not created (using defaults)[/yellow]'}",
        file=sys.stderr
    )

    rich_print(f"\n[bold cyan]Data Storage:[/bold cyan] {data_file}", file=sys.stderr)
    rich_print(f"  Status: {'[green]Found[/green]' if data_file.exists() else '[yellow]Empty[/yellow]'}",
               file=sys.stderr)