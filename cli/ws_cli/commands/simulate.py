import asyncio
import random
from pathlib import Path
from typing import Optional

import typer
from rich.progress import Progress, SpinnerColumn, TextColumn

from ws_cli.core.azure_telemetry import AzureTelemetryTransmitter
from ws_cli.core.device_manager import DeviceManager
from ws_cli.core.interfaces import TelemetryTransmitter
from ws_cli.core.sim_data_gen import SimulatedDataGenerator
from ws_cli.core.telemetry import ConsoleTelemetryTransmitter
from ws_cli.utils.console import print_info, print_success, print_error, print_warning

app = typer.Typer(help="Simulation commands")


def _create_transmitter(device, dry_run: bool, progress=None, task_id=None) -> TelemetryTransmitter:
    """Factory function to create the appropriate transmitter based on dry_run flag."""
    if dry_run:
        return ConsoleTelemetryTransmitter(progress=progress, task_id=task_id)
    else:
        # Assuming device has dps_config - you may need to handle missing config
        if not device.dps_config:
            raise ValueError("Device missing DPS configuration for Azure transmission")
        return AzureTelemetryTransmitter(
            device_cfg=device,
            dps_cfg=device.dps_config,
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
):
    """
    Send exactly one telemetry message and exit.

    Examples:
        ws-cli simulate once
        ws-cli simulate once --device-id sim-001 --dry-run
    """

    async def send_telemetry(progress_, task_id):
        """Async function to handle the telemetry sending process."""
        # Create transmitter based on dry_run flag with progress support
        transmitter = _create_transmitter(device, dry_run, progress=progress_, task_id=task_id)

        try:
            # Connect to the transmitter
            await transmitter.connect()

            # Generate and send data
            generator = SimulatedDataGenerator()
            data = generator.generate(device)
            await transmitter.send(data)

        finally:
            # Always disconnect
            await transmitter.disconnect()

    try:
        # Get device
        device_manager = DeviceManager()
        device = _get_device(device_manager, device_id)

        if not device:
            print_error("No device found. Add a device first with 'ws-cli devices add'")
            raise typer.Exit(1)

        print_info(f"Using device: [bold]{device.device_id}[/bold]")

        if dry_run:
            print_warning("DRY RUN MODE - No data will be sent")

        # Generate and send telemetry
        with Progress(
                SpinnerColumn(),
                TextColumn("[progress.description]{task.description}"),
                transient=True,
        ) as progress:
            task = progress.add_task(description="Initializing...", total=None)

            # Run the async telemetry sending with progress updates
            asyncio.run(send_telemetry(progress, task))

        print_success("✓ Telemetry sent successfully")

    except KeyboardInterrupt:
        print_warning("\nOperation cancelled by user")
        raise typer.Exit(130)
    except Exception as e:
        print_error(f"Failed to send telemetry: {e}")
        raise typer.Exit(1)


@app.command("continuous")
def simulate_continuous(
        device_id: Optional[str] = typer.Option(
            None,
            "--device-id",
            "-d",
            help="Device ID to use",
            autocompletion=lambda: ["sim-001", "sim-002", "dev-001"],
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
):
    """
    Run continuous simulation until stopped or limit reached.

    Examples:
        ws-cli simulate continuous
        ws-cli simulate continuous --interval 60 --max-messages 10
        ws-cli simulate continuous --device-id sim-001 --seed 42
    """

    try:
        # Get device
        device_manager = DeviceManager()
        device = _get_device(device_manager, device_id)

        if not device:
            print_error("No device found. Add a device first with 'ws-cli devices add'")
            raise typer.Exit(1)

        print_info(f"Starting continuous simulation for device: [bold]{device.device_id}[/bold]")
        print_info(f"Interval: {interval}s, Jitter: ±{jitter}s")

        if max_messages:
            print_info(f"Will send {max_messages} messages")
        else:
            print_info("Press Ctrl+C to stop")

        if seed is not None:
            print_info(f"Using random seed: {seed}")
            random.seed(seed)

        if dry_run:
            print_warning("DRY RUN MODE - No data will be sent")

        # Run simulation loop
        asyncio.run(_simulation_loop(
            device=device,
            interval=interval,
            jitter=jitter,
            max_messages=max_messages,
            dry_run=dry_run,
        ))

    except KeyboardInterrupt:
        print_warning("\n\nSimulation stopped by user")
        print_success("Graceful shutdown complete")
        raise typer.Exit(0)
    except Exception as e:
        print_error(f"Simulation failed: {e}")
        raise typer.Exit(1)


async def _simulation_loop(
        device,
        interval: int,
        jitter: float,
        max_messages: Optional[int],
        dry_run: bool,
):
    """Run the continuous simulation loop."""
    from rich import print
    import asyncio
    import random

    messages_sent = 0
    generator = SimulatedDataGenerator()

    # Create transmitter once and reuse connection
    transmitter = _create_transmitter(device, dry_run)

    # Set up signal handlers for graceful shutdown
    stop_event = asyncio.Event()
    import signal

    def signal_handler(signum, frame):
        stop_event.set()

    signal.signal(signal.SIGINT, signal_handler)
    signal.signal(signal.SIGTERM, signal_handler)

    try:
        # Connect once at the start
        await transmitter.connect()

        while not stop_event.is_set():
            # Check message limit
            if max_messages and messages_sent >= max_messages:
                print(f"\n✓ Sent {messages_sent} messages (limit reached)")
                break

            try:
                # Generate and send telemetry
                data = generator.generate(device)
                await transmitter.send(data)
                messages_sent += 1

                if dry_run:
                    print(f"[dim]Message {messages_sent}: Generated (DRY RUN)[/dim]")
                else:
                    print(f"[green]Message {messages_sent}: Sent successfully[/green]")

            except Exception as e:
                print(f"[red]Error sending message {messages_sent + 1}: {e}[/red]")
                # Continue with next message rather than failing completely
                continue

            # Wait for next interval
            if max_messages is None or messages_sent < max_messages:
                # Apply jitter to interval
                jitter_amount = random.uniform(-jitter, jitter) if jitter > 0 else 0
                wait_time = max(1, interval + jitter_amount)  # Ensure minimum 1 second
                print(f"[dim]Next message in {wait_time:.1f}s...[/dim]")

                try:
                    await asyncio.wait_for(stop_event.wait(), timeout=wait_time)
                    break  # Stop event was triggered
                except asyncio.TimeoutError:
                    continue  # Continue to next iteration

    except Exception as e:
        print(f"\nError in simulation loop: {e}")
        raise
    finally:
        # Always disconnect
        try:
            await transmitter.disconnect()
        except Exception as e:
            print(f"[yellow]Warning: Error during disconnect: {e}[/yellow]")


def _get_device(device_manager, device_id: Optional[str]):
    """Get device by ID or return default device."""
    if device_id:
        return device_manager.get_device(device_id)
    else:
        return device_manager.get_default_device()