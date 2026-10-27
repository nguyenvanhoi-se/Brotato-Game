import math
import time
from pathlib import Path

import pytest
from driver import By
from flows import FlowRunner


def scene_objects(game):
    assert game.driver.get_current_scene() == "SampleScene"
    camera = game.driver.wait_for_object(By.NAME, "Main Camera", timeout=5)
    game.driver.wait_for_object(By.NAME, "Global Light 2D", timeout=5)
    assert camera.get_component_property("UnityEngine.Camera", "orthographic", "UnityEngine.CoreModule") is True
    assert camera.get_component_property("UnityEngine.Camera", "orthographicSize", "UnityEngine.CoreModule") == pytest.approx(5)
    assert len(game.driver.find_objects(By.NAME, "Main Camera")) == 1


def distance(state):
    return math.hypot(state["enemyX"] - state["targetX"], state["enemyY"] - state["targetY"])


def test_project_flow(spec, game):
    cid = spec["id"]
    if cid == "E2E-001":
        FlowRunner(game, spec).run(Path(__file__).resolve().parents[1] / "flows/startup.json")
        return
    if cid.startswith("E2E-"):
        scene_objects(game)
        if cid == "E2E-002":
            time.sleep(2)
            scene_objects(game)
        elif cid == "E2E-003":
            game.driver.load_scene("SampleScene")
            game.driver.wait_for_object(By.NAME, "__E2EHarness", timeout=10)
            scene_objects(game)
        elif cid == "E2E-004":
            game.screenshot("launch-1.png")
            game.restart()
            scene_objects(game)
        game.screenshot()
        return
    number = int(cid.split("-")[1])
    if number <= 13:
        game.call("CreateEnemy", number != 13)
        before = game.snapshot()
        assert before["enemyExists"]
        if number == 1:
            time.sleep(.5)
            state = game.snapshot()
            assert distance(state) < distance(before) - .2
            assert abs(state["enemyY"]) < .05
        elif number in (2, 5):
            # Let Start run before deliberately removing the target.
            time.sleep(.1)
            game.call("SetTarget", False) if number == 2 else game.call("RemoveTarget")
            time.sleep(.1)
            still = game.snapshot()
            time.sleep(.3)
            final = game.snapshot()
            assert final["enemyX"] == pytest.approx(still["enemyX"], abs=.03)
        elif number in (3, 4):
            game.call("SetSpeed", 0.0 if number == 3 else -5.0)
            state = game.snapshot()
            assert state["speed"] == pytest.approx(.1)
            time.sleep(.5)
            assert distance(game.snapshot()) < distance(state)
        elif number == 6:
            assert before["currentHealth"] == before["maxHealth"] == 30
            game.call("Damage", 10)
            assert game.snapshot()["currentHealth"] == 20
            game.call("Damage", 20)
            game.wait(lambda s: not s["enemyExists"])
        elif number in (7, 8):
            game.call("Damage", 0 if number == 7 else -10)
            assert game.snapshot()["currentHealth"] == 30
        elif number == 9:
            game.call("Damage", 100000)
            game.wait(lambda s: not s["enemyExists"])
        elif number in (10, 11):
            game.call("InitializeHealth", 0 if number == 10 else -5)
            state = game.snapshot()
            assert state["currentHealth"] == state["maxHealth"] == 1
            if number == 10:
                game.call("Damage", 1)
                game.wait(lambda s: not s["enemyExists"])
        elif number == 12:
            game.call("Damage", 10)
            assert game.snapshot()["currentHealth"] == 20
            game.call("InitializeHealth", 45)
            state = game.snapshot()
            assert state["currentHealth"] == state["maxHealth"] == 45
        elif number == 13:
            assert before["currentHealth"] == -1
            assert not before["sourceEnemyHasHealth"]
            assert before["speed"] == pytest.approx(2)
        return
    if number in (27, 28):
        game.call("SetupContact", number == 27)
        before = game.snapshot()
        assert before["contactExists"] and before["contactColliderIsChild"]
        assert before["contactEnterCount"] == 0
        assert before["contactHasPlayerHealth"] is (number == 27)
        if number == 27:
            assert before["contactHealth"] == before["contactMaxHealth"] == 100
            assert before["contactDamageCooldown"] == pytest.approx(.75)
        else:
            assert before["contactHealth"] == -1
            assert before["contactOtherHealth"] == 30
        game.call("SetContact", True)
        entered = game.wait(lambda s: s["contactEnterCount"] == 1, timeout=3)
        if number == 27:
            assert entered["contactHealth"] == 90, entered

            def cooldown_active(state):
                assert state["contactHealth"] == 90, state
                return state["contactStayCount"] > 0 and state["contactTime"] >= entered["contactEnterTime"] + .3

            game.wait(cooldown_active, timeout=3)
            damaged = game.wait(lambda s: s["contactHealth"] < 90, timeout=3)
            assert damaged["contactHealth"] == 80, damaged
            assert damaged["contactStayCount"] > 0
            assert damaged["contactTime"] - entered["contactEnterTime"] >= .73
        else:
            def ignored_contact(state):
                assert state["contactHealth"] == -1 and state["contactOtherHealth"] == 30, state
                assert state["errorCount"] == 0, state["errors"]
                return state["contactStayCount"] > 0 and state["contactTime"] >= entered["contactEnterTime"] + .9

            game.wait(ignored_contact, timeout=3)
        game.call("SetContact", False)
        exited = game.wait(lambda s: s["contactExitCount"] == 1, timeout=3)

        def after_exit(state):
            assert state["contactHealth"] == exited["contactHealth"], state
            assert state["contactOtherHealth"] == exited["contactOtherHealth"], state
            assert state["contactEnterCount"] == 1 and state["contactExitCount"] == 1, state
            assert state["contactStayCount"] == exited["contactStayCount"], state
            assert state["errorCount"] == 0, state["errors"]
            return state["contactTime"] >= exited["contactTime"] + .9

        game.wait(after_exit, timeout=3)
        game.screenshot("contact.png")
        return
    game.call("CreateSpawner", number not in (25, 26))
    if 15 <= number <= 19:
        reference = {15: "enemyPrefab", 16: "player", 17: "enemiesParent", 18: "gameplayCamera", 19: "settings"}[number]
        # Drop before enabling for object references, and after configuring for settings.
        if number != 19:
            game.call("DropReference", reference)
        game.call("SetWave", .1, 2, .1, 40)
        if number == 19:
            game.call("DropReference", reference)
            baseline = game.snapshot()["spawnedCount"]
        else:
            baseline = 0
        time.sleep(.5)
        assert game.snapshot()["spawnedCount"] == baseline
        if number == 17:
            assert game.snapshot()["aliveCount"] == 0
            game.call("ClearEnemies")
        return
    if number == 22:
        game.call("SetMinimumDistance", 100000.0)
    if number == 24:
        game.call("SetRandomSeed", 18941)
    cap = 1 if number == 23 else 0 if number == 21 else 32 if number == 24 else 10 if number == 20 else 2
    game.call("SetWave", -1.0 if number == 20 else .1, cap, .1, 40)
    if number == 22:
        time.sleep(.5)
        assert game.snapshot()["spawnedCount"] == 0
        return
    target = max(1, cap)
    state = game.wait(lambda s: s["spawnedCount"] >= target, timeout=12)
    if number in (14, 21, 25):
        time.sleep(.4)
        state = game.snapshot()
        assert len(state["positions"]) == target, state
        assert state["aliveCount"] == target, state
    game.call("SetSpawning", False)
    if number == 14:
        assert all(p["currentHealth"] == 40 and p["speed"] == pytest.approx(.1) for p in state["positions"])
    elif number == 20:
        times = [s["time"] for s in state["spawnSamples"]]
        assert len(times) >= 10
        assert all(b - a >= .03 for a, b in zip(times, times[1:])), times
    elif number == 23:
        game.call("SetWave", .1, 1, 3.0, 70)
        game.call("SetSpawning", False)
        state = game.snapshot()
        assert state["positions"] and all(p["speed"] == pytest.approx(3) and p["currentHealth"] == 70 for p in state["positions"])
    elif number == 24:
        sides = set()
        for point in state["spawnSamples"]:
            dx, dy = point["x"] - state["cameraX"], point["y"] - state["cameraY"]
            edge_x, edge_y = state["halfWidth"] + state["spawnMargin"], state["halfHeight"] + state["spawnMargin"]
            assert abs(dx) >= edge_x - .1 or abs(dy) >= edge_y - .1, point
            assert math.hypot(point["x"] - state["targetX"], point["y"] - state["targetY"]) >= 2.9
            assert point["z"] == pytest.approx(0)
            if abs(dx) >= edge_x - .1:
                sides.add("right" if dx > 0 else "left")
            if abs(dy) >= edge_y - .1:
                sides.add("top" if dy > 0 else "bottom")
        assert len(sides) == 4, sides
    if number in (14, 26):
        game.call("ClearEnemies")
        game.wait(lambda s: not s["positions"])
        assert game.snapshot()["aliveCount"] == 0
