import logging
import sys
from importlib.metadata import PackageNotFoundError
from importlib.metadata import version as get_package_version
from pathlib import Path
from typing import Optional

import typer
from rich import print as rich_print

from hw_cli.commands import devices, simulate
from hw_cli.commands.cache import app as cache_app
from hw_cli.commands.config_cmd import app as config_app
from hw_cli.core.config import AppConfig, load_config

logging.basicConfig(
    level=logging.ERROR,
    format="%(asctime)s %(levelname)-8s [%(name)s] %(message)s",
    datefmt="%H:%M:%S",
)

app = typer.Typer(
    name="hw",
    help="Heavy Weather CLI - Weather station telemetry tool",
    no_args_is_help=True,
)

app.add_typer(devices.app, name="devices", help="Device management")
app.add_typer(simulate.app, name="simulate", help="Simulation commands")
app.add_typer(cache_app, name="cache", help="Token cache management")
app.add_typer(config_app, name="config", help="Configuration management")


def _setup_logging(config: AppConfig):
    """Setup logging based on config object."""
    level = (
        logging.DEBUG
        if config.verbose
        else getattr(logging, config.logging.level, logging.ERROR)
    )

    logging.basicConfig(
        level=level,
        format=config.logging.format,
        datefmt=config.logging.date_format,
        force=True,
    )

    logging.getLogger().setLevel(level)


def get_version() -> str:
    try:
        return get_package_version("heavyweather-cli")
    except PackageNotFoundError:
        return "unknown"


def version_callback(value: bool):
    if value:
        app_version = get_version()
        rich_print(f"[green]Heavy Weather CLI v{app_version}[/green]", file=sys.stderr)
        raise typer.Exit()


@app.callback()
def main(
    ctx: typer.Context,
    version: Optional[bool] = typer.Option(
        None,
        "--version",
        "-v",
        callback=version_callback,
        is_eager=True,
        help="Show version",
    ),
    config_file: Optional[Path] = typer.Option(
        None, "--config", "-c", help="Config file path", envvar="HW_CLI_CONFIG"
    ),
    output: str = typer.Option(
        "text", "--output", "-o", help="Output format (text|json)"
    ),
    quiet: bool = typer.Option(
        False, "--quiet", "-q", help="Suppress informational output"
    ),
    verbose: bool = typer.Option(False, "--verbose", help="Enable verbose logging"),
    no_color: bool = typer.Option(False, "--no-color", help="Disable colors"),
):
    """Heavy Weather CLI - Weather station telemetry tool."""
    if ctx.resilient_parsing:
        return

    import os

    if os.getenv("NO_COLOR") or no_color or not sys.stderr.isatty():
        pass

    config = load_config(config_file)
    if verbose:
        config.verbose = True

    _setup_logging(config)
    ctx.obj = {
        "config": config,
        "verbose": config.verbose,
        "output": output,
        "quiet": quiet,
    }


if __name__ == "__main__":
    app()
