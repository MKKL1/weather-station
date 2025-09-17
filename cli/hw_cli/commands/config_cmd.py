# hw_cli/commands/config_cmd.py
import json
import sys
from typing import Optional
from pathlib import Path

import typer
from rich import print
from rich.prompt import Confirm

from hw_cli.core.config import get_config_manager
from hw_cli.core.storage import APP_DIR
from hw_cli.utils.console import print_info, print_success, print_error

config_app = typer.Typer(help="Configuration management commands")


@config_app.command("show")
def show_config():
    """Show current configuration."""
    try:
        config_manager = get_config_manager()
        config = config_manager.get_config()

        print(f"[bold cyan]Configuration[/bold cyan]")

        # Prefer showing the resolved absolute path if available, otherwise show
        # the default path that will be used (APP_DIR/config.json).
        cfg_path = config_manager.config_path
        if cfg_path:
            try:
                shown_path = str(Path(cfg_path).resolve())
            except Exception:
                shown_path = str(cfg_path)
        else:
            shown_path = str((APP_DIR / "config.json").resolve())

        print(f"Config file: {shown_path}")
        print()

        # Create a nice display of the config
        config_dict = config.to_dict()
        print(json.dumps(config_dict, indent=2))

    except Exception as e:
        print_error(f"Failed to show config: {e}")
        raise typer.Exit(1)


@config_app.command("create")
def create_config(
    path: Optional[Path] = typer.Option(
        None,
        "--path",
        "-p",
        help="Path for config file (defaults to config.json in app directory)"
    ),
    force: bool = typer.Option(
        False,
        "--force",
        "-f",
        help="Overwrite existing config file"
    )
):
    """Create a default configuration file."""
    try:
        config_manager = get_config_manager()

        # Determine the target path in a safe way:
        if path:
            config_path = Path(path).resolve()
        else:
            # prefer the currently loaded config path, otherwise use APP_DIR/config.json
            cfg = config_manager.config_path
            config_path = Path(cfg).resolve() if cfg else (APP_DIR / "config.json").resolve()

        if config_path.exists() and not force:
            print_error(f"Config file already exists: {config_path}")
            if Confirm.ask("Overwrite existing file?"):
                force = True
            else:
                print_info("Cancelled")
                raise typer.Exit(0)

        created_path = config_manager.create_default_config(config_path)
        try:
            created_path = Path(created_path).resolve()
        except Exception:
            created_path = Path(created_path)

        print_success(f"Created configuration file: {created_path}")

    except Exception as e:
        print_error(f"Failed to create config: {e}")
        raise typer.Exit(1)


@config_app.command("edit")
def edit_config():
    """Open config file in default editor."""
    import os
    import subprocess

    try:
        config_manager = get_config_manager()
        config_path = config_manager.config_path

        if not config_path or not Path(config_path).exists():
            print_error("No config file found. Use 'ws-cli config create' first.")
            raise typer.Exit(1)

        resolved = str(Path(config_path).resolve())

        # Try to open with system default
        if os.name == 'nt':  # Windows
            os.startfile(resolved)
        elif os.name == 'posix':  # macOS and Linux
            subprocess.call(['open' if sys.platform == 'darwin' else 'xdg-open', resolved])
        else:
            print_info(f"Please edit the config file manually: {resolved}")

    except Exception as e:
        print_error(f"Failed to open config file: {e}")
        raise typer.Exit(1)
