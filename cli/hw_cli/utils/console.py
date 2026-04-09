import os
import sys
from typing import List, Tuple

from rich import print as rich_print


def is_tty(file=sys.stderr) -> bool:
    """Check if the given file is an interactive terminal."""
    return hasattr(file, "isatty") and file.isatty()


def should_use_color(file=sys.stderr) -> bool:
    if os.getenv("NO_COLOR"):
        return False
    return is_tty(file)


def print_info(msg: str, file=sys.stderr):
    if should_use_color(file):
        rich_print(f"[blue]{msg}[/blue]", file=file)
    else:
        print(msg, file=file)


def print_success(msg: str, file=sys.stderr):
    if should_use_color(file):
        rich_print(f"[green]{msg}[/green]", file=file)
    else:
        print(msg, file=file)


def print_warning(msg: str, file=sys.stderr):
    if should_use_color(file):
        rich_print(f"[yellow]{msg}[/yellow]", file=file)
    else:
        print(msg, file=file)


def print_error(msg: str, file=sys.stderr):
    if should_use_color(file):
        rich_print(f"[red]error: {msg}[/red]", file=file)
    else:
        print(f"error: {msg}", file=file)


def print_table_header(columns: List[Tuple[str, int]], file=sys.stdout):
    header = "  ".join(f"{col:<{width}}" for col, width in columns)
    print(header, file=file)
    print("-" * len(header), file=file)


def print_table_row(values: List[str], widths: List[int], file=sys.stdout):
    row = "  ".join(f"{str(val):<{width}}" for val, width in zip(values, widths))
    print(row, file=file)
