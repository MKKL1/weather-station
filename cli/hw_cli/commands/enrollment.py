import base64
import csv
import hashlib
import hmac
import json
import uuid
from pathlib import Path
from typing import List, Optional

import typer
from rich.table import Table
from rich import print

app = typer.Typer(help="Azure DPS group-enrollment helper: derive per-device symmetric keys from a group master key")


def _derive_device_key(master_key_b64: str, registration_id: str) -> str:
    """Derive a per-device symmetric key from a group symmetric key."""
    try:
        master_key = base64.b64decode(master_key_b64)
    except Exception as e:
        raise ValueError("Invalid base64 master key") from e

    digest = hmac.new(master_key, registration_id.encode("utf-8"), hashlib.sha256).digest()
    return base64.b64encode(digest).decode("utf-8")


@app.command("generate")
def generate_keys(
        dps_id_scope: str = typer.Option(
            ..., "--dps-id-scope", "-s", help="Azure DPS ID Scope"
        ),
        dps_provisioning_host: str = typer.Option(
            "global.azure-devices-provisioning.net",
            "--dps-provisioning-host",
            help="DPS provisioning host (optional, informational)",
        ),
        group_master_key: str = typer.Option(
            ..., "--group-master-key", "-g", help="Group enrollment primary key (base64)", hide_input=True
        ),
        secondary_master_key: Optional[str] = typer.Option(
            None, "--secondary-master-key", help="Optional secondary group key (base64)", hide_input=True
        ),
        devices_file: Optional[Path] = typer.Option(
            None, "--devices-file", "-f", help="Path to a file with one device name (registration id) per line"
        ),
        output_format: str = typer.Option(
            "table", "--output-format", "-o", help="Output format: table | json | csv",
        ),
        out_file: Optional[Path] = typer.Option(
            None, "--out-file", help="Write output to file (json or csv depending on --output-format)"
        ),
        num_devices: int = typer.Option(
            1, "--num-devices", "-n", help="Number of devices to generate (if no device names are provided)."
        ),
        name_template: str = typer.Option(
            "dev-device-{}", "--name-template",
            help="Template for generated names. Must contain exactly one '{}' placeholder."
        ),
        device_names: Optional[List[str]] = typer.Argument(
            None,
            metavar="DEVICE",
            help="Device registration ids (space separated). If omitted, names will be generated.",
        ),
):
    """Generate per-device symmetric keys from a group enrollment primary key."""
    from hw_cli.utils.console import print_error, print_info, print_success

    try:
        devices: List[str] = list(device_names or [])

        if devices_file:
            if not devices_file.exists():
                print_error(f"Devices file not found: {devices_file}")
                raise typer.Exit(1)
            with devices_file.open("r", encoding="utf-8") as f:
                for line in f:
                    name = line.strip()
                    if name:
                        devices.append(name)

        if not devices:
            if name_template.count("{}") != 1:
                print_error("--name-template must contain exactly one '{}' placeholder.")
                raise typer.Exit(1)

            print_info(f"No device names provided. Generating {num_devices} device(s)...")
            for _ in range(num_devices):
                random_part = str(uuid.uuid4())[:8]
                device_name = name_template.format(random_part)
                devices.append(device_name)

        results = []
        for reg_id in devices:
            primary = _derive_device_key(group_master_key, reg_id)
            row = {
                "registration_id": reg_id,
                "device_primary_key": primary,
                "dps_id_scope": dps_id_scope,
                "dps_provisioning_host": dps_provisioning_host,
            }
            if secondary_master_key:
                secondary = _derive_device_key(secondary_master_key, reg_id)
                row["device_secondary_key"] = secondary
            results.append(row)

        if output_format == "table":
            table = Table(title="Derived device keys")
            table.add_column("Registration ID", style="cyan")
            table.add_column("Primary Key", style="dim")
            if secondary_master_key:
                table.add_column("Secondary Key", style="dim")
            for r in results:
                row_data = [r["registration_id"], r["device_primary_key"]]
                if secondary_master_key:
                    row_data.append(r["device_secondary_key"])
                table.add_row(*row_data)
            print(table)

        elif output_format == "json":
            payload = json.dumps(results, indent=2)
            if out_file:
                out_file.write_text(payload, encoding="utf-8")
                print_success(f"✓ Wrote JSON to {out_file}")
            else:
                print(payload)

        elif output_format == "csv":
            if out_file is None:
                print_error("--out-file is required when using csv output")
                raise typer.Exit(1)
            fieldnames = ["registration_id", "device_primary_key"]
            if secondary_master_key:
                fieldnames.append("device_secondary_key")
            fieldnames.extend(["dps_id_scope", "dps_provisioning_host"])
            with out_file.open("w", newline="", encoding="utf-8") as csvf:
                writer = csv.DictWriter(csvf, fieldnames=fieldnames)
                writer.writeheader()
                writer.writerows(results)
            print_success(f"✓ Wrote CSV to {out_file}")

        else:
            print_error(f"Unsupported output format: {output_format}")
            raise typer.Exit(1)

    except Exception as e:
        print_error(f"Failed to generate keys: {e}")
        raise typer.Exit(1)