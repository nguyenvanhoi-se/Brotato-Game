"""Declarative journeys over the real build. Setup hooks require simulated kind."""
import json
from pathlib import Path
from driver import By


class FlowRunner:
    def __init__(self, game, spec):
        self.game, self.spec = game, spec

    def run(self, path):
        flow = json.loads(Path(path).read_text(encoding="utf-8"))
        if flow["id"] != self.spec["id"] or flow["kind"] != self.spec["kind"]:
            raise ValueError("Flow identity/kind must match the reviewed catalog case")
        for index, step in enumerate(flow["steps"], 1):
            self.game.record("flow_step", dict(index=index, step=step))
            try:
                self.step(step, flow["kind"])
            except Exception as exc:
                raise AssertionError(f"Flow {flow['id']} step {index} {step}: {exc}") from exc

    def step(self, step, kind):
        game, action = self.game, step["action"]
        by = By.PATH if "path" in step else By.NAME
        selector = step.get("path", step.get("name"))
        if action == "launch":
            if game.process is None:
                game.start()
            if not game.connection_verified:
                raise AssertionError("Player identity has not been verified")
        elif action == "assert_scene":
            assert game.driver.get_current_scene() == step["equals"]
        elif action == "wait_visible":
            game.driver.wait_for_object(by, selector, timeout=step.get("timeout", 5))
        elif action == "assert_property":
            obj = game.driver.find_object(by, selector)
            actual = obj.get_component_property(step["component"], step["property"])
            assert actual == step["equals"], (actual, step["equals"])
        elif action == "click":
            game.driver.click(by, selector)
        elif action == "press_key":
            game.driver.press_key(step["key"], duration=step.get("duration", .2))
        elif action == "load_scene":
            game.driver.load_scene(step["scene"])
        elif action == "wait_state":
            game.wait(lambda state: state[step["field"]] == step["equals"], timeout=step.get("timeout", 5))
        elif action == "screenshot":
            name = step.get("file", "screen.png")
            if Path(name).name != name:
                raise ValueError("Screenshot file must be a basename")
            game.screenshot(name)
        elif action == "expect_no_errors":
            game.healthy()
        elif action == "hook":
            if kind != "simulated":
                raise ValueError("A natural journey cannot create states through a test hook")
            game.call(step["method"], *step.get("args", []))
        elif action == "close":
            game.healthy()
            process = game.process
            game.stop()
            assert process is not None and process.poll() is not None, "Player process did not stop"
            game.closed_intentionally = True
        else:
            raise ValueError(f"Unsupported action: {action}")
