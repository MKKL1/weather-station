import typer

from ws_cli.core.identity.provisioning_cache import ProvisioningCache

app = typer.Typer(help="Config commands")

@app.command("path")
def show_config_path():
    """
    Show configuration directory and possible config file paths (JSON and YAML).

    Examples:
    ws-cli config path
    """

    from pathlib import Path
    from ws_cli.core.config import ConfigManager
    from rich import print

    try:
        config_manager = ConfigManager()
        config_path = Path(config_manager.get_config_path())
        print(f"Config: {config_path} {'(exists)' if config_path.exists() else '(missing)'}")

        # Also show cache file location
        cache = ProvisioningCache(config_manager)
        cache_path = Path(str(cache._cache_file))
        print(f"DPS Cache: {cache_path} {'(exists)' if cache_path.exists() else '(missing)'}")

    except Exception as e:
        from ws_cli.utils.console import print_error
        print_error(f"Failed to determine config path: {e}")
        raise typer.Exit(1)