"""Build, launch, drive and report Unity player journeys without an external server."""
import argparse
from datetime import datetime, timezone
import importlib.util
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys
import tempfile

ROOT = Path(__file__).resolve().parents[1]
DEPS = ROOT / ".e2e/deps"
if DEPS.is_dir():
    sys.path.insert(0, str(DEPS))
    os.environ["PYTHONPATH"] = str(DEPS) + os.pathsep + os.environ.get("PYTHONPATH", "")
os.environ["PYTHONUTF8"] = "1"
from audit import inspect_project, save_inventory
from catalog import CASES
from processes import run_logged
from reporting import report


def arguments():
    parser = argparse.ArgumentParser(description=__doc__)
    mode = parser.add_mutually_exclusive_group()
    mode.add_argument("--build-game", action="store_true")
    mode.add_argument("--build", type=Path, help="Reuse matching independent test build")
    mode.add_argument("--audit-only", action="store_true")
    parser.add_argument("--unity", type=Path)
    parser.add_argument("--out", type=Path)
    parser.add_argument("--build-timeout", type=int, default=900)
    parser.add_argument("--suite-timeout", type=int, default=900)
    parser.add_argument("--case", action="append", default=[])
    return parser.parse_args()


def build(args, out, inventory):
    unity = args.unity or Path(f'C:/Program Files/Unity/Hub/Editor/{inventory["unity_version"]}/Editor/Unity.exe')
    if not unity.is_file():
        raise RuntimeError(f"Unity {inventory['unity_version']} executable missing: {unity}")
    parent = ROOT / ".e2e/projects"
    parent.mkdir(parents=True, exist_ok=True)
    clone = Path(tempfile.mkdtemp(prefix="unity-e2e-", dir=parent))
    for name in ("Assets", "Packages", "ProjectSettings"):
        shutil.copytree(ROOT / name, clone / name)
    if inspect_project(clone)["fingerprint"] != inventory["fingerprint"]:
        raise RuntimeError("Source changed while copying; retry on a stable snapshot")
    (clone / ".brotato-e2e-clone").write_text(str(ROOT), encoding="utf-8")
    shutil.copytree(ROOT / "E2E/Unity", clone / "Assets/E2E")
    exe = out / "build/BrotatoE2E.exe"
    exe.parent.mkdir(parents=True)
    env = dict(os.environ, BROTATO_E2E_OUTPUT=str(exe), BROTATO_E2E_SOURCE_REVISION=inventory["revision"],
               BROTATO_E2E_SOURCE_FINGERPRINT=inventory["fingerprint"],
               BROTATO_E2E_TEST_BUILD_FINGERPRINT=inventory["test_build_fingerprint"])
    common = [str(unity), "-batchmode", "-nographics", "-quit", "-projectPath", str(clone), "-buildTarget", "Win64"]
    commands = dict(clone=str(clone))
    for phase, method in (("configure", "Brotato.E2E.Editor.E2EConfigure.Configure"),
                          ("build", "Brotato.E2E.Editor.E2EBuild.Build")):
        command = common + ["-executeMethod", method, "-logFile", str(out / f"unity-{phase}.log")]
        commands[phase] = command
        (out / "build-command.json").write_text(json.dumps(commands, indent=2), encoding="utf-8")
        code = run_logged(command, out / f"unity-{phase}-launcher.log", args.build_timeout, env=env)
        if code != 0:
            log = out / f"unity-{phase}.log"
            text = log.read_text(encoding="utf-8", errors="replace") if log.exists() else ""
            errors = list(dict.fromkeys(re.findall(r"^.*error CS\d+:.*$", text, re.M)))
            raise RuntimeError(f"Unity {phase} exit={code}; see {log.name}\n" + "\n".join(errors[:10]))
    if not exe.is_file() or not Path(str(exe) + ".e2e.json").is_file():
        raise RuntimeError("Unity did not produce the executable + successful build manifest")
    return exe


