import typer
from typing import Optional
from pathlib import Path

app = typer.Typer(help="Simulation commands")


@app.command("once")
def simulate_once(
        device_id: Optional[str] = typer.Option(
            None,
            "--device-id",
            "-d",
            help="Device ID to use (defaults to configured default device)",
            autocompletion=lambda: ["sim-001", "sim-002", "dev-001"],  # TODO: Dynamic completion
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
    from ws_cli.utils.console import print_info, print_success, print_error, print_warning
    from rich.progress import Progress, SpinnerColumn, TextColumn
    from rich.table import Table
    from rich import print
    from ws_cli.core.device_manager import DeviceManager
    from ws_cli.core.interfaces import TelemetryTransmitter, WeatherDataGenerator
    from ws_cli.models import Device

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
            progress.add_task(description="Generating weather data...", total=None)

            # TODO: Implement actual telemetry generation and transmission
            # generator = WeatherDataGenerator(device)
            # data = generator.generate()

            if not dry_run:
                progress.update(task_id=0, description="Sending telemetry...")
                # TODO: transmitter = TelemetryTransmitter(device)
                # asyncio.run(transmitter.send(data))

        print_success("✓ Telemetry sent successfully")

        if dry_run:
            # TODO: Display generated data
            table = Table(title="Generated Weather Data (DRY RUN)")
            table.add_column("Metric", style="cyan")
            table.add_column("Value", style="green")
            table.add_row("Temperature", "22.5°C")
            table.add_row("Humidity", "65%")
            table.add_row("Pressure", "1013.25 hPa")
            table.add_row("Rain", "0.2 mm")
            print(table)

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
    from ws_cli.utils.console import print_info, print_success, print_error, print_warning
    from ws_cli.core.device_manager import DeviceManager
    from ws_cli.models import Device
    import asyncio

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
            # TODO: Set random seed

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
    messages_sent = 0

    # Set up signal handlers for graceful shutdown
    stop_event = asyncio.Event()
    import signal

    def signal_handler(signum, frame):
        stop_event.set()

    signal.signal(signal.SIGINT, signal_handler)
    signal.signal(signal.SIGTERM, signal_handler)

    import asyncio
    try:
        while not stop_event.is_set():
            # Check message limit
            if max_messages and messages_sent >= max_messages:
                print(f"\n✓ Sent {messages_sent} messages (limit reached)")
                break

            # TODO: Generate and send telemetry
            messages_sent += 1

            if dry_run:
                print(f"[dim]Message {messages_sent}: Generated (DRY RUN)[/dim]")
            else:
                print(f"[green]Message {messages_sent}: Sent successfully[/green]")

            # Wait for next interval
            if max_messages is None or messages_sent < max_messages:
                wait_time = interval  # TODO: Add jitter
                print(f"[dim]Next message in {wait_time}s...[/dim]")

                try:
                    await asyncio.wait_for(stop_event.wait(), timeout=wait_time)
                    break  # Stop event was triggered
                except asyncio.TimeoutError:
                    continue  # Continue to next iteration

    except Exception as e:
        print(f"\nError in simulation loop: {e}")
        raise


def _get_device(device_manager, device_id: Optional[str]):
    """Get device by ID or return default device."""
    if device_id:
        return device_manager.get_device(device_id)
    else:
        return device_manager.get_default_device()