import json
import os
from pathlib import Path
import subprocess
import time
import uuid

from alttester import AltDriver, By
from alttester.exceptions import ConnectionError as AltConnectionError
from identity import validate_identity
from processes import stop_owned


class ConnectionUnavailable(RuntimeError):
    pass


class Game:
    def __init__(self, context, folder):
        self.context, self.folder = context, folder
        self.process = self.driver = None
        self.observations = []
        self.launch_index = 0
        self.connection_verified = False

    def record(self, action, value=None):
        self.observations.append(dict(action=action, value=value, monotonic=time.monotonic()))
        (self.folder / "observations.json").write_text(json.dumps(self.observations, ensure_ascii=False, indent=2), encoding="utf-8")

    def start(self):
        self.launch_index += 1
        self.nonce = uuid.uuid4().hex
        player_log = self.folder / f"player-{self.launch_index}.log"
        self.process = subprocess.Popen([self.context["build"], "-screen-fullscreen", "0", "-screen-width", "960",
                                         "-screen-height", "540", "-logFile", str(player_log)],
                                         env=dict(os.environ, BROTATO_E2E_SESSION_NONCE=self.nonce))
        self.record("launch", dict(pid=self.process.pid, log=player_log.name))
        try:
            self.driver = AltDriver(host=self.context["host"], port=self.context["port"],
                                    app_name=self.context["app_name"], timeout=30)
        except AltConnectionError as exc:
            raise ConnectionUnavailable(str(exc)) from exc
        self.record("connect", self.driver.get_current_scene())
        self.driver.wait_for_object(By.NAME, "__E2EHarness", timeout=10)
        self.snapshot()
        self.connection_verified = True

    def call(self, method, *args):
        obj = self.driver.find_object(By.NAME, "__E2EHarness")
        types = [{bool: "System.Boolean", int: "System.Int32", float: "System.Single", str: "System.String"}[type(a)] for a in args]
        result = obj.call_component_method("Brotato.E2E.E2EHarness", method, "Assembly-CSharp",
                                            parameters=list(args), type_of_parameters=types)
        self.record(method, dict(parameters=args, result=result))
        return result

    def snapshot(self):
        value = self.call("Snapshot")
        # Driver decodes one serialization layer; JsonUtility result is a JSON string.
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
        # Capture errors even if SDK lost its connection before the hook could report them.
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
