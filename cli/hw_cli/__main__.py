import logging
import sys
from pathlib import Path
from typing import Optional

import click
import typer
from click.exceptions import UsageError, Exit
from click_repl import repl, ClickCompleter
from prompt_toolkit.history import FileHistory
from rich import print as rich_print

from hw_cli.commands import devices, simulate
from hw_cli.commands.cache import app as cache_app
from hw_cli.commands.config_cmd import app as config_app
from hw_cli.core.config import AppConfig
from hw_cli.core.config import load_config
from hw_cli.core.storage import get_app_dir

from importlib.metadata import version as get_package_version, PackageNotFoundError

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


class SafeCompleter(ClickCompleter):
    def get_completions(self, document, complete_event):
        try:
            yield from super().get_completions(document, complete_event)
        except (UsageError, Exit):
            pass


def _setup_logging(config: AppConfig):
    """Setup logging based on config object."""
    level = logging.DEBUG if config.verbose else getattr(logging, config.logging.level, logging.ERROR)

    logging.basicConfig(
        level=level,
        format=config.logging.format,
        datefmt=config.logging.date_format,
        force=True
    )

    logging.getLogger().setLevel(level)

def get_version() -> str:
    try:
        return get_package_version("heavyweather-cli")
    except PackageNotFoundError:
        # Fallback for local development if not installed via pip/poetry
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
            None, "--version", "-v", callback=version_callback, is_eager=True, help="Show version"
        ),
        config_file: Optional[Path] = typer.Option(
            None, "--config", "-c", help="Config file path", envvar="HW_CLI_CONFIG"
        ),
        verbose: bool = typer.Option(False, "--verbose", help="Enable verbose logging"),
):
    """Heavy Weather CLI - Weather station telemetry tool."""
    if ctx.resilient_parsing:
        return

    config = load_config(config_file)
    if verbose:
        config.verbose = True

    _setup_logging(config)
    ctx.obj = {
        "config": config,
        "verbose": config.verbose
    }


@app.command()
def console(
        ctx: typer.Context,
        config_file: Optional[Path] = typer.Option(
            None, "--config", "-c", help="Config file path", envvar="HW_CLI_CONFIG"
        ),
        verbose: bool = typer.Option(False, "--verbose", help="Enable verbose logging"),
):
    """Interactive CLI mode."""
    config = load_config(config_file)

    if verbose:
        config.verbose = True

    _setup_logging(config)

    rich_print("[bold blue]-------- Heavy Weather Console ------[/bold blue]", file=sys.stderr)
    rich_print("[cyan]Welcome to the interactive shell.[/cyan]", file=sys.stderr)
    rich_print("Type [bold green]help[/bold green] to see commands or [bold green]exit[/bold green] to quit.",
               file=sys.stderr)
    rich_print("-" * 37, file=sys.stderr)

    click_obj = typer.main.get_command(app)

    if not isinstance(click_obj, click.Group):
        rich_print("[red]Error: Unable to create interactive console[/red]", file=sys.stderr)
        raise typer.Exit(1)

    class ExitREPL(Exception):
        pass

    @click.command(name='exit')
    @click.pass_context
    def exit_cmd(ctx):
        """Exit the interactive shell."""
        rich_print("[cyan]Goodbye![/cyan]", file=sys.stderr)
        raise ExitREPL()

    click_obj.add_command(exit_cmd)
    click_obj.add_command(exit_cmd, name='quit')
    click_obj.add_command(exit_cmd, name='q')

    click_ctx = click.Context(
        click_obj,
        obj={"config": config, "verbose": config.verbose}
    )
    completer = SafeCompleter(click_obj, click_ctx)
    prompt_kwargs = {
        "prompt_kwargs": {
            "message": "hw> ",
            "completer": completer,
        }
    }
    app_dir = get_app_dir()
    app_dir.mkdir(parents=True, exist_ok=True)
    history_file = app_dir / ".hw_cli_history"

    try:
        prompt_kwargs["prompt_kwargs"]["history"] = FileHistory(str(history_file))
    except Exception:
        pass

    try:
        repl(click_ctx, prompt_kwargs=prompt_kwargs["prompt_kwargs"])
    except ExitREPL:
        pass
    except (EOFError, KeyboardInterrupt):
        rich_print("\n[cyan]Goodbye![/cyan]", file=sys.stderr)
    except Exception as e:
        rich_print(f"[red]Error in REPL: {e}[/red]", file=sys.stderr)
        raise typer.Exit(1)


if __name__ == "__main__":
    app()