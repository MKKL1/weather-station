import asyncio
import json
import random
import time
from contextlib import asynccontextmanager
from typing import Optional, Any

import httpx
import typer
from rich import print as rich_print
from rich.console import Group
from rich.panel import Panel
from rich.progress import Progress, SpinnerColumn, TextColumn
from rich.rule import Rule
from rich.syntax import Syntax
from rich.table import Table

from hw_cli.core.api.client import WeatherIoTClient
from hw_cli.core.data_generator import DataGenerator
from hw_cli.core.device_manager import DeviceManager
from hw_cli.core.models import DeviceConfig
from hw_cli.utils.console import print_error, print_info, print_success, print_warning

app = typer.Typer(help="Simulation commands", no_args_is_help=True)


def _get_device(device_ref: Optional[str]) -> DeviceConfig:
    """Resolve device by name/ID or get default."""
    mgr = DeviceManager()
    if device_ref:
        device = mgr.get_device_by_name(device_ref) or mgr.get_device_by_id(device_ref)
        if device:
            return device
        print_error(f"Device '{device_ref}' not found")
        raise typer.Exit(1)

    device = mgr.get_default_device()
    if not device:
        print_error("No default device. Use --device-id or 'hw devices set-default'")
        raise typer.Exit(1)
    return device


def _patch_data_for_api(data: Any) -> Any:
    """
    Ensure generated data passes strict API validation.
    Patches empty rain histograms which cause HTTP 400 errors.
    """
    if data.reading.rain is not None:
        if not data.reading.rain.data:
            data.reading.rain.data = {"0": 0}
    return data


def _print_telemetry_table(data: Any, format_type: str = "text") -> None:
    """Print telemetry data summary in the requested format."""
    if format_type == "json":
        rich_print(json.dumps(data.to_api_payload(), indent=2))
        return

    r = data.reading
    rain_tips = sum(r.rain.data.values()) if r.rain and r.rain.data else 0

    table = Table(show_header=True, header_style="bold magenta", box=None)
    table.add_column("Metric", style="dim")
    table.add_column("Value")
    table.add_column("Unit")

    table.add_row("Timestamp", str(data.timestamp), "")
    table.add_row("Temperature", f"{r.temperature:.2f}", "°C")
    table.add_row("Humidity", f"{r.humidity:.1f}", "%")
    table.add_row("Pressure", f"{r.pressure:.1f}", "hPa")
    table.add_row("Rain Tips", str(rain_tips), "count")

    rich_print(Panel(table, title="Telemetry Summary", expand=False))


def _print_debug_info(req: httpx.Request, res: Optional[httpx.Response], format_type: str) -> None:
    """
    Print raw request/response details with strict separation of Headers and Body.
    Handles JSON decoding safely and falls back to text/string representations.
    """
    debug_data = {
        "request": {
            "method": req.method,
            "url": str(req.url),
            "headers": dict(req.headers),
            "body": None
        },
        "response": None
    }

    try:
        if req.content:
            debug_data["request"]["body"] = json.loads(req.content)
        else:
            debug_data["request"]["body"] = None
    except (json.JSONDecodeError, UnicodeDecodeError):
        debug_data["request"]["body"] = req.content.decode('utf-8', errors='replace') if req.content else None

    if res:
        debug_data["response"] = {
            "status_code": res.status_code,
            "headers": dict(res.headers),
            "body": None
        }
        try:
            debug_data["response"]["body"] = res.json()
        except (json.JSONDecodeError, ValueError):
            debug_data["response"]["body"] = res.text

    if format_type == "json":
        rich_print(json.dumps(debug_data))
        return

    def render_body(content: Any, style_theme: str = "monokai") -> Any:
        if isinstance(content, (dict, list)):
            return Syntax(json.dumps(content, indent=2), "json", theme=style_theme, word_wrap=True)
        return str(content) if content else "[dim]Empty Body[/dim]"

    req_headers = "\n".join([f"[cyan]{k}[/cyan]: {v}" for k, v in debug_data["request"]["headers"].items()])
    req_group = Group(
        f"[bold]{req.method}[/bold] [green]{req.url}[/green]",
        Rule(style="dim"),
        Panel(req_headers, title="Request Headers", border_style="dim"),
        Panel(render_body(debug_data["request"]["body"]), title="Request Body", border_style="blue"),
    )
    rich_print(Panel(req_group, title="[bold blue]HTTP REQUEST[/bold blue]", expand=True))

    if res:
        status_color = "green" if res.status_code < 400 else "red"
        res_headers = "\n".join([f"[cyan]{k}[/cyan]: {v}" for k, v in debug_data["response"]["headers"].items()])
        res_group = Group(
            f"Status: [bold {status_color}]{res.status_code} {res.reason_phrase}[/bold {status_color}]",
            Rule(style="dim"),
            Panel(res_headers, title="Response Headers", border_style="dim"),
            Panel(render_body(debug_data["response"]["body"]), title="Response Body", border_style=status_color),
        )
        rich_print(Panel(res_group, title=f"[bold {status_color}]HTTP RESPONSE[/bold {status_color}]", expand=True))
    else:
        rich_print(Panel("[dim]No response received[/dim]", title="HTTP RESPONSE", border_style="dim"))


