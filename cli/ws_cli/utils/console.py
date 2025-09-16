"""
Weather Station CLI - Console Output Utilities
"""
from rich import print

def print_info(message: str):
    """Print an informational message."""
    print(f"[bold blue]INFO[/bold blue]: {message}")

def print_success(message: str):
    """Print a success message."""
    print(f"[bold green]SUCCESS[/bold green]: {message}")

def print_warning(message: str):
    """Print a warning message."""
    print(f"[bold yellow]WARNING[/bold yellow]: {message}")

def print_error(message: str):
    """Print an error message."""
    print(f"[bold red]ERROR[/bold red]: {message}")