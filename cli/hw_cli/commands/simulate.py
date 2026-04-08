import asyncio
import json
import random
import sys
import time
from contextlib import asynccontextmanager
from dataclasses import asdict
from datetime import datetime
from typing import Any, Optional

import httpx
import rich
import rich.console
import typer
from rich import print as rich_print

from hw_cli.core.api.client import WeatherIoTClient
from hw_cli.core.data_generator import DataGenerator
from hw_cli.core.device_manager import DeviceManager
from hw_cli.utils.console import print_error, print_info, print_success, print_warning

app = typer.Typer(help="Simulation commands", no_args_is_help=True)


def _print_telemetry_summary(data: Any, format_type: str = "text") -> None:
    if format_type == "json":
        print(json.dumps(asdict(data), indent=2))
        return

    r = data.reading
    rain_tips = sum(r.rain.data.values()) if r.rain and r.rain.data else 0

    ts_str = datetime.fromtimestamp(data.timestamp).strftime("%Y-%m-%d %H:%M:%S")
    print(f"Timestamp:   {ts_str} ({data.timestamp})")
    print(f"Temperature: {r.temperature:.2f} C")
    print(f"Humidity:    {r.humidity:.1f} %")
    print(f"Pressure:    {r.pressure:.1f} hPa")
    print(f"Rain Tips:   {rain_tips}")


def _print_debug_info(
    req: httpx.Request, res: Optional[httpx.Response], format_type: str
) -> None:
    from rich.console import Group
    from rich.panel import Panel
    from rich.rule import Rule
    from rich.syntax import Syntax

    debug_data = {
        "request": {
            "method": req.method,
            "url": str(req.url),
            "headers": dict(req.headers),
            "body": None,
        },
        "response": None,
    }

    try:
        if req.content:
            debug_data["request"]["body"] = json.loads(req.content)
    except (json.JSONDecodeError, UnicodeDecodeError):
        debug_data["request"]["body"] = (
            req.content.decode("utf-8", errors="replace") if req.content else None
        )

    if res:
        debug_data["response"] = {
            "status_code": res.status_code,
            "headers": dict(res.headers),
            "body": None,
        }
        try:
            debug_data["response"]["body"] = res.json()
        except (json.JSONDecodeError, ValueError):
            debug_data["response"]["body"] = res.text

    if format_type == "json":
        print(json.dumps(debug_data))
        return

    def render_body(content: Any, style_theme: str = "monokai") -> Any:
        if isinstance(content, (dict, list)):
            return Syntax(
                json.dumps(content, indent=2), "json", theme=style_theme, word_wrap=True
            )
        return str(content) if content else "[dim]Empty Body[/dim]"

    req_group = Group(
        f"[bold]{req.method}[/bold] [green]{req.url}[/green]",
        Rule(style="dim"),
        Panel(
            render_body(debug_data["request"]["body"]),
            title="Request Body",
            border_style="blue",
        ),
    )
    rich_print(Panel(req_group, title="HTTP REQUEST", expand=True), file=sys.stderr)

    if res:
        status_color = "green" if res.status_code < 400 else "red"
        res_group = Group(
            f"Status: [bold {status_color}]{res.status_code} {res.reason_phrase}[/bold {status_color}]",
            Rule(style="dim"),
            Panel(
                render_body(debug_data["response"]["body"]),
                title="Response Body",
                border_style=status_color,
            ),
        )
        rich_print(
            Panel(res_group, title="HTTP RESPONSE", expand=True), file=sys.stderr
        )


@asynccontextmanager
async def _debug_hooks(client: WeatherIoTClient, enabled: bool):
    captured = {"req": [], "res": []}

    if not enabled or not client._client:
        yield captured
        return

    async def on_request(request: httpx.Request):
        captured["req"].append(request)

    async def on_response(response: httpx.Response):
        await response.aread()
        captured["res"].append(response)

    client._client.event_hooks["request"].append(on_request)
    client._client.event_hooks["response"].append(on_response)

    try:
        yield captured
    finally:
        if client._client:
            try:
                client._client.event_hooks["request"].remove(on_request)
                client._client.event_hooks["response"].remove(on_response)
            except ValueError:
                pass


