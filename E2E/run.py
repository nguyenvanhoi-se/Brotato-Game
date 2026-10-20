"""CLI orchestrator. Reports unavailable execution honestly and exits nonzero."""
import argparse
import importlib.util
import json
import os
from pathlib import Path
import re
import shutil
import socket
import subprocess
import sys
import tempfile
from datetime import datetime, timezone

# bootstrap.py installs here for machines without an editable Python environment.
_deps = Path(__file__).resolve().parents[1] / ".e2e/deps"
if _deps.is_dir():
    sys.path.insert(0, str(_deps))
    os.environ["PYTHONPATH"] = str(_deps) + os.pathsep + os.environ.get("PYTHONPATH", "")

from audit import ROOT, save_inventory, inspect_project
from catalog import CASES
from processes import run_logged, stop_owned
from reporting import report


def arguments():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("--build", type=Path, help="Existing Windows instrumented executable (with .e2e.json)")
    p.add_argument("--build-game", action="store_true", help="Clone inputs and build before pytest")
    p.add_argument("--unity", type=Path)
    p.add_argument("--sdk", type=Path, default=ROOT / ".e2e/sdk/Assets/AltTester")
    p.add_argument("--desktop-command", help="JSON argv to start an installed/licensed AltTester Desktop; otherwise use existing server")
    p.add_argument("--host", default="127.0.0.1")
    p.add_argument("--port", default=13000, type=int)
    p.add_argument("--app-name", default="BrotatoE2E")
    p.add_argument("--out", type=Path)
    p.add_argument("--audit-only", action="store_true")
    p.add_argument("--build-timeout", type=int, default=900)
    p.add_argument("--suite-timeout", type=int, default=900)
    p.add_argument("--case", action="append", default=[], help="Select exact case ID, repeatable; rest remain Not Tested")
    return p.parse_args()


def build(args, out, inventory):
    unity = args.unity or Path(f'C:/Program Files/Unity/Hub/Editor/{inventory["unity_version"]}/Editor/Unity.exe')
    if not unity.is_file():
        raise RuntimeError(f"Unity Editor {inventory['unity_version']} không tìm thấy: {unity}")
    sdk_version = args.sdk / "package.json"
    if not sdk_version.is_file() or json.loads(sdk_version.read_text())["version"] != "2.3.2":
        raise RuntimeError("Cần SDK chính thức 2.3.2 tại --sdk; xem E2E/README.md.")
    parent = ROOT / ".e2e/projects"
    parent.mkdir(parents=True, exist_ok=True)
    clone = Path(tempfile.mkdtemp(prefix="run-", dir=parent))
    for name in ("Assets", "Packages", "ProjectSettings"):
        shutil.copytree(ROOT / name, clone / name)
    if inspect_project(clone)["fingerprint"] != inventory["fingerprint"]:
        raise RuntimeError("Source changed while copying; rerun on a stable snapshot.")
    (clone / ".brotato-e2e-clone").write_text(str(ROOT), encoding="utf-8")
    shutil.copytree(ROOT / "E2E/Unity", clone / "Assets/E2E")
    embedded = clone / "Packages/com.alttester.sdk"
    if embedded.exists():
        raise RuntimeError("Clone đã chứa com.alttester.sdk; kiểm tra package hiện hữu trước khi tích hợp.")
    shutil.copytree(args.sdk, embedded)
    manifest_path = clone / "Packages/manifest.json"
    manifest = json.loads(manifest_path.read_text())
    manifest["dependencies"]["com.alttester.sdk"] = "file:com.alttester.sdk"
    manifest_path.write_text(json.dumps(manifest, indent=2), encoding="utf-8")
    exe = out / "build/BrotatoE2E.exe"
    exe.parent.mkdir(parents=True, exist_ok=True)
    env = dict(os.environ, BROTATO_E2E_OUTPUT=str(exe), BROTATO_E2E_HOST=args.host,
               BROTATO_E2E_PORT=str(args.port), BROTATO_E2E_APP_NAME=args.app_name,
               BROTATO_E2E_SOURCE_REVISION=inventory["revision"],
               BROTATO_E2E_TEST_BUILD_FINGERPRINT=inventory["test_build_fingerprint"],
               BROTATO_E2E_SOURCE_FINGERPRINT=inventory["fingerprint"])
    common = [str(unity), "-batchmode", "-nographics", "-quit", "-projectPath", str(clone), "-buildTarget", "Win64"]
    configure = common + ["-executeMethod", "Brotato.E2E.Editor.E2EConfigure.Configure",
                          "-logFile", str(out / "unity-configure.log")]
    configure_code = run_logged(configure, out / "unity-configure-launcher.log", args.build_timeout, env=env)
    if configure_code != 0:
        raise RuntimeError(f"Unity configure/compile không thành công (exit={configure_code}); xem unity-configure.log.")
    cmd = common + [
           "-executeMethod", "Brotato.E2E.Editor.E2EBuild.Build", "-logFile", str(out / "unity-build.log")]
    (out / "build-command.json").write_text(json.dumps(dict(argv=cmd, configure_argv=configure, clone=str(clone)), indent=2), encoding="utf-8")
    code = run_logged(cmd, out / "unity-launcher.log", args.build_timeout, env=env)
    if code != 0 or not exe.is_file() or not Path(str(exe) + ".e2e.json").is_file():
        log = out / "unity-build.log"
        errors = list(dict.fromkeys(re.findall(r"^.*error CS\d+:.*$", log.read_text(encoding="utf-8", errors="replace"), re.M))) if log.exists() else []
        detail = "\n" + "\n".join(errors[:10]) if errors else ""
        raise RuntimeError(f"Unity build không thành công (exit={code}); xem unity-build.log/unity-launcher.log.{detail}")
    return exe


