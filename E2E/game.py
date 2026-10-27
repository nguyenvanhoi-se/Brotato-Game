import json
import os
from pathlib import Path
import subprocess
import time
import uuid
import secrets
import socket

from driver import UnityDriver, By, ConnectionUnavailable
from identity import validate_identity
from processes import stop_owned


class Game:
    def __init__(self, context, folder):
        self.context, self.folder = context, folder
        self.process = self.driver = None
        self.observations = []
        self.launch_index = 0
        self.connection_verified = False
        self.closed_intentionally = False

    def record(self, action, value=None):
        self.observations.append(dict(action=action, value=value, monotonic=time.monotonic()))
        (self.folder / "observations.json").write_text(json.dumps(self.observations, ensure_ascii=False, indent=2), encoding="utf-8")

    def start(self):
        self.launch_index += 1
        self.nonce = uuid.uuid4().hex
        token = secrets.token_hex(32)
        with socket.socket() as reservation:
            reservation.bind(("127.0.0.1", 0))
            port = reservation.getsockname()[1]
        player_log = self.folder / f"player-{self.launch_index}.log"
        self.process = subprocess.Popen([self.context["build"], "-screen-fullscreen", "0", "-screen-width", "960",
                                         "-screen-height", "540", "-logFile", str(player_log)],
                                         env=dict(os.environ, BROTATO_E2E_SESSION_NONCE=self.nonce,
                                                  UNITY_E2E_PORT=str(port), UNITY_E2E_TOKEN=token,
                                                  UNITY_E2E_ARTIFACTS_ROOT=str(self.folder.resolve())))
        self.record("launch", dict(pid=self.process.pid, log=player_log.name))
        self.driver = UnityDriver(port=port, token=token, process=self.process, timeout=30)
        validate_identity(self.driver.hello, self.process.pid, self.nonce,
                          self.context["environment"]["source_fingerprint"],
                          self.context["environment"]["test_build_fingerprint"])
        self.record("connect", self.driver.get_current_scene())
        self.driver.wait_for_object(By.NAME, "__E2EHarness", timeout=10)
        self.snapshot()
        self.connection_verified = True

    def call(self, method, *args):
        result = self.driver.call(method, *args)
        self.record(method, dict(parameters=args, result=result))
        return result

    def snapshot(self):
        value = self.call("Snapshot")
        # The bridge returns actual observed state from the player process.
        state = json.loads(value) if isinstance(value, str) else value
        assert isinstance(state, dict), f"Invalid Snapshot: {state!r}"
        self.record("snapshot", state)
        validate_identity(state, self.process.pid, self.nonce, self.context["environment"]["source_fingerprint"],
                          self.context["environment"]["test_build_fingerprint"])
        return state

    def wait(self, predicate, timeout=5):
        deadline = time.monotonic() + timeout
        while True:
            state = self.snapshot()
            if predicate(state):
                return state
            if time.monotonic() >= deadline:
                raise AssertionError(f"Condition timed out after {timeout}s; last snapshot={state}")
            time.sleep(.05)

    def screenshot(self, name="screen.png"):
        path = self.folder / name
        self.driver.get_png_screenshot(str(path))
        assert path.read_bytes().startswith(b"\x89PNG\r\n\x1a\n"), "Screenshot must be a real PNG"
        self.record("screenshot", path.name)
        return path

    def healthy(self):
        state = self.snapshot()
        assert state["errorCount"] == 0, state["errors"]
        assert self.process.poll() is None, f"Player exited early: {self.process.returncode}"
        # Capture errors even if the bridge disconnected before the hook could report them.
        for log in self.folder.glob("player-*.log"):
            text = log.read_text(encoding="utf-8", errors="replace") if log.exists() else ""
            errors = [line for line in text.splitlines() if any(token in line for token in
                      ("NullReferenceException:", "MissingReferenceException:", "Unhandled Exception:", "Assertion failed"))]
            assert not errors, "\n".join(errors)

    def stop(self):
        try:
            if self.driver:
                self.driver.stop()
        finally:
            self.driver = None
            stop_owned(self.process)
            self.record("shutdown", None if self.process is None else self.process.poll())
            self.process = None

    def restart(self):
        self.healthy()
        self.stop()
        self.start()
