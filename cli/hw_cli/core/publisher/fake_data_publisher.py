import asyncio

from hw_cli.core.publisher.data_publisher import DataPublisher
from hw_cli.utils.console import print_info, print_success
from hw_cli.utils.telemetry_format import print_telemetry


class FakeDataPublisher(DataPublisher):
    """A transmitter that prints telemetry to the console instead of sending it."""

    def __init__(self, progress=None, task_id=None):
        self.progress = progress
        self.task_id = task_id

    def _update_progress(self, description: str):
        """Update progress bar if available, otherwise print to console."""
        if self.progress and self.task_id is not None:
            self.progress.update(self.task_id, description=description)
        else:
            # Fallback to console printing when no progress bar
            print_info(description)

    async def connect(self) -> None:
        self._update_progress("Establishing (dummy) connection...")
        await asyncio.sleep(0.1)  # Simulate connection time

        # Only print success if no progress bar (backward compatibility)
        if not self.progress:
            print_success("Connection established.")

    async def send(self, data) -> None:
        self._update_progress("Sending telemetry...")
        await asyncio.sleep(0.2)
        print_telemetry("SENT", data)

    async def disconnect(self) -> None:
        self._update_progress("Closing (dummy) connection...")
        await asyncio.sleep(0.1)

        # Only print success if no progress bar (backward compatibility)
        if not self.progress:
            print_success("Connection closed.")