@asynccontextmanager
async def _debug_hooks(client: WeatherIoTClient, enabled: bool):
    """
    Context manager to attach/detach debug hooks to the underlying HTTP client.
    """
    captured = {"req": [], "res": []}

    #TODO access thru getter
    if not enabled or not client._client:
        yield captured
        return

    async def on_request(request: httpx.Request):
        captured["req"].append(request)

    async def on_response(response: httpx.Response):
        # Must read stream to ensure body is available for printing
        await response.aread()
        captured["res"].append(response)

    client._client.event_hooks['request'].append(on_request)
    client._client.event_hooks['response'].append(on_response)

    try:
        yield captured
    finally:
        if client._client:
            try:
                client._client.event_hooks['request'].remove(on_request)
                client._client.event_hooks['response'].remove(on_response)
            except ValueError:
                pass


async def _ensure_registered(client: WeatherIoTClient, device: DeviceConfig,
                             device_manager: DeviceManager, progress: Optional[Progress] = None) -> DeviceConfig:
    if device.is_registered:
        return device

    msg = "Device not registered, registering..."
    if progress:
        progress.update(progress.task_ids[0], description=msg)
    else:
        print_info(msg)

    secret = await client.register()
    device.hmac_secret = secret
    device_manager.update_device(device)
    client.update_device(device)

    if not progress:
        print_success("Device registered successfully")
    return device


async def _send_telemetry_safe(
        client: WeatherIoTClient,
        data: Any,
        debug: bool,
        format_type: str,
        dry_run: bool = False,
        quiet: bool = False,
) -> None:
    if dry_run:
        if debug:
            req = httpx.Request("POST", f"{client.device.api_base_url}/telemetry", json=data.to_api_payload())
            _print_debug_info(req, None, format_type)
        elif not quiet:
            if format_type == "json":
                _print_telemetry_table(data, "json")
            else:
                rich_print("[yellow]Generated (Dry Run)[/yellow]")
                _print_telemetry_table(data, "text")
        return

    async with _debug_hooks(client, debug) as captured:
        try:
            await client.send_telemetry(data)

            if debug and captured["req"]:
                _print_debug_info(captured["req"][-1], captured["res"][-1] if captured["res"] else None, format_type)
            elif not debug and not quiet:  # Only print if not quiet
                if format_type == "text":
                    print_success("✓ Telemetry sent successfully")
                    _print_telemetry_table(data, "text")
                else:
                    _print_telemetry_table(data, "json")

        except httpx.HTTPStatusError as e:
            if debug:
                req = captured["req"][-1] if captured["req"] else e.request
                res = captured["res"][-1] if captured["res"] else e.response
                _print_debug_info(req, res, format_type)
            else:
                print_error(f"HTTP Error {e.response.status_code}: {e.response.text}")
            raise


@app.command("once")
def simulate_once(
        device_id: Optional[str] = typer.Option(None, "--device-id", "-d", help="Device name or device_id"),
        dry_run: bool = typer.Option(False, "--dry-run", help="Print data without sending"),
        force_token: bool = typer.Option(False, "--force-token", help="Force new token (ignore cache)"),
        format: str = typer.Option("text", "--format", "-f", help="Output format: text or json"),
        debug: bool = typer.Option(False, "--debug", help="Print full request/response headers and body"),
):
    """
    Send a single telemetry message and exit.
    """
    if format not in ["text", "json"]:
        print_error("Format must be 'text' or 'json'")
        raise typer.Exit(1)

    async def run():
        device = _get_device(device_id)
        device_manager = DeviceManager()
        generator = DataGenerator()

        print_info(f"Using device: [bold]{device.name}[/bold] ({device.device_id})")
        data = _patch_data_for_api(generator.generate(device))

        try:
            use_spinner = (not debug) and (format == "text") and (not dry_run)

            async with WeatherIoTClient(device) as client:
                if force_token:
                    client.invalidate_token()

                if use_spinner:
                    with Progress(SpinnerColumn(), TextColumn("{task.description}"), transient=True) as progress:
                        task = progress.add_task("Connecting...", total=None)
                        await _ensure_registered(client, device, device_manager, progress)
                        progress.update(task, description="Sending telemetry...")

                        await _send_telemetry_safe(client, data, debug, format, dry_run, quiet=True)

                    print_success("✓ Telemetry sent successfully")
                    _print_telemetry_table(data, format)
                else:
                    if not dry_run:
                        await _ensure_registered(client, device, device_manager)
                    await _send_telemetry_safe(client, data, debug, format, dry_run, quiet=False)

        except httpx.HTTPStatusError:
            raise typer.Exit(1)
        except Exception as e:
            print_error(f"Failed: {e}")
            raise typer.Exit(1)

    try:
        asyncio.run(run())
    except KeyboardInterrupt:
        print_warning("\nOperation cancelled by user")
        raise typer.Exit(130)


