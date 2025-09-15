import typer
app = typer.Typer(help="Config commands")

@app.command("path")
def show_config_path():
    """
    Show configuration directory and possible config file paths (JSON and YAML).

    Examples:
    ws-cli config path
    """

    from pathlib import Path
    from ws_cli.config import ConfigManager
    from rich import print

    try:
        config_manager = ConfigManager()
        config_path = Path(config_manager.get_config_path())
        print(f"Config: {config_path} {'(exists)' if config_path.exists() else '(missing)'}")
    except Exception as e:
        from ws_cli.utils.console import print_error
        print_error(f"Failed to determine config path: {e}")
        raise typer.Exit(1)