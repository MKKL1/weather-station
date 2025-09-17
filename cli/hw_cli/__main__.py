import logging
import sys

import typer
from typer import Context
from typing import Optional
from pathlib import Path

if any(arg == "?" for arg in sys.argv[1:]):
    sys.argv = [sys.argv[0]] + ["--help" if arg == "?" else arg for arg in sys.argv[1:]]

from hw_cli.commands import simulate, devices, enrollment
from hw_cli.commands.cache import cache_app
from hw_cli.commands.config_cmd import config_app  # NEW IMPORT
from hw_cli.core.config import get_config_manager, AppConfig  # NEW IMPORT

app = typer.Typer(
    name="ws-cli",
    help="Weather Station CLI - A tool for simulating weather telemetry data",
    add_completion=True,
    rich_markup_mode="rich",
    no_args_is_help=True,
    pretty_exceptions_enable=True,
)

app.add_typer(simulate.app, name="simulate", help="Simulation commands")
app.add_typer(devices.app, name="devices", help="Device management commands")
app.add_typer(cache_app, name="cache", help="Cache management commands")
app.add_typer(enrollment.app, name="enrollment", help="Enrollment management commands")
app.add_typer(config_app, name="config", help="Configuration management commands")  # NEW LINE


def _configure_logging(verbose: bool, config: Optional[AppConfig] = None) -> None:
    """Set up the root logger using config values (safe to call multiple times)."""
    root = logging.getLogger()

    # If no handlers exist, create a basic console handler.
    if not root.handlers:
        handler = logging.StreamHandler()

        # Use config format if available
        if config and config.logging.format:
            formatter = logging.Formatter(
                config.logging.format,
                datefmt=config.logging.date_format,
            )
        else:
            formatter = logging.Formatter(
                "%(asctime)s %(levelname)-8s [%(name)s] %(message)s",
                datefmt="%Y-%m-%d %H:%M:%S",
            )
        handler.setFormatter(formatter)
        root.addHandler(handler)

    # Set level: verbose flag takes precedence, otherwise default to WARNING
    if verbose:
        root.setLevel(logging.DEBUG)
    else:
        # Default to WARNING when not verbose to avoid library INFO spam
        # Config logging level only applies when verbose is True
        root.setLevel(logging.WARNING)


def verbose_callback(ctx: typer.Context, param, value: bool):
    # Click/Typer often supplies `ctx.resilient_parsing` while
    # building shell completion or showing help; ignore in that case.
    if ctx.resilient_parsing:
        return value
    # Note: We'll do the actual logging configuration in main() after loading config
    return value


def version_callback(value: bool):
    if value:
        from hw_cli.utils.console import print_success
        print_success("Weather Station CLI v0.1.0")
        raise typer.Exit()


@app.callback()
def main(
        ctx: Context,
        version: Optional[bool] = typer.Option(
            None,
            "--version",
            "-v",
            help="Show version and exit",
            callback=version_callback,
            is_eager=True,
        ),
        config: Optional[Path] = typer.Option(
            None,
            "--config",
            "-c",
            help="Configuration file path (defaults to config.json)",
            envvar="hw_cli_CONFIG",
        ),
        verbose: bool = typer.Option(
            False,
            "--verbose",
            help="Enable verbose output",
            callback=verbose_callback,
            is_eager=True,
        ),
):
    """
    Weather Station CLI - Simulate weather telemetry for Azure IoT Hub
    """
    from hw_cli.utils.console import print_info

    # Initialize context
    if ctx.obj is None:
        ctx.obj = {}

    # Load configuration
    config_manager = get_config_manager()
    app_config = config_manager.load_config(config)

    # Override config verbose setting with CLI flag if provided
    if verbose and not app_config.verbose:
        app_config.verbose = True

    # Store config and verbose in context for other commands
    ctx.obj["config"] = app_config
    ctx.obj["verbose"] = app_config.verbose or verbose

    # Configure logging with config settings
    _configure_logging(app_config.verbose or verbose, app_config)

    # Show config info if verbose
    if app_config.verbose or verbose:
        config_path = config_manager._config_path
        if config_path:
            if config_path.exists():
                print_info(f"Using config file: {config_path}")
            else:
                print_info(f"Config file not found: {config_path}")
                print_info("Using default configuration")
        else:
            print_info("Using default configuration")
        print_info("Verbose mode enabled")


if __name__ == "__main__":
    app()