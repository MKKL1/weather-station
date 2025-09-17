# hw_cli/commands/simulate.py
import asyncio
import random
import signal
import sys
import threading
import time
from pathlib import Path
from typing import Optional, Dict

import typer
from rich.progress import Progress, SpinnerColumn, TextColumn

from hw_cli.core.device_manager import DeviceManager
from hw_cli.core.publisher.azure_publisher import AzureDataPublisher
from hw_cli.core.publisher.fake_data_publisher import FakeDataPublisher
from hw_cli.core.simulation.sim_data_gen import SimulatedDataGenerator
from hw_cli.utils.console import print_info, print_success, print_error, print_warning

app = typer.Typer(help="Simulation commands")


def create_publisher(device, dry_run: bool, force_provision: bool = False,
                     progress=None, task_id=None):
    """Create appropriate publisher based on dry_run flag."""
    if dry_run:
        return FakeDataPublisher(progress=progress, task_id=task_id)
    else:
        if not device.dps_config:
            raise ValueError("Device missing DPS configuration for Azure transmission")

        return AzureDataPublisher(
            device_cfg=device,
            force_provision=force_provision,
            progress=progress,
            task_id=task_id
        )


@app.command("once")
def simulate_once(
        device_id: Optional[str] = typer.Option(
            None,
            "--device-id",
            "-d",
            help="Device ID to use (defaults to configured default device)",
        ),
        config: Optional[Path] = typer.Option(
            None,
            "--config",
            "-c",
            help="Configuration file",
            exists=True,
            file_okay=True,
            dir_okay=False,
        ),
        dry_run: bool = typer.Option(
            False,
            "--dry-run",
            help="Simulate without actually sending data",
        ),
        force_provision: bool = typer.Option(
            False,
            "--force-provision",
            help="Force re-provisioning even if cached DPS identity exists",
        ),
):
    """
    Send exactly one telemetry message and exit.

    Examples:
        ws-cli simulate once
        ws-cli simulate once --device-id sim-001 --dry-run
        ws-cli simulate once --force-provision  # Ignore DPS cache
    """

    async def send_once(progress_, task_id):
        """Send a single telemetry message."""
        publisher = create_publisher(device, dry_run, force_provision, progress_, task_id)
        generator = SimulatedDataGenerator()

        try:
            await asyncio.wait_for(publisher.connect(), timeout=30.0)
            data = generator.generate(device)
            await asyncio.wait_for(publisher.send(data), timeout=30.0)
        except asyncio.TimeoutError:
            print_error("Operation timed out")
            raise
        except Exception as e:
            # Try cache invalidation for Azure failures
            if not dry_run and hasattr(publisher, 'invalidate_cache_and_reconnect'):
                try:
                    print_warning("Transmission failed, attempting cache invalidation and retry...")
                    await asyncio.wait_for(publisher.invalidate_cache_and_reconnect(), timeout=30.0)
                    data = generator.generate(device)
                    await asyncio.wait_for(publisher.send(data), timeout=30.0)
                except Exception as retry_e:
                    print_error(f"Retry after cache invalidation failed: {retry_e}")
                    raise retry_e
            else:
                raise e
        finally:
            try:
                await asyncio.wait_for(publisher.disconnect(), timeout=10.0)
            except asyncio.TimeoutError:
                print_warning("Disconnect timed out")

    try:
        # Get device
        device_manager = DeviceManager()
        device = get_device(device_manager, device_id)

        print_info(f"Using device: [bold]{device.device_id}[/bold]")

        if dry_run:
            print_warning("DRY RUN MODE - No data will be sent")
        if force_provision:
            print_info("Force provisioning enabled - will ignore DPS cache")

        # Send telemetry with progress
        with Progress(
                SpinnerColumn(),
                TextColumn("[progress.description]{task.description}"),
                transient=True,
        ) as progress:
            task = progress.add_task(description="Initializing...", total=None)
            asyncio.run(send_once(progress, task))

        print_success("✓ Telemetry sent successfully")

    except KeyboardInterrupt:
        print_warning("\nOperation cancelled by user")
        raise typer.Exit(130)
    except Exception as e:
        print_error(f"Failed to send telemetry: {e}")
        raise typer.Exit(1)