def main():
    args = arguments()
    unknown = set(args.case) - {case["id"] for case in CASES}
    if unknown:
        raise SystemExit(f"Unknown case IDs: {sorted(unknown)}")
    out = (args.out or ROOT / ".e2e/reports" / datetime.now(timezone.utc).strftime("independent-%Y%m%dT%H%M%S%fZ")).resolve()
    if out.exists() and any(out.iterdir()):
        raise SystemExit("Output directory must be empty; previous evidence is preserved")
    out.mkdir(parents=True, exist_ok=True)
    inventory = save_inventory(out)
    environment = dict(python=sys.version, unity_version=inventory["unity_version"], build_status="Not Attempted",
                       revision=inventory["revision"], source_fingerprint=inventory["fingerprint"],
                       test_build_fingerprint=inventory["test_build_fingerprint"],
                       missing_source_types=inventory["missing_source_types"], blockers=[],
                       driver="unity-e2e-tcp", protocol_version="1",
                       note="Player connections use per-process loopback ports/tokens; no commercial server.")
    if args.audit_only:
        report(out, [], environment, 0)
        print(f"AUDIT ONLY — no runtime cases executed: {out / 'report.html'}")
        return 0
    records, code = [], 2
    try:
        if args.build_game:
            try:
                args.build = build(args, out, inventory)
                environment["build_status"] = "PASS"
            except (OSError, RuntimeError, subprocess.TimeoutExpired) as exc:
                environment["build_status"] = "FAIL"
                environment["blockers"].append(str(exc))
        if args.build:
            args.build = args.build.resolve()
            manifest = Path(str(args.build) + ".e2e.json")
            if not args.build.is_file() or not manifest.is_file():
                environment["blockers"].append("Missing executable or successful-build manifest")
            else:
                metadata = json.loads(manifest.read_text(encoding="utf-8-sig"))
                environment["build_manifest"] = metadata
                for name, expected in (("driver", "unity-e2e-tcp"), ("protocolVersion", "1"),
                    ("sourceFingerprint", inventory["fingerprint"]), ("testBuildFingerprint", inventory["test_build_fingerprint"]),
                    ("harness", True), ("scenePaths", inventory["scenes"])):
                    if metadata.get(name) != expected:
                        environment["blockers"].append(f"Build mismatch {name}: {metadata.get(name)!r} != {expected!r}")
                if not args.build_game:
                    environment["build_status"] = "PASS" if not environment["blockers"] else "Invalid build"
                    environment["build_mode"] = "Reused matching independently instrumented build"
        else:
            environment["blockers"].append("No test build; use --build-game or --build <exe>")
        if importlib.util.find_spec("pytest") is None:
            environment["blockers"].append("Missing pytest; run python E2E/bootstrap.py")
        environment["build"] = str(args.build) if args.build else None
        (out / "context.json").write_text(json.dumps(dict(environment=environment, build=environment["build"], selected=args.case),
                                              ensure_ascii=False, indent=2), encoding="utf-8")
        if importlib.util.find_spec("pytest"):
            command = [sys.executable, "-m", "pytest", "-c", str(ROOT / "E2E/pytest.ini"), str(ROOT / "E2E/tests"),
                       "--e2e-context", str(out / "context.json"), "--junitxml", str(out / "pytest.xml")]
            try:
                code = run_logged(command, out / "pytest.log", args.suite_timeout, cwd=ROOT)
            except subprocess.TimeoutExpired:
                environment["blockers"].append("Suite timeout; owned process tree terminated")
                code = 2
        events = out / "events.jsonl"
        if events.exists():
            records = [json.loads(line) for line in events.read_text(encoding="utf-8").splitlines() if line.strip()]
        if environment["blockers"] or any(row["status"] in ("BLOCKED", "SKIP") for row in records):
            code = code or 2
        if any(row["status"] == "FAIL" for row in records):
            code = code or 1
    except (OSError, ValueError, RuntimeError) as exc:
        environment["blockers"].append(str(exc))
        code = 2
    finally:
        completed = {row["id"] for row in records}
        for case in CASES:
            if case["id"] not in completed and (not args.case or case["id"] in args.case):
                records.append(dict(id=case["id"], status="BLOCKED", actual="Execution did not complete: " + "; ".join(environment["blockers"])))
        result = report(out, records, environment, code)
        print(json.dumps(dict(summary=result["summary"], execution=result["execution"]), ensure_ascii=False))
        print(f"Report: {out / 'report.html'}; exit={code}")
    return code


if __name__ == "__main__":
    raise SystemExit(main())