@app.command("once")
def simulate_once(
    ctx: typer.Context,
    device: Optional[str] = typer.Option(
        None,
        "--device",
        "--name",
        "-n",
        "--device-id",
        "-d",
        help="Device name or device_id",
    ),
    dry_run: bool = typer.Option(False, "--dry-run", help="Print data without sending"),
    force_token: bool = typer.Option(
        False, "--force-token", help="Force new token (ignore cache)"
    ),
    format: str = typer.Option(
        "text", "--format", "-f", help="Output format: text or json"
    ),
    debug: bool = typer.Option(
        False, "--debug", help="Print full request/response headers and body"
    ),
):
    if format not in ["text", "json"]:
        print_error("Format must be 'text' or 'json'")
        raise typer.Exit(1)

    async def run():
        mgr = DeviceManager()
        device_obj = mgr.resolve_device(device)
        output_format = ctx.obj["output"]
        quiet = ctx.obj["quiet"]

        if not device_obj:
            print_error("Device not found or no default set")
            raise typer.Exit(1)

        if not device_obj.is_registered:
            print_error(
                f"Device '{device_obj.name}' not registered. Run 'hw devices register {device_obj.name}' first."
            )
            raise typer.Exit(1)

        generator = DataGenerator()
        data = generator.generate(device_obj)

        if dry_run:
            if not quiet and format == "text":
                print_info("(Dry Run)", file=sys.stderr)
            _print_telemetry_summary(data, format)
            return

        try:
            async with WeatherIoTClient(device_obj) as client:
                if force_token:
                    client.invalidate_token()

                use_spinner = (not debug) and (format == "text") and (not quiet)

                async with _debug_hooks(client, debug) as captured:
                    if use_spinner:
                        from rich.progress import Progress, SpinnerColumn, TextColumn

                        with Progress(
                            SpinnerColumn(),
                            TextColumn("{task.description}"),
                            transient=True,
                            console=rich.console.Console(stderr=True),
                        ) as progress:
                            progress.add_task("Sending telemetry...", total=None)
                            await client.send_telemetry(data)
                    else:
                        await client.send_telemetry(data)

                if debug and captured["req"]:
                    _print_debug_info(
                        captured["req"][-1],
                        captured["res"][-1] if captured["res"] else None,
                        format,
                    )
                else:
                    if not quiet and format == "text":
                        print_success("Telemetry sent", file=sys.stderr)
                    _print_telemetry_summary(data, format)

        except httpx.HTTPStatusError as e:
            if debug and "captured" in locals():
                req = captured["req"][-1] if captured["req"] else e.request
                res = captured["res"][-1] if captured["res"] else e.response
                _print_debug_info(req, res, format)
            else:
                if e.response.status_code == 401:
                    print_error(
                        f"HTTP 401 - token may be invalid. Run: hw cache clear -d {device_obj.name}"
                    )
                else:
                    print_error(f"HTTP {e.response.status_code}: {e.response.text}")
            raise typer.Exit(1)
        except Exception as e:
            print_error(f"Failed: {e}")
            raise typer.Exit(1)

    try:
        asyncio.run(run())
    except KeyboardInterrupt:
        print_warning("\nOperation cancelled")
        raise typer.Exit(130)