@app.command("loop")
def simulate_continuous(
        device_id: Optional[str] = typer.Option(
            None,
            "--device-id",
            "-d",
            help="Device ID to use",
        ),
        interval: int = typer.Option(
            1800,
            "--interval",
            "-i",
            help="Interval between messages in seconds",
            min=1,
        ),
        jitter: float = typer.Option(
            5.0,
            "--jitter",
            "-j",
            help="Random jitter in seconds (0 to disable)",
            min=0.0,
        ),
        max_messages: Optional[int] = typer.Option(
            None,
            "--max-messages",
            "-m",
            help="Maximum number of messages to send",
            min=1,
        ),
        seed: Optional[int] = typer.Option(
            None,
            "--seed",
            "-s",
            help="Random seed for deterministic behavior",
        ),
        dry_run: bool = typer.Option(
            False,
            "--dry-run",
            help="Simulate without actually sending data",
        ),
        force_provision: bool = typer.Option(
            False,
            "--force-provision",
            help="Force re-provisioning even if cached DPS identity exists",
        ),
):
    """
    Run continuous simulation until stopped or limit reached.

    Examples:
        ws-cli simulate loop
        ws-cli simulate loop --interval 60 --max-messages 10
        ws-cli simulate loop --device-id sim-001 --seed 42
        ws-cli simulate loop --force-provision  # Ignore DPS cache
    """

    try:
        # Get device and setup
        device_manager = DeviceManager()
        device = get_device(device_manager, device_id)

        print_info(f"Starting continuous simulation for device: [bold]{device.device_id}[/bold]")
        print_info(f"Interval: {interval}s, Jitter: ±{jitter}s")

        if max_messages:
            print_info(f"Will send {max_messages} messages")
        else:
            print_info("Press Ctrl+C or 'q' to stop")

        if seed is not None:
            print_info(f"Using random seed: {seed}")
            random.seed(seed)

        if dry_run:
            print_warning("DRY RUN MODE - No data will be sent")
        if force_provision:
            print_info("Force provisioning enabled - will ignore DPS cache")

        # Stats tracking
        stats = {'messages': 0, 'start_time': 0.0, 'elapsed': None}

        # Run simulation loop
        try:
            asyncio.run(run_simulation_loop(
                device=device,
                interval=interval,
                jitter=jitter,
                max_messages=max_messages,
                dry_run=dry_run,
                force_provision=force_provision,
                stats=stats,
            ))
            print_info("Simulation completed!")
            print_info(f"Sent {int(stats['messages'])} messages in {format_elapsed(stats['elapsed'])}")
        except KeyboardInterrupt:
            if stats['elapsed'] is None:
                stats['elapsed'] = time.time() - stats['start_time']
            print_info("Simulation canceled!")
            print_info(f"Sent {int(stats['messages'])} messages in {format_elapsed(stats['elapsed'])}")
            raise typer.Exit(0)

    except Exception as e:
        print_error(f"Simulation failed: {e}")
        raise typer.Exit(1)


