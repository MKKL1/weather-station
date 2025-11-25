import sys
from rich import print as rich_print


def print_info(msg: str):
    """Print info message to stderr."""
    rich_print(f"[blue]INFO[/blue]: {msg}", file=sys.stderr)


def print_success(msg: str):
    """Print success message to stderr."""
    rich_print(f"[green]SUCCESS[/green]: {msg}", file=sys.stderr)


def print_warning(msg: str):
    """Print warning message to stderr."""
    rich_print(f"[yellow]WARNING[/yellow]: {msg}", file=sys.stderr)


def print_error(msg: str):
    """Print error message to stderr."""
    rich_print(f"[red]ERROR[/red]: {msg}", file=sys.stderr)