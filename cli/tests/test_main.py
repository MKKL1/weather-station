from typer.testing import CliRunner
from hw_cli.__main__ import app

def test_help(runner):
    """Test the main help command."""
    result = runner.invoke(app, ["--help"])
    assert result.exit_code == 0
    assert "Heavy Weather CLI" in result.stdout
    assert "devices" in result.stdout
    assert "simulate" in result.stdout

def test_version(runner):
    """Test the version flag."""
    result = runner.invoke(app, ["--version"])
    assert result.exit_code == 0
    assert "Heavy Weather CLI v" in result.stdout