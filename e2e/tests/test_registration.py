import json
import time

import httpx
import pytest

_state: dict = {}


@pytest.fixture(autouse=True)
def _require_cli(run_cli):
    _state["run_cli"] = run_cli

"""
Tests the full device lifecycle after generation:
register,claim,telemetry send
"""
@pytest.mark.order(1)
def test_device_registers_and_returns_claim_code(registered_device):
    device, registration = registered_device

    assert len(registration.claim_code) == 9, (
        f"Expected 9-char claim code, got '{registration.claim_code}'"
    )

    _state["device"] = device
    _state["claim_code"] = registration.claim_code


@pytest.mark.order(2)
def test_device_claim_succeeds(api_client):
    device = _state["device"]

    result = api_client.claim_device(
        device_id=device.device_id,
        claim_code=_state["claim_code"],
        key=device.claim_words,
    )

    assert result.get("success") is True


@pytest.mark.order(3)
def test_stale_token_rejected_after_claim():
    run_cli = _state["run_cli"]
    device = _state["device"]

    result = run_cli(["simulate", "once", "-d", device.device_id])

    assert result.returncode != 0, "Expected simulate to fail with stale token"
    combined_output = result.stdout + result.stderr
    assert "401" in combined_output, (
        f"Expected HTTP 401 in output, got:\n{combined_output}"
    )


@pytest.mark.order(4)
def test_telemetry_succeeds_after_cache_clear():
    run_cli = _state["run_cli"]
    device = _state["device"]

    clear_result = run_cli(["cache", "clear", "-d", device.device_id, "-f"])
    assert clear_result.returncode == 0, f"cache clear failed: {clear_result.stderr}"

    sim_result = run_cli(["-o", "json", "simulate", "once", "-d", device.device_id, "-f", "json"])
    assert sim_result.returncode == 0, f"simulate once failed: {sim_result.stderr}"

    telemetry = json.loads(sim_result.stdout)
    assert "reading" in telemetry
    assert "timestamp" in telemetry

    _state["sent_telemetry"] = telemetry


@pytest.mark.order(5)
def test_telemetry_values_match_api_query(api_client):
    device = _state["device"]
    sent = _state["sent_telemetry"]["reading"]

    # wait for data to be available
    max_wait = 5.0
    poll_interval = 0.25
    start_time = time.time()
    latest = None
    last_err = None

    while time.time() - start_time < max_wait:
        try:
            latest = api_client.get_latest_measurement(device.device_id)
            assert latest.get("deviceId") == device.device_id
            measurements = latest.get("measurements", {})
            assert "temperature" in measurements
            assert "humidity" in measurements
            assert "pressure" in measurements
            break
        except (httpx.HTTPStatusError, KeyError, AssertionError) as e:
            last_err = e
            time.sleep(poll_interval)
    else:
        if last_err:
            raise last_err
        pytest.fail("Timed out waiting for telemetry data in API query")

    assert latest is not None
    assert latest["deviceId"] == device.device_id

    measurements = latest["measurements"]
    assert measurements["temperature"] == pytest.approx(sent["temperature"])
    assert measurements["humidity"] == pytest.approx(sent["humidity"])
    assert measurements["pressure"] == pytest.approx(sent["pressure"])
