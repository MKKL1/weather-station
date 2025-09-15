import typer
from typing import Optional
from pathlib import Path

from ws_cli.commands import simulate, devices, config as config_cmd

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
app.add_typer(config_cmd.app, name="config", help="Config commands")

def version_callback(value: bool):
    if value:
        from ws_cli.utils.console import print_success
        print_success("Weather Station CLI v0.1.0")
        raise typer.Exit()

@app.callback()
def main(
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
        ),
):
    """
    Weather Station CLI - Simulate weather telemetry for Azure IoT Hub
    """
    from ws_cli.utils.console import print_info

    if config:
        print_info(f"Using config file: {config}")
        # TODO: Load global config

    if verbose:
        print_info("Verbose mode enabled")
        # TODO: Set logging level


if __name__ == "__main__":
    app()