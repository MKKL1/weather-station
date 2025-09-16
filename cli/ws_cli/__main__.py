import logging
import sys

import typer
from typer import Context
from typing import Optional
from pathlib import Path

if any(arg == "?" for arg in sys.argv[1:]):
    sys.argv = [sys.argv[0]] + ["--help" if arg == "?" else arg for arg in sys.argv[1:]]

from ws_cli.commands import simulate, devices, config as config_cmd, enrollment
from ws_cli.commands.cache import cache_app

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
app.add_typer(config_cmd.app, name="config", help="Config management commands")
app.add_typer(cache_app, name="cache", help="Cache management commands")
app.add_typer(enrollment.app, name="enrollment", help="Enrollment management commands")

def _configure_logging(verbose: bool) -> None:
    """Set up the root logger (safe to call multiple times)."""
    root = logging.getLogger()
    # If no handlers exist, create a basic console handler.
    if not root.handlers:
        handler = logging.StreamHandler()
        formatter = logging.Formatter(
            "%(asctime)s %(levelname)-8s [%(name)s] %(message)s",
            datefmt="%Y-%m-%d %H:%M:%S",
        )
        handler.setFormatter(formatter)
        root.addHandler(handler)
    # Default to WARNING when not verbose to avoid library INFO spam.
    root.setLevel(logging.DEBUG if verbose else logging.WARNING)

def verbose_callback(ctx: typer.Context, param, value: bool):
    # Click/Typer often supplies `ctx.resilient_parsing` while
    # building shell completion or showing help; ignore in that case.
    if ctx.resilient_parsing:
        return value
    _configure_logging(value)
    # keep the value so we can save it into ctx.obj in the main callback
    return value

def version_callback(value: bool):
    if value:
        from ws_cli.utils.console import print_success
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
            help="Global configuration file",
            envvar="WS_CLI_CONFIG",
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
    from ws_cli.utils.console import print_info
    if ctx.obj is None:
        ctx.obj = {}
    ctx.obj["verbose"] = verbose
    _configure_logging(verbose)

    if config:
        print_info(f"Using config file: {config}")
        # TODO: Load global config

    if verbose:
        print_info("Verbose mode enabled")
        # TODO: Set logging level


if __name__ == "__main__":
    app()