async def run_simulation_loop(device, interval: int, jitter: float,
                              max_messages: Optional[int], dry_run: bool,
                              force_provision: bool, stats: Dict):
    """Main simulation loop with cancellation handling."""
    from rich import print
    import threading
    import sys

    messages_sent = 0
    generator = SimulatedDataGenerator()
    publisher = None
    shutdown_event = asyncio.Event()

    stats['start_time'] = time.time()

    # Key monitoring thread
    key_thread = start_key_monitor(shutdown_event)

    try:
        # Create publisher and connect
        publisher = create_publisher(device, dry_run, force_provision)
        await asyncio.wait_for(publisher.connect(), timeout=30.0)

        while not shutdown_event.is_set():
            # Check message limit
            if max_messages and messages_sent >= max_messages:
                print(f"✓ Sent {messages_sent} messages (limit reached)")
                break

            try:
                # Generate and send
                data = generator.generate(device)
                await asyncio.wait_for(publisher.send(data), timeout=30.0)
                messages_sent += 1
                stats['messages'] = float(messages_sent)

                status = "Generated (DRY RUN)" if dry_run else "Sent successfully"
                print(f"Message {messages_sent}: {status}")

            except asyncio.TimeoutError:
                print(f"Message {messages_sent + 1}: Timeout during send")
                continue
            except asyncio.CancelledError:
                break
            except Exception as e:
                print(f"Error sending message {messages_sent + 1}: {e}")

                # Try cache invalidation for Azure failures
                if not dry_run and hasattr(publisher, 'invalidate_cache_and_reconnect'):
                    try:
                        await asyncio.wait_for(publisher.invalidate_cache_and_reconnect(), timeout=30.0)
                        continue
                    except:
                        pass

            # Wait for next interval with cancellation check
            if (max_messages is None or messages_sent < max_messages) and not shutdown_event.is_set():
                jitter_amount = random.uniform(-jitter, jitter) if jitter > 0 else 0
                wait_time = max(1, interval + jitter_amount)

                # Sleep in chunks to be responsive to cancellation
                remaining = wait_time
                while remaining > 0 and not shutdown_event.is_set():
                    sleep_chunk = min(0.1, remaining)
                    await asyncio.sleep(sleep_chunk)
                    remaining -= sleep_chunk

        stats['elapsed'] = time.time() - stats['start_time']

    except asyncio.CancelledError:
        stats['elapsed'] = time.time() - stats['start_time']
        raise
    except Exception as e:
        print(f"Error in simulation loop: {e}")
        raise
    finally:
        # Cleanup
        cleanup_tasks = asyncio.all_tasks() - {asyncio.current_task()}
        for task in cleanup_tasks:
            task.cancel()
        if cleanup_tasks:
            await asyncio.gather(*cleanup_tasks, return_exceptions=True)

        if publisher:
            try:
                await asyncio.wait_for(publisher.disconnect(), timeout=10.0)
            except Exception:
                pass

        shutdown_event.set()


def start_key_monitor(shutdown_event):
    """Start key monitoring thread for 'q' key press."""

    def key_monitor():
        try:
            if sys.platform == "win32":
                import msvcrt
                while not shutdown_event.is_set():
                    if msvcrt.kbhit():
                        key = msvcrt.getch().decode('utf-8', errors='ignore')
                        if key.lower() == 'q':
                            shutdown_event.set()
                            break
                    threading.Event().wait(0.1)
            else:
                import termios, tty, select
                old_settings = termios.tcgetattr(sys.stdin)
                tty.setraw(sys.stdin.fileno())

                while not shutdown_event.is_set():
                    if select.select([sys.stdin], [], [], 0.1) == ([sys.stdin], [], []):
                        key = sys.stdin.read(1)
                        if key.lower() == 'q':
                            shutdown_event.set()
                            break

                termios.tcsetattr(sys.stdin, termios.TCSADRAIN, old_settings)
        except:
            pass  # Continue without key monitoring if it fails

    try:
        thread = threading.Thread(target=key_monitor, daemon=True)
        thread.start()
        return thread
    except:
        return None


def get_device(device_manager, device_id: Optional[str]):
    """Get device by ID or return default device."""
    if device_id:
        device = device_manager.get_device(device_id)
        if not device:
            print_error(f"Device '{device_id}' not found")
            print_info("Run 'ws-cli devices list' to see available devices")
            raise typer.Exit(1)
    else:
        device = device_manager.get_default_device()
        if not device:
            print_error("No default device is set. Use --device-id or 'ws-cli devices set-default'")
            raise typer.Exit(1)

    return device


def format_elapsed(seconds: float) -> str:
    """Format elapsed time in a human-readable way."""
    if seconds < 60:
        return f"{int(seconds)}s"
    elif seconds < 3600:
        mins = int(seconds / 60)
        secs = int(seconds % 60)
        return f"{mins}m {secs}s"
    else:
        hours = int(seconds / 3600)
        mins = int((seconds % 3600) / 60)
        return f"{hours}h {mins}m"