@app.command("loop")
def simulate_loop(
    ctx: typer.Context,
    device: Optional[str] = typer.Option(
        None,
        "--device",
        "--name",
        "-n",
        "--device-id",
        "-d",
        help="Device name or device_id",
    ),
    interval: int = typer.Option(
        300, "--interval", "-i", help="Interval in seconds", min=1
    ),
    jitter: float = typer.Option(
        5.0, "--jitter", "-j", help="Random jitter seconds", min=0
    ),
    max_messages: Optional[int] = typer.Option(
        None, "--max-messages", "-m", help="Max messages to send", min=1
    ),
    seed: Optional[int] = typer.Option(
        None, "--seed", "-s", help="Random seed for deterministic data"
    ),
    dry_run: bool = typer.Option(False, "--dry-run", help="Print without sending"),
    force_token: bool = typer.Option(
        False, "--force-token", help="Force new token (ignore cache)"
    ),
    format: str = typer.Option(
        "text", "--format", "-f", help="Output format: text or json"
    ),
    debug: bool = typer.Option(False, "--debug", help="Print full request/response"),
):
    """Run continuous telemetry simulation."""
    if jitter >= interval:
        print_error(f"Jitter ({jitter}s) must be less than interval ({interval}s)")
        raise typer.Exit(1)

    async def run():
        mgr = DeviceManager()
        device_obj = mgr.resolve_device(device)
        quiet = ctx.obj["quiet"]

        if not device_obj:
            print_error("Device not found or no default set")
            raise typer.Exit(1)

        if not device_obj.is_registered:
            print_error(
                f"Device '{device_obj.name}' not registered. Run 'hw devices register {device_obj.name}' first."
            )
            raise typer.Exit(1)

        generator = DataGenerator(seed=seed)

        stats = {"sent": 0, "errors": 0, "start_time": time.time()}
        consecutive_errors = 0
        MAX_CONSECUTIVE_ERRORS = 5

        if not quiet:
            print_info(f"Starting simulation for: {device_obj.name}", file=sys.stderr)
            print_info(f"Interval: {interval}s (+/-{jitter}s jitter)", file=sys.stderr)

        async with WeatherIoTClient(device_obj) as client:
            if force_token:
                client.invalidate_token()

            while max_messages is None or stats["sent"] < max_messages:
                data = generator.generate(device_obj)

                try:
                    if dry_run:
                        if format == "text":
                            if not quiet:
                                print(
                                    f"[{stats['sent']}] Generated @ {data.timestamp}",
                                    file=sys.stderr,
                                )
                            print(f"{data.timestamp} | Generated (Dry Run)")
                        elif format == "json":
                            print(json.dumps(asdict(data)))
                    else:
                        async with _debug_hooks(client, debug) as captured:
                            await client.send_telemetry(data)

                        stats["sent"] += 1
                        consecutive_errors = 0

                        if debug and captured["req"]:
                            _print_debug_info(
                                captured["req"][-1],
                                captured["res"][-1] if captured["res"] else None,
                                format,
                            )
                        elif not debug:
                            if format == "text":
                                if not quiet:
                                    print(
                                        f"[{stats['sent']}] Sent @ {data.timestamp}",
                                        file=sys.stderr,
                                    )
                                print(f"{data.timestamp} | Sent")
                            elif format == "json":
                                print(json.dumps(asdict(data)))

                except httpx.HTTPStatusError as e:
                    consecutive_errors += 1
                    stats["errors"] += 1

                    if debug and "captured" in locals():
                        req = captured["req"][-1] if captured["req"] else e.request
                        res = captured["res"][-1] if captured["res"] else e.response
                        _print_debug_info(req, res, format)
                    else:
                        if e.response.status_code == 401:
                            print_error(
                                f"HTTP 401 - token may be invalid. Run: hw cache clear -d {device_obj.name}"
                            )
                        else:
                            print_error(f"HTTP {e.response.status_code}")

                    if consecutive_errors >= MAX_CONSECUTIVE_ERRORS:
                        print_error(
                            f"Aborting: {MAX_CONSECUTIVE_ERRORS} consecutive errors."
                        )
                        break
                except Exception as e:
                    consecutive_errors += 1
                    stats["errors"] += 1
                    print_error(f"Failed: {e}")

                    if consecutive_errors >= MAX_CONSECUTIVE_ERRORS:
                        print_error(
                            f"Aborting: {MAX_CONSECUTIVE_ERRORS} consecutive errors."
                        )
                        break

                if max_messages and stats["sent"] >= max_messages:
                    break

                target_timestamp = stats["start_time"] + (stats["sent"] * interval)
                target_timestamp += random.uniform(-jitter, jitter)

                now = time.time()
                sleep_secs = max(0.1, target_timestamp - now)

                remaining = sleep_secs
                while remaining > 0:
                    chunk = min(0.1, remaining)
                    await asyncio.sleep(chunk)
                    remaining -= chunk

        elapsed = time.time() - stats["start_time"]
        if not quiet:
            print_info("\nStopped", file=sys.stderr)
            print(
                f"Sent: {stats['sent']} | Errors: {stats['errors']} | Time: {elapsed:.1f}s",
                file=sys.stderr,
            )

    try:
        asyncio.run(run())
    except KeyboardInterrupt:
        pass
    except Exception as e:
        print_error(f"Fatal: {e}")
        raise typer.Exit(1)