def main():
    args = arguments()
    unknown = set(args.case) - {c["id"] for c in CASES}
    if unknown:
        raise SystemExit(f"Unknown case IDs: {sorted(unknown)}")
    out = (args.out or ROOT / ".e2e/reports" / datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%S%fZ")).resolve()
    if out.exists() and any(out.iterdir()):
        raise SystemExit("Output directory must be empty to avoid mixing evidence from earlier runs.")
    out.mkdir(parents=True, exist_ok=True)
    inventory = save_inventory(out)
    environment = dict(python=sys.version, unity_version=inventory["unity_version"],
                       build_status="Not Attempted", server_tcp_reachable=False,
                       revision=inventory["revision"], source_fingerprint=inventory["fingerprint"],
                       test_build_fingerprint=inventory["test_build_fingerprint"],
                       missing_source_types=inventory["missing_source_types"], blockers=[],
                       note="Static findings are not runtime failures; natural and simulated results remain separate.")
    if args.audit_only:
        report(out, [], environment, 0)
        print(f"AUDIT ONLY — no E2E execution: {out / 'report.html'}")
        return 0
    desktop = None
    records = []
    code = 2
    try:
        if args.build_game:
            try:
                args.build = build(args, out, inventory)
                environment["build_status"] = "PASS"
            except (RuntimeError, OSError, subprocess.TimeoutExpired) as exc:
                environment["build_status"] = "FAIL"
                environment["blockers"].append(str(exc))
        if args.build:
            args.build = args.build.resolve()
            evidence = Path(str(args.build) + ".e2e.json")
            if not args.build.is_file() or not evidence.is_file():
                environment["blockers"].append("Executable hoặc manifest .e2e.json chưa tồn tại.")
            else:
                meta = json.loads(evidence.read_text(encoding="utf-8-sig"))
                environment["build_manifest"] = meta
                for key, expected in (("sourceFingerprint", inventory["fingerprint"]), ("testBuildFingerprint", inventory["test_build_fingerprint"]), ("sdkVersion", "2.3.2"),
                                      ("appName", args.app_name), ("host", args.host), ("port", args.port)):
                    if meta.get(key) != expected:
                        environment["blockers"].append(f"Build manifest mismatch {key}: {meta.get(key)!r} != {expected!r}")
                if meta.get("harness") is not True:
                    environment["blockers"].append("Build không có test harness.")
                if not args.build_game:
                    environment["build_status"] = "REUSED" if not environment["blockers"] else "Invalid build"
        else:
            environment["blockers"].append("Không có test build. Dùng --build-game hoặc --build <exe>.")
        for module in ("pytest", "alttester"):
            if importlib.util.find_spec(module) is None:
                environment["blockers"].append(f"Thiếu Python dependency: {module}.")
        if args.desktop_command:
            command = json.loads(args.desktop_command)
            if not isinstance(command, list) or not command or not all(isinstance(s, str) for s in command):
                raise ValueError("--desktop-command must be a nonempty JSON string array")
            desktop = subprocess.Popen(command, stdout=(out / "desktop.log").open("w"), stderr=subprocess.STDOUT)
            # Server readiness is bounded; a listener alone never counts as AltTester connection success.
            import time
            deadline = time.monotonic() + 30
            while time.monotonic() < deadline:
                if desktop.poll() is not None:
                    break
                try:
                    with socket.create_connection((args.host, args.port), timeout=.5):
                        break
                except OSError:
                    time.sleep(.25)
        try:
            with socket.create_connection((args.host, args.port), timeout=2):
                environment["server_tcp_reachable"] = True
        except OSError as exc:
            environment["blockers"].append(f"AltTester Desktop/Server không kết nối được {args.host}:{args.port}: {exc}")
        environment["build"] = str(args.build) if args.build else None
        (out / "context.json").write_text(json.dumps(dict(environment=environment, host=args.host,
                  port=args.port, app_name=args.app_name, build=environment["build"], selected=args.case), ensure_ascii=False, indent=2), encoding="utf-8")
        if importlib.util.find_spec("pytest"):
            command = [sys.executable, "-m", "pytest", "-c", str(ROOT / "E2E/pytest.ini"), str(ROOT / "E2E/tests"),
                       "--e2e-context", str(out / "context.json"), "--junitxml", str(out / "pytest.xml")]
            try:
                code = run_logged(command, out / "pytest.log", args.suite_timeout, cwd=ROOT)
            except subprocess.TimeoutExpired:
                code = 2
                environment["blockers"].append("Pytest suite timeout; owned process tree terminated.")
        events = out / "events.jsonl"
        if events.exists():
            records = [json.loads(line) for line in events.read_text(encoding="utf-8").splitlines() if line.strip()]
        completed = {r["id"] for r in records}
        for c in CASES:
            if c["id"] not in completed and (not args.case or c["id"] in args.case):
                records.append(dict(id=c["id"], status="BLOCKED", actual="Pytest không hoàn tất case; " + "; ".join(environment["blockers"])))
        if environment["blockers"] or any(r["status"] in ("BLOCKED", "SKIP") for r in records):
            code = code or 2
        if any(r["status"] == "FAIL" for r in records):
            code = code or 1
    except (OSError, ValueError, RuntimeError) as exc:
        environment["blockers"].append(str(exc))
        code = 2
    finally:
        stop_owned(desktop)
        completed = {r["id"] for r in records}
        for c in CASES:
            if c["id"] not in completed and (not args.case or c["id"] in args.case):
                records.append(dict(id=c["id"], status="BLOCKED", actual="Execution unavailable: " + "; ".join(environment["blockers"])))
        payload = report(out, records, environment, code)
        print(json.dumps(payload["summary"], ensure_ascii=False))
        print(f"Report: {out / 'report.html'}; exit={code}")
    return code


if __name__ == "__main__":
    raise SystemExit(main())
