import json
from pathlib import Path
import sys
import time

import pytest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from catalog import CASES


def pytest_addoption(parser):
    parser.addoption("--e2e-context", required=True, help="Created by E2E/run.py")


def pytest_generate_tests(metafunc):
    if "spec" in metafunc.fixturenames:
        context = json.loads(Path(metafunc.config.getoption("--e2e-context")).read_text(encoding="utf-8"))
        selected = [c for c in CASES if not context["selected"] or c["id"] in context["selected"]]
        metafunc.parametrize("spec", selected, ids=[c["id"] for c in selected])


@pytest.fixture
def game(request, spec):
    context_path = Path(request.config.getoption("--e2e-context"))
    context = json.loads(context_path.read_text(encoding="utf-8"))
    folder = context_path.parent / spec["id"]
    folder.mkdir(exist_ok=True)
    request.node.e2e_folder = folder
    if spec["blocker"]:
        pytest.skip("BLOCKED: " + spec["blocker"])
    if context["environment"]["blockers"]:
        pytest.skip("BLOCKED: " + "; ".join(context["environment"]["blockers"]))
    from game import Game, ConnectionUnavailable
    instance = Game(context, folder)
    request.node.e2e_game = instance
    # Register before start so connection/setup failures still clean up their player.
    request.addfinalizer(instance.stop)
    try:
        instance.start()
    except ConnectionUnavailable as exc:
        if instance.process is not None and instance.process.poll() is not None:
            pytest.fail(f"Player crashed before connection: {exc}")
        pytest.skip(f"BLOCKED: AltTester connection/startup unavailable: {exc}")
    instance.healthy()
    yield instance
    instance.healthy()


@pytest.hookimpl(hookwrapper=True)
def pytest_runtest_makereport(item, call):
    outcome = yield
    rep = outcome.get_result()
    reports = getattr(item, "e2e_reports", [])
    reports.append(rep)
    item.e2e_reports = reports
    instance = getattr(item, "e2e_game", None)
    folder = getattr(item, "e2e_folder", None)
    if rep.failed and instance and instance.driver:
        try:
            instance.screenshot(f"failure-{rep.when}.png")
        except Exception as exc:
            (folder / "screenshot-error.txt").write_text(str(exc), encoding="utf-8")
    if rep.when != "teardown":
        return
    spec = item.callspec.params["spec"]
    failures = [r for r in reports if r.failed]
    skips = [r for r in reports if r.skipped]
    if failures:
        status = "FAIL"
        actual = "\n".join(f"{r.when}: {r.longrepr}" for r in failures)
    elif skips:
        actual = "\n".join(str(r.longrepr) for r in skips)
        status = "BLOCKED" if "BLOCKED:" in actual else "SKIP"
    elif any(r.when == "call" and r.passed for r in reports):
        status = "PASS"
        actual = "Các assertion của case và kiểm tra log teardown đã đạt; xem observations.json và player log."
    else:
        status, actual = "BLOCKED", "Không có call-phase result."
    context = Path(item.config.getoption("--e2e-context"))
    artifacts = [p.relative_to(context.parent).as_posix() for p in sorted(folder.glob("*"))] if folder else []
    record = dict(id=spec["id"], status=status, actual=actual, artifacts=artifacts,
                  call_executed=any(r.when == "call" for r in reports),
                  alt_connected=bool(instance and instance.connection_verified),
                  duration=sum(r.duration for r in reports))
    with (context.parent / "events.jsonl").open("a", encoding="utf-8") as stream:
        stream.write(json.dumps(record, ensure_ascii=False) + "\n")


def pytest_sessionfinish(session, exitstatus):
    events = Path(session.config.getoption("--e2e-context")).parent / "events.jsonl"
    if events.exists():
        rows = [json.loads(line) for line in events.read_text(encoding="utf-8").splitlines()]
        if any(row["status"] in ("BLOCKED", "SKIP") for row in rows) and session.exitstatus == 0:
            session.exitstatus = 2
