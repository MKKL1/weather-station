import math
import random
import time
from typing import Optional

from ws_cli.core.interfaces import WeatherDataGenerator
from ws_cli.models import Device, SimulationConfig, WeatherData, Histogram, DeviceInfo


class WeatherEntry:
    ENTRY_DURATION = 32 * 60
    NUM_INTERVALS = 16
    INTERVAL_S = ENTRY_DURATION // NUM_INTERVALS
    BITS = 4
    MAX_TIP = (1 << BITS) - 1
    MASK_BYTES = (NUM_INTERVALS * BITS + 7) // 8

    def __init__(self, ts: int = None):
        self.ts = ts or int(time.time())
        self.tip_mask = bytearray(self.MASK_BYTES)

    def _idx(self, evt_ts: int, now: int) -> Optional[int]:
        end = (now // self.INTERVAL_S) * self.INTERVAL_S
        start = end - self.ENTRY_DURATION
        if not (start <= evt_ts < end):
            return None
        return (evt_ts - start) // self.INTERVAL_S

    def add_tip(self, evt_ts: int, now: int) -> bool:
        idx = self._idx(evt_ts, now)
        if idx is None:
            return False
        byte, shift = divmod(idx * self.BITS, 8)
        cur = (self.tip_mask[byte] >> shift) & self.MAX_TIP
        if cur >= self.MAX_TIP:
            return False
        self.tip_mask[byte] |= (cur + 1) << shift
        return True

    @classmethod
    def start_time(cls, ts: int) -> int:
        return (ts // cls.INTERVAL_S) * cls.INTERVAL_S - cls.ENTRY_DURATION


class SimulatedDataGenerator(WeatherDataGenerator):
    """Concrete implementation of WeatherDataGenerator using the original simulation logic."""

    def __init__(self, config: Optional[SimulationConfig] = None):
        self.config = config or SimulationConfig()
        self.base = {'temp': 20.0, 'pres': 1013.25, 'hum': 50}
        self.rain_p = 0.3
        # Apply deterministic seed if provided
        if self.config.seed is not None:
            random.seed(self.config.seed)

    def _poisson_sample(self, lmbd: float) -> int:
        L = math.exp(-lmbd)
        k = 0
        p = 1.0
        while p > L and k < 100:
            k += 1
            p *= random.random()
        return max(0, k - 1)

    def _generate_rain_slots(
        self,
        num_slots: int,
        slot_secs: int,
        base_rain_p: float,
        humidity: float,
        pressure: float,
        start_ts: int,
        diurnal_pref_hour: int = 15,
        burst_chance: float = 0.05,
        persist_base: float = 0.6,
        max_count_per_slot: int = 4
    ):
        hum_factor = max(0.0, min(1.0, humidity / 100.0))
        pres_factor = max(0.5, 1.5 - (pressure - 1000.0) / 100.0)

        def diurnal(h):
            return 1.0 + 0.6 * math.exp(-((h - diurnal_pref_hour) % 24)**2 / (2 * 4**2))

        start_prob_base = base_rain_p * (0.6 + 0.8 * hum_factor) * (0.8 + 0.4 * (1.5 - pres_factor))
        start_prob_base = max(0.01, min(0.9, start_prob_base))

        persist_prob = min(0.95, persist_base + 0.3 * hum_factor)

        state = 'dry'
        slots = [0] * num_slots

        for idx in range(num_slots):
            slot_ts = start_ts + idx * slot_secs
            hour = (slot_ts % 86400) / 3600.0
            dmod = diurnal(hour)

            if state == 'dry':
                p_start = start_prob_base * dmod * hum_factor
                if random.random() < p_start:
                    state = 'wet'
                else:
                    slots[idx] = 0
                    continue

            if state == 'wet':
                base_lambda = 0.3 + 3.0 * hum_factor * base_rain_p * dmod
                if random.random() < burst_chance * hum_factor:
                    lam = base_lambda * (3 + random.random() * 4)
                else:
                    lam = base_lambda * (0.6 + random.random() * 1.2)

                count = self._poisson_sample(max(0.1, lam))
                count = min(max_count_per_slot, count)
                slots[idx] = count

                if random.random() > persist_prob:
                    state = 'dry'

        return slots

    def generate(self, device: Device) -> WeatherData:
        now = int(time.time())
        h = (now % 86400) / 3600
        temp = self.base['temp'] + 5 * math.sin((h - 6) * math.pi / 12) + random.gauss(0, 2)
        pres = self.base['pres'] + random.gauss(0, 5)
        hum = max(0.0, min(100.0, self.base['hum'] + random.gauss(0, 10)))

        entry = WeatherEntry(now)
        start_ts = WeatherEntry.start_time(now)

        slots = self._generate_rain_slots(
            num_slots=WeatherEntry.NUM_INTERVALS,
            slot_secs=WeatherEntry.INTERVAL_S,
            base_rain_p=self.rain_p,
            humidity=hum,
            pressure=pres,
            start_ts=start_ts,
            diurnal_pref_hour=15,
            burst_chance=0.05,
            persist_base=0.6,
            max_count_per_slot=4
        )

        for i, count in enumerate(slots):
            slot_start = start_ts + i * WeatherEntry.INTERVAL_S
            for j in range(count):
                tip_ts = slot_start + 1 + int((WeatherEntry.INTERVAL_S - 2) * (j / max(1, count)))
                entry.add_tip(tip_ts, now)

        tips = Histogram(
            data=bytes(entry.tip_mask),
            count=WeatherEntry.NUM_INTERVALS,
            interval_duration=WeatherEntry.INTERVAL_S,
            start_time=WeatherEntry.start_time(now)
        )
        info = DeviceInfo(device.device_id, device.mm_per_tip, random.randint(1000, 9999))
        return WeatherData(now, temp, pres, hum, tips, info)