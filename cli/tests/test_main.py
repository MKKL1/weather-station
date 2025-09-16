from ws_cli.__main__ import app

def test_help(runner):
    result = runner.invoke(app, ["--help"])
    assert result.exit_code == 0
    assert "Weather Station CLI" in result.stdout

def test_version(runner, monkeypatch):
    # Patch print_success to avoid fancy console output breaking the test
    monkeypatch.setattr("ws_cli.utils.console.print_success", lambda msg: print(msg))
    result = runner.invoke(app, ["--version"])
    assert result.exit_code == 0
    assert "Weather Station CLI v" in result.stdout