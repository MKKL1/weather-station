import asyncio
import json
import random
import sys
import time
from typing import Optional

import typer
from rich import print as rich_print

from hw_cli.core.data_generator import DataGenerator
from hw_cli.core.device_manager import DeviceManager
from hw_cli.core.models import DeviceConfig
from hw_cli.utils.console import print_error, print_info, print_success, print_warning

app = typer.Typer(help="Simulation commands", no_args_is_help=True)


def _get_device(device_ref: Optional[str]) -> DeviceConfig:
    """Resolve device by name/ID or get default."""
    mgr = DeviceManager()
    if device_ref:
        # Try by name first
        device = mgr.get_device_by_name(device_ref)
        if device:
            return device

        # Try by device_id
        device = mgr.get_device_by_id(device_ref)
        if device:
            return device

        print_error(f"Device '{device_ref}' not found")
        raise typer.Exit(1)

    device = mgr.get_default_device()
    if not device:
        print_error("No default device. Use --device-id or 'hw devices set-default'")
        raise typer.Exit(1)
    return device


async def _ensure_registered(client, device: DeviceConfig) -> DeviceConfig:
    """Ensure device is registered, return updated config."""
    if device.is_registered:
        return device

    print_info("Device not registered, registering...")
    secret = await client.register()
    device.hmac_secret = secret
    DeviceManager().update_device(device)
    print_success("Device registered")
    return device


@app.command("once")
def simulate_once(
        device_id: Optional[str] = typer.Option(None, "--device-id", "-d", help="Device name or device_id"),
        dry_run: bool = typer.Option(False, "--dry-run", help="Print data without sending"),
        force_token: bool = typer.Option(False, "--force-token", help="Force new token (ignore cache)"),
        format: str = typer.Option("text", "--format", "-f", help="Output format: text or json"),
):
    """Send a single telemetry message."""
    import httpx
    from hw_cli.core.api.client import WeatherIoTClient

    if format not in ["text", "json"]:
        print_error("Format must be 'text' or 'json'")
        raise typer.Exit(1)

    async def run():
        device = _get_device(device_id)
        generator = DataGenerator()
        data = generator.generate(device)

        if dry_run:
            print_warning("DRY RUN - not sending")
            _print_telemetry(data, format)
            return

        async with WeatherIoTClient(device) as client:
            if force_token:
                client.invalidate_token()

            updated = await _ensure_registered(client, device)
            client.device = updated
            await client.send_telemetry(data)

        print_success("Telemetry sent")
        _print_telemetry(data, format)

    try:
        asyncio.run(run())
    except KeyboardInterrupt:
        print_warning("\nCancelled")
        raise typer.Exit(130)
    except httpx.HTTPStatusError as e:
        print_error(f"HTTP error {e.response.status_code}")
        raise typer.Exit(1)
    except Exception as e:
        print_error(f"Failed: {e}")
        raise typer.Exit(1)


@app.command("loop")
def simulate_loop(
        device_id: Optional[str] = typer.Option(None, "--device-id", "-d", help="Device name or device_id"),
        interval: int = typer.Option(1800, "--interval", "-i", help="Interval in seconds", min=1),
        jitter: float = typer.Option(5.0, "--jitter", "-j", help="Random jitter seconds", min=0),
        max_messages: Optional[int] = typer.Option(None, "--max", "-m", help="Max messages", min=1),
        seed: Optional[int] = typer.Option(None, "--seed", "-s", help="Random seed"),
        dry_run: bool = typer.Option(False, "--dry-run", help="Print without sending"),
        format: str = typer.Option("text", "--format", "-f", help="Output format: text or json"),
):
    """Run continuous telemetry simulation."""
    import httpx
    from hw_cli.core.api.client import WeatherIoTClient

    if jitter >= interval:
        print_error(f"Jitter ({jitter}s) must be less than interval ({interval}s)")
        raise typer.Exit(1)

    if format not in ["text", "json"]:
        print_error("Format must be 'text' or 'json'")
        raise typer.Exit(1)

    async def run():
        device = _get_device(device_id)
        generator = DataGenerator(seed=seed)
        sent_count = 0

        consecutive_errors = 0
        MAX_CONSECUTIVE_ERRORS = 5

        print_info(f"Device: {device.name} (device_id: {device.device_id})")
        print_info(f"Interval: {interval}s (±{jitter}s jitter)")
        if max_messages:
            print_info(f"Max messages: {max_messages}")
        print_info("Press Ctrl+C to stop\n")

        loop_start_time = time.time()

        while max_messages is None or sent_count < max_messages:
            if consecutive_errors >= MAX_CONSECUTIVE_ERRORS:
                print_error(f"Too many consecutive errors ({consecutive_errors}). Exiting.")
                sys.exit(1)

            # 1. Generate Data
            data = generator.generate(device)

            # 2. Send Data
            if dry_run:
                sent_count += 1
                print_info(f"[{sent_count}] Generated (dry run)")
                _print_telemetry(data, format)
                consecutive_errors = 0  # Reset on success
            else:
                try:
                    async with WeatherIoTClient(device) as client:
                        updated = await _ensure_registered(client, device)
                        client.device = updated
                        await client.send_telemetry(data)
                        sent_count += 1
                        consecutive_errors = 0  # Reset on success
                        print_info(f"[{sent_count}] Sent @ ts={data.timestamp}")
                except httpx.HTTPStatusError as e:
                    consecutive_errors += 1
                    print_error(
                        f"HTTP {e.response.status_code} (Errors: {consecutive_errors}/{MAX_CONSECUTIVE_ERRORS})")
                    if e.response.status_code == 401:
                        print_warning("Token might be invalid on next run")
                except Exception as e:
                    consecutive_errors += 1
                    print_error(f"Send failed: {e} (Errors: {consecutive_errors}/{MAX_CONSECUTIVE_ERRORS})")

            if max_messages and sent_count >= max_messages:
                break

            # 3. Calculate Sleep
            target_next_time = loop_start_time + (sent_count * interval)
            current_time = time.time()
            sleep_secs = target_next_time - current_time

            # Add jitter
            jitter_amount = random.uniform(-jitter, jitter)
            sleep_secs += jitter_amount

            if sleep_secs > 0:
                await asyncio.sleep(sleep_secs)
            else:
                print_warning(f"Processing took longer than interval, skipping sleep")

    try:
        asyncio.run(run())
    except KeyboardInterrupt:
        print("\r", end="")
        print_warning("Stopped by user")
        raise typer.Exit(130)
    except Exception as e:
        print_error(f"Fatal error: {e}")
        raise typer.Exit(1)


def _print_telemetry(data, format: str = "text"):
    """Print telemetry summary to stdout."""
    if format == "json":
        print(json.dumps(data.to_api_payload(), indent=2))
    else:
        r = data.reading
        rain_tips = sum(r.rain.data.values()) if r.rain and r.rain.data else 0
        print(f"ts: {data.timestamp}")
        print(f"temp: {r.temperature}°C, hum: {r.humidity}%, pres: {r.pressure} hPa")
        print(f"rain tips: {rain_tips}, precip: {r.precipitation_mm} mm")