import json
import logging
import os
import sqlite3
import time
from pathlib import Path
from typing import Any, Dict, List, Optional

logger = logging.getLogger(__name__)

DB_FILENAME = "hw.db"

def get_app_dir() -> Path:
    """Get the application data directory."""
    env_path = os.getenv("HW_CLI_DATA_DIR")
    if env_path:
        return Path(env_path).expanduser().resolve()
    return Path.home() / ".hw-cli"


class Database:
    """
    SQLite database wrapper for application data (Devices, Tokens, State).
    Replaces the previous file-locked JSON storage.
    """

    def __init__(self):
        self.app_dir = get_app_dir()
        self.app_dir.mkdir(parents=True, exist_ok=True)
        self.db_path = self.app_dir / DB_FILENAME
        self._conn = sqlite3.connect(self.db_path, check_same_thread=False)
        self._conn.row_factory = sqlite3.Row  # Allow accessing columns by name
        self._init_schema()

    def _init_schema(self):
        """Initialize SQL tables."""
        with self._conn:
            # 1. Devices Table
            # We store the bulk of the config as a JSON blob to maintain compatibility
            # with the flexible Python dictionary model without strict schema migration.
            self._conn.execute("""
                CREATE TABLE IF NOT EXISTS devices (
                    device_id TEXT PRIMARY KEY,
                    data TEXT NOT NULL
                )
            """)

            # 2. Token Cache Table
            # Native columns for expiry allow us to query 'expired' tokens efficiently.
            self._conn.execute("""
                CREATE TABLE IF NOT EXISTS tokens (
                    device_id TEXT PRIMARY KEY,
                    token TEXT NOT NULL,
                    expires_at INTEGER NOT NULL,
                    cached_at INTEGER NOT NULL,
                    expires_in INTEGER NOT NULL
                )
            """)

            # 3. App Settings (Key-Value)
            # Used for things like 'default_device_id'
            self._conn.execute("""
                CREATE TABLE IF NOT EXISTS settings (
                    key TEXT PRIMARY KEY,
                    value TEXT NOT NULL
                )
            """)

    def close(self):
        self._conn.close()

    # --- Generic Settings Methods ---

    def get_setting(self, key: str) -> Optional[str]:
        cur = self._conn.execute("SELECT value FROM settings WHERE key = ?", (key,))
        row = cur.fetchone()
        return row["value"] if row else None

    def set_setting(self, key: str, value: str) -> None:
        with self._conn:
            self._conn.execute(
                "INSERT OR REPLACE INTO settings (key, value) VALUES (?, ?)",
                (key, value)
            )

    def delete_setting(self, key: str) -> None:
        with self._conn:
            self._conn.execute("DELETE FROM settings WHERE key = ?", (key,))

    # --- Device Methods ---

    def get_device(self, device_id: str) -> Optional[Dict[str, Any]]:
        cur = self._conn.execute("SELECT data FROM devices WHERE device_id = ?", (device_id,))
        row = cur.fetchone()
        if row:
            try:
                return json.loads(row["data"])
            except json.JSONDecodeError:
                logger.error(f"Corrupted JSON for device {device_id}")
                return None
        return None

    def get_all_devices(self) -> List[Dict[str, Any]]:
        cur = self._conn.execute("SELECT data FROM devices")
        results = []
        for row in cur:
            try:
                results.append(json.loads(row["data"]))
            except json.JSONDecodeError:
                continue
        return results

    def save_device(self, device_id: str, data: Dict[str, Any]) -> None:
        json_str = json.dumps(data)
        with self._conn:
            self._conn.execute(
                "INSERT OR REPLACE INTO devices (device_id, data) VALUES (?, ?)",
                (device_id, json_str)
            )

    def delete_device(self, device_id: str) -> bool:
        with self._conn:
            cur = self._conn.execute("DELETE FROM devices WHERE device_id = ?", (device_id,))
            return cur.rowcount > 0

    def device_exists(self, device_id: str) -> bool:
        cur = self._conn.execute("SELECT 1 FROM devices WHERE device_id = ?", (device_id,))
        return cur.fetchone() is not None

    # --- Token Cache Methods ---

    def get_token_entry(self, device_id: str) -> Optional[Dict[str, Any]]:
        cur = self._conn.execute(
            "SELECT token, expires_at, cached_at, expires_in FROM tokens WHERE device_id = ?",
            (device_id,)
        )
        row = cur.fetchone()
        if row:
            return dict(row)
        return None

    def save_token(self, device_id: str, token: str, expires_at: int, cached_at: int, expires_in: int) -> None:
        with self._conn:
            self._conn.execute(
                """
                INSERT OR REPLACE INTO tokens (device_id, token, expires_at, cached_at, expires_in)
                VALUES (?, ?, ?, ?, ?)
                """,
                (device_id, token, expires_at, cached_at, expires_in)
            )

    def delete_token(self, device_id: str) -> bool:
        with self._conn:
            cur = self._conn.execute("DELETE FROM tokens WHERE device_id = ?", (device_id,))
            return cur.rowcount > 0

    def get_all_tokens(self) -> Dict[str, Dict[str, Any]]:
        """Returns a dict format compatible with the existing CLI view logic."""
        cur = self._conn.execute("SELECT * FROM tokens")
        result = {}
        for row in cur:
            result[row["device_id"]] = dict(row)
        return result

    def delete_expired_tokens(self) -> int:
        now = int(time.time())
        with self._conn:
            cur = self._conn.execute("DELETE FROM tokens WHERE expires_at <= ?", (now,))
            return cur.rowcount

    def clear_all_tokens(self) -> int:
        with self._conn:
            cur = self._conn.execute("DELETE FROM tokens")
            return cur.rowcount


# Singleton instance
_db_instance: Optional[Database] = None

def get_data() -> Database:
    """Get the database singleton."""
    global _db_instance
    if _db_instance is None:
        _db_instance = Database()
    return _db_instance