@app.command("loop")
def simulate_loop(
        device_id: Optional[str] = typer.Option(None, "--device-id", "-d", help="Device name or device_id"),
        interval: int = typer.Option(1800, "--interval", "-i", help="Interval in seconds", min=1),
        jitter: float = typer.Option(5.0, "--jitter", "-j", help="Random jitter seconds", min=0),
        max_messages: Optional[int] = typer.Option(None, "--max-messages", "-m", help="Max messages to send", min=1),
        seed: Optional[int] = typer.Option(None, "--seed", "-s", help="Random seed for deterministic data"),
        dry_run: bool = typer.Option(False, "--dry-run", help="Print without sending"),
        force_token: bool = typer.Option(False, "--force-token", help="Force new token (ignore cache)"),
        format: str = typer.Option("text", "--format", "-f", help="Output format: text or json"),
        debug: bool = typer.Option(False, "--debug", help="Print full request/response"),
):
    """
    Run continuous telemetry simulation.
    """
    if jitter >= interval:
        print_error(f"Jitter ({jitter}s) must be less than interval ({interval}s)")
        raise typer.Exit(1)

    async def run():
        device = _get_device(device_id)
        device_manager = DeviceManager()
        generator = DataGenerator(seed=seed)

        stats = {'sent': 0, 'errors': 0, 'start_time': time.time()}
        consecutive_errors = 0
        MAX_CONSECUTIVE_ERRORS = 5

        print_info(f"Starting continuous simulation for: [bold]{device.name}[/bold]")
        print_info(f"Interval: {interval}s (±{jitter}s jitter)")
        if debug:
            print_warning("DEBUG MODE ENABLED: Output will be verbose")

        async with WeatherIoTClient(device) as client:
            if force_token:
                client.invalidate_token()

            if not dry_run:
                try:
                    await _ensure_registered(client, device, device_manager)
                except Exception as e:
                    print_error(f"Initial registration failed: {e}")
                    raise typer.Exit(1)

            while max_messages is None or stats['sent'] < max_messages:
                data = _patch_data_for_api(generator.generate(device))

                try:
                    await _send_telemetry_safe(client, data, debug, format, dry_run, quiet=True)

                    stats['sent'] += 1
                    consecutive_errors = 0

                    if not debug:
                        if format == "text":
                            status = "[green]✓ Sent[/green]" if not dry_run else "[yellow]Generated (Dry Run)[/yellow]"
                            rich_print(f"[dim][{stats['sent']}][/dim] {status} @ {data.timestamp}")
                        elif format == "json":
                            rich_print(json.dumps(data.to_api_payload(), indent=None))

                except (httpx.HTTPStatusError, Exception) as e:
                    consecutive_errors += 1
                    stats['errors'] += 1

                    if not debug:
                        print_error(f"Send failed: {e}")

                    if consecutive_errors >= MAX_CONSECUTIVE_ERRORS:
                        print_error(f"Aborting: {MAX_CONSECUTIVE_ERRORS} consecutive errors.")
                        break

                if max_messages and stats['sent'] >= max_messages:
                    break

                target_timestamp = stats['start_time'] + (stats['sent'] * interval)
                target_timestamp += random.uniform(-jitter, jitter)

                now = time.time()
                sleep_secs = target_timestamp - now

                if sleep_secs < 0:
                    sleep_secs = 0.1

                remaining = sleep_secs
                while remaining > 0:
                    chunk = min(0.1, remaining)
                    await asyncio.sleep(chunk)
                    remaining -= chunk

        elapsed = time.time() - stats['start_time']
        print_info("\nSimulation stopped!")
        rich_print(f"Sent: [bold]{stats['sent']}[/bold] | Errors: [red]{stats['errors']}[/red] | Time: {elapsed:.1f}s")

    try:
        asyncio.run(run())
    except KeyboardInterrupt:
        pass
    except Exception as e:
        print_error(f"Fatal error: {e}")
        raise typer.Exit(1)