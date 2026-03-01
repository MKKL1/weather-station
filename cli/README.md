# Heavy Weather CLI tool

Python CLI tool (`hw`) for simulating weather station devices.

## Prerequisites

- Python ≥ 3.9
- Poetry

## Installation

```bash
...
```

You can now use the `hw` command.

## Quick Start

```bash
...
hw simulate once
```

## Commands

| Command | Subcommands | Description |
| --- | --- | --- |
| `hw devices` | `add`, `list`, `show`, `remove`, `set-default` | Manage device configurations |
| `hw simulate` | `once`, `loop` | Send simulated telemetry data |
| `hw cache` | `show`, `clear`, `clean`, `stats` | Manage cached access tokens |
| `hw config` | `show`, `create`, `edit`, `path` | Manage CLI configuration |
| `hw console` | - | Interactive REPL mode |

Use `hw <command> --help` for detailed options on any command.

## Configuration

The CLI reads a `config.json` file from its app data directory. 
Use `hw config path` to see the location, or pass `--config <path>` / set `HW_CLI_CONFIG` to override.
