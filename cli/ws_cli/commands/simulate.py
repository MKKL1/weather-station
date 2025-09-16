# ws_cli/commands/simulate.py
import asyncio
import random
import signal
import sys
import threading
import time  # Added for timing stats
from pathlib import Path
from typing import Optional, Dict

import typer
from rich.progress import Progress, SpinnerColumn, TextColumn

from ws_cli.core.device_manager import DeviceManager
from ws_cli.core.identity.azure_dps_provisioner import AzureDPSProvisioner
from ws_cli.core.publisher.azure_publisher import AzureDataPublisher
from ws_cli.core.publisher.data_publisher import DataPublisher
from ws_cli.core.publisher.fake_data_publisher import FakeDataPublisher
from ws_cli.core.simulation.sim_data_gen import SimulatedDataGenerator
from ws_cli.utils.console import print_info, print_success, print_error, print_warning

app = typer.Typer(help="Simulation commands")


def _create_transmitter(device, dry_run: bool, force_provision: bool = False,
                        progress=None, task_id=None) -> DataPublisher:
    """Factory function to create the appropriate transmitter based on dry_run flag."""
    if dry_run:
        return FakeDataPublisher(progress=progress, task_id=task_id)
    else:
        # Create Azure transmitter with DPS provisioner
        if not device.dps_config:
            raise ValueError("Device missing DPS configuration for Azure transmission")

        # Create shared provisioner for potential reuse
        provisioner = AzureDPSProvisioner()

        return AzureDataPublisher(
            device_cfg=device,
            provisioner=provisioner,
            progress=progress,
            task_id=task_id,
            force_provision=force_provision
        )


