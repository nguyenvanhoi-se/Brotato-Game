"""License-free local driver for the development-build Unity E2E bridge."""
import json
import socket
import time
from enum import Enum


class By(Enum):
    NAME = "name"
    PATH = "path"


class ConnectionUnavailable(RuntimeError):
    pass


class CommandError(RuntimeError):
    pass


class Object:
    def __init__(self, driver, data):
        self.driver, self.data = driver, data
        self.id, self.name, self.path = data["id"], data["name"], data["path"]

    def get_component_property(self, component_name, property_name, assembly=None):
        return self.driver.command("property", objectId=self.id, component=component_name, property=property_name)

    def click(self):
        return self.driver.command("click", objectId=self.id)


class UnityDriver:
    def __init__(self, port, token, process, timeout=20):
        self.socket = None
        self.stream = None
        self.counter = 0
        self.token = token
        deadline = time.monotonic() + timeout
        last = None
        while time.monotonic() < deadline:
            if process.poll() is not None:
                raise RuntimeError(f"Player exited before bridge startup: {process.returncode}")
            try:
                self.socket = socket.create_connection(("127.0.0.1", port), timeout=.5)
                self.socket.settimeout(35)
                self.stream = self.socket.makefile("rwb")
                break
            except OSError as exc:
                last = exc
                if self.socket:
                    self.socket.close()
                time.sleep(.1)
        if self.stream is None:
            raise ConnectionUnavailable(f"Unity bridge not ready on loopback port {port}: {last}")
        try:
            self.hello = self.command("hello")
            if self.hello.get("protocolVersion") != "1":
                raise CommandError(f"Unsupported bridge protocol: {self.hello}")
        except Exception:
            self.stop()
            raise

    def command(self, command, **parameters):
        self.counter += 1
        request = dict(id=self.counter, token=self.token, command=command, **parameters)
        raw = json.dumps(request, ensure_ascii=False, separators=(",", ":")).encode("utf-8") + b"\n"
        if len(raw) > 1024 * 1024:
            raise ValueError("Request exceeds protocol limit")
        self.stream.write(raw)
        self.stream.flush()
        raw_response = self.stream.readline(1024 * 1024 + 1)
        if not raw_response or len(raw_response) > 1024 * 1024 or not raw_response.endswith(b"\n"):
            raise CommandError("Bridge closed or returned an oversized/incomplete response")
        response = json.loads(raw_response)
        if response.get("id") != self.counter:
            raise CommandError("Response correlation mismatch")
        if not response.get("ok"):
            raise CommandError(f"{command}: {response.get('error', 'unknown bridge error')}")
        return json.loads(response["result"])

    def get_current_scene(self):
        return self.command("scene")

    def load_scene(self, scene_name):
        return self.command("load_scene", scene=scene_name)

    def find_objects(self, by, value):
        return [Object(self, item) for item in self.command("find", **{by.value: value})["objects"]]

    def find_object(self, by, value):
        found = self.find_objects(by, value)
        if len(found) != 1:
            raise CommandError(f"Expected exactly one {by.value}={value!r}, found {len(found)}")
        return found[0]

    def wait_for_object(self, by, value, timeout=5):
        deadline = time.monotonic() + timeout
        while True:
            found = self.find_objects(by, value)
            if len(found) == 1:
                return found[0]
            if len(found) > 1:
                raise CommandError(f"Ambiguous selector {by.value}={value!r}")
            if time.monotonic() >= deadline:
                raise CommandError(f"Object did not appear: {by.value}={value!r}")
            time.sleep(.05)

    def call(self, method, *args):
        if method == "Snapshot":
            return self.command("snapshot")
        parameters = [str(a).lower() if isinstance(a, bool) else str(a) for a in args]
        return self.command("call", method=method, args=parameters)

    def get_png_screenshot(self, path):
        return self.command("screenshot", path=str(path))

    def press_key(self, key, duration=.2):
        return self.command("input", key=key, duration=float(duration))

    def click(self, by, value):
        return self.find_object(by, value).click()

    def stop(self):
        if self.stream:
            self.stream.close()
            self.stream = None
        if self.socket:
            self.socket.close()
            self.socket = None
