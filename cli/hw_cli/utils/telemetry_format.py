from datetime import datetime
from rich import print
from rich.text import Text

from datetime import datetime

from hw_cli.core.models.models import WeatherData


def print_telemetry(msg: str, data: WeatherData, timezone: str = "CEST") -> None:
    created_at = datetime.fromtimestamp(data.created_at).strftime("%Y-%m-%d %H:%M:%S") + f" {timezone}"

    # Extract tips per interval from histogram data
    tips_per_interval = []
    for idx in range(data.tips.count):  # 16 intervals
        byte, shift = divmod(idx * 4, 8)  # 4 bits per interval
        tip_count = (data.tips.data[byte] >> shift) & 0xF
        tips_per_interval.append(tip_count)

    total_tips = sum(tips_per_interval)
    rainfall_mm = total_tips * data.info.mm_per_tip

    message = Text()
    message.append(msg, style="bold cyan")
    message.append(":\n")
    message.append(f"  Timestamp: {created_at}\n")
    message.append(f"  Device ID: {data.info.id}\n")
    message.append(f"  Instance ID: {data.info.instance_id}\n")
    message.append(f"  Temperature: {data.temperature:.2f}°C\n")
    message.append(f"  Humidity: {data.humidity:.2f}%\n")
    message.append(f"  Pressure: {data.pressure:.2f} hPa\n")
    message.append(f"  Rainfall: {rainfall_mm:.2f} mm ({total_tips} tips, {data.info.mm_per_tip} mm/tip)\n")
    message.append(f"  Tips per interval: {tips_per_interval}\n")
    message.append(
        f"  Histogram: {data.tips.count} intervals, {data.tips.interval_duration}s each, "
        f"starting {datetime.fromtimestamp(data.tips.start_time).strftime('%Y-%m-%d %H:%M:%S')} {timezone}"
    )

    print(message)