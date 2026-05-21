def test_cli_is_installed_and_responds(run_cli):
    result = run_cli(["--help"])
    assert result.returncode == 0