@app.command("once")
def simulate_once(
        device_id: Optional[str] = typer.Option(
            None,
            "--device-id",
            "-d",
            help="Device ID to use (defaults to configured default device)",
            autocompletion=lambda: ["sim-001", "sim-002", "dev-001"],
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

    async def send_telemetry_with_timeout(progress_, task_id):
        """Async function to handle the telemetry sending process with timeout."""
        # Create transmitter based on dry_run flag with progress support
        transmitter = _create_transmitter(device, dry_run, force_provision,
                                          progress=progress_, task_id=task_id)

        try:
            # Connect to the transmitter with timeout
            await asyncio.wait_for(transmitter.connect(), timeout=30.0)

            # Generate and send data with timeout
            generator = SimulatedDataGenerator()
            data = generator.generate(device)
            await asyncio.wait_for(transmitter.send(data), timeout=30.0)

        except asyncio.TimeoutError:
            print_error("Operation timed out")
            raise
        except Exception as e:
            # If Azure transmission fails, it might be due to stale cache
            if not dry_run and hasattr(transmitter, 'invalidate_cache_and_reconnect'):
                try:
                    print_warning("Transmission failed, attempting cache invalidation and retry...")
                    await asyncio.wait_for(transmitter.invalidate_cache_and_reconnect(), timeout=30.0)
                    # Retry once
                    data = generator.generate(device)
                    await asyncio.wait_for(transmitter.send(data), timeout=30.0)
                except Exception as retry_e:
                    print_error(f"Retry after cache invalidation also failed: {retry_e}")
                    raise retry_e
            else:
                raise e
        finally:
            # Always disconnect with timeout
            try:
                await asyncio.wait_for(transmitter.disconnect(), timeout=10.0)
            except asyncio.TimeoutError:
                print_warning("Disconnect timed out")

    try:
        # Get device
        device_manager = DeviceManager()
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

        print_info(f"Using device: [bold]{device.device_id}[/bold]")

        if dry_run:
            print_warning("DRY RUN MODE - No data will be sent")

        if force_provision:
            print_info("Force provisioning enabled - will ignore DPS cache")

        # Generate and send telemetry
        with Progress(
                SpinnerColumn(),
                TextColumn("[progress.description]{task.description}"),
                transient=True,
        ) as progress:
            task = progress.add_task(description="Initializing...", total=None)

            # Run the async telemetry sending with progress updates
            asyncio.run(send_telemetry_with_timeout(progress, task))

        print_success("✓ Telemetry sent successfully")

    except KeyboardInterrupt:
        print_warning("\nOperation cancelled by user")
        raise typer.Exit(130)
    except Exception as e:
        print_error(f"Failed to send telemetry: {e}")
        raise typer.Exit(1)


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


@app.command("loop")
def simulate_continuous(
        device_id: Optional[str] = typer.Option(
            None,
            "--device-id",
            "-d",
            help="Device ID to use",
            autocompletion=lambda: ["0", "1", "2"],
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
        ws-cli simulate continuous
        ws-cli simulate continuous --interval 60 --max-messages 10
        ws-cli simulate continuous --device-id sim-001 --seed 42
        ws-cli simulate continuous --force-provision  # Ignore DPS cache
    """

    try:
        # Get device
        device_manager = DeviceManager()
        if device_id:
            device = device_manager.get_device(device_id)
            if not device:
                print_error(f"Device '{device_id}' not found")
                raise typer.Exit(1)
        else:
            device = device_manager.get_default_device()
            if not device:
                print_error("No default device is set")
                raise typer.Exit(1)

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
        stats: Dict[str, float] = {'messages': 0, 'start_time': 0.0, 'elapsed': None}

        # Run simulation loop
        try:
            asyncio.run(_simulation_loop_with_cancellation(
                device=device,
                interval=interval,
                jitter=jitter,
                max_messages=max_messages,
                dry_run=dry_run,
                force_provision=force_provision,
                stats=stats,
            ))
            # Normal exit (max messages or 'q')
            print_info("Simulation completed!")
            print_info(f"Sent {int(stats['messages'])} messages in {format_elapsed(stats['elapsed'])}")
        except KeyboardInterrupt:
            # Cancellation (Ctrl+C)
            if stats['elapsed'] is None:
                stats['elapsed'] = time.time() - stats['start_time']
            print_info("Simulation canceled!")
            print_info(f"Sent {int(stats['messages'])} messages in {format_elapsed(stats['elapsed'])}")
            raise typer.Exit(0)

    except Exception as e:
        print_error(f"Simulation failed: {e}")
        raise typer.Exit(1)


async def _simulation_loop_with_cancellation(
        device,
        interval: int,
        jitter: float,
        max_messages: Optional[int],
        dry_run: bool,
        force_provision: bool = False,
        stats: Optional[Dict[str, float]] = None,  # New param for stats
):
    """Run the continuous simulation loop with proper cancellation handling."""
    from rich import print
    import asyncio
    import random
    import threading
    import sys

    messages_sent = 0
    generator = SimulatedDataGenerator()
    transmitter = None
    shutdown_event = asyncio.Event()

    # Set start time early
    if stats is not None:
        stats['start_time'] = time.time()

    # Key monitoring for 'q' key - Windows compatible
    def key_monitor():
        """Monitor for 'q' key press in a separate thread."""
        try:
            if sys.platform == "win32":
                import msvcrt
                while not shutdown_event.is_set():
                    if msvcrt.kbhit():
                        key = msvcrt.getch().decode('utf-8', errors='ignore')
                        if key.lower() == 'q':
                            shutdown_event.set()
                            break
                    threading.Event().wait(0.1)  # Small delay to prevent busy waiting
            else:
                # Unix-like systems
                import termios
                import tty
                import select

                old_settings = termios.tcgetattr(sys.stdin)
                tty.setraw(sys.stdin.fileno())

                while not shutdown_event.is_set():
                    if select.select([sys.stdin], [], [], 0.1) == ([sys.stdin], [], []):
                        key = sys.stdin.read(1)
                        if key.lower() == 'q':
                            shutdown_event.set()
                            break

                termios.tcsetattr(sys.stdin, termios.TCSADRAIN, old_settings)

        except (ImportError, OSError, Exception):
            # Fall back to no key monitoring if anything fails
            pass

    # Start key monitoring thread
    key_thread = None
    try:
        key_thread = threading.Thread(target=key_monitor, daemon=True)
        key_thread.start()
    except:
        pass  # Continue without key monitoring if it fails

    try:
        # Create transmitter and connect
        transmitter = _create_transmitter(device, dry_run, force_provision)
        await asyncio.wait_for(transmitter.connect(), timeout=30.0)

        while not shutdown_event.is_set():
            # Check message limit
            if max_messages and messages_sent >= max_messages:
                print(f"✓ Sent {messages_sent} messages (limit reached)")
                break

            try:
                # Generate and send telemetry
                data = generator.generate(device)
                await asyncio.wait_for(transmitter.send(data), timeout=30.0)
                messages_sent += 1
                if stats is not None:
                    stats['messages'] = float(messages_sent)  # Update stats

                if dry_run:
                    print(f"Message {messages_sent}: Generated (DRY RUN)")
                else:
                    print(f"Message {messages_sent}: Sent successfully")

            except asyncio.TimeoutError:
                print(f"Message {messages_sent + 1}: Timeout during send")
                continue
            except asyncio.CancelledError:
                break
            except Exception as e:
                print(f"Error sending message {messages_sent + 1}: {e}")

                # Try cache invalidation and reconnect for Azure
                if not dry_run and hasattr(transmitter, 'invalidate_cache_and_reconnect'):
                    try:
                        await asyncio.wait_for(
                            transmitter.invalidate_cache_and_reconnect(),
                            timeout=30.0
                        )
                        continue
                    except:
                        pass  # Continue with next message

            # Wait for next interval with cancellation check
            if (max_messages is None or messages_sent < max_messages) and not shutdown_event.is_set():
                jitter_amount = random.uniform(-jitter, jitter) if jitter > 0 else 0
                wait_time = max(1, interval + jitter_amount)

                # Sleep in small chunks to be responsive to cancellation
                remaining_time = wait_time
                while remaining_time > 0 and not shutdown_event.is_set():
                    sleep_chunk = min(0.1, remaining_time)  # Reduced for faster response (<0.1s max delay)
                    await asyncio.sleep(sleep_chunk)
                    remaining_time -= sleep_chunk

        # Normal exit: set elapsed
        if stats is not None:
            stats['elapsed'] = time.time() - stats['start_time']

    except asyncio.CancelledError:
        # Cancellation: set elapsed and re-raise to propagate
        if stats is not None:
            stats['elapsed'] = time.time() - stats['start_time']
        raise
    except Exception as e:
        print(f"Error in simulation loop: {e}")
        raise
    finally:
        # Cancel and await all pending tasks to retrieve any unhandled exceptions (e.g., from Azure SDK internals)
        loop = asyncio.get_running_loop()
        pending_tasks = asyncio.all_tasks(loop) - {asyncio.current_task(loop)}
        for task in pending_tasks:
            task.cancel()
        if pending_tasks:
            await asyncio.gather(*pending_tasks, return_exceptions=True)

        # Clean disconnect
        if transmitter:
            try:
                await asyncio.wait_for(transmitter.disconnect(), timeout=10.0)
            except Exception:
                pass  # Silent cleanup - suppress any disconnect errors

        shutdown_event.set()  # Ensure key monitoring thread stops


def _get_device(device_manager, device_id: Optional[str]):
    """Get device by ID or return default device."""
    if device_id:
        return device_manager.get_device(device_id)
    else:
        return device_manager.get_default_device()