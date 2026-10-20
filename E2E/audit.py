"""Evidence inventory, not a compiler or a claim of complete semantic discovery."""
import hashlib
import json
import re
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def test_build_fingerprint(root=ROOT):
    recipe = sorted((root / "E2E/Unity").rglob("*.cs")) + [root / "E2E/run.py", root / "E2E/audit.py"]
    hashes = [(p.relative_to(root).as_posix(), hashlib.sha256(p.read_bytes()).hexdigest()) for p in recipe if p.is_file()]
    return hashlib.sha256(json.dumps(hashes, sort_keys=True).encode()).hexdigest()


def inspect_project(root=ROOT):
    files = []
    for folder in ("Assets", "Packages", "ProjectSettings"):
        for path in sorted((root / folder).rglob("*")):
            if path.is_file():
                files.append(dict(path=path.relative_to(root).as_posix(), size=path.stat().st_size,
                                  sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
    fingerprint = hashlib.sha256(json.dumps(files, sort_keys=True).encode()).hexdigest()
    scripts = list((root / "Assets").rglob("*.cs"))
    text = "\n".join(p.read_text(encoding="utf-8-sig") for p in scripts)
    missing = [name for name in ("PlayerHealth", "WaveSettings")
               if re.search(r"\b" + name + r"\b", text) and
               not re.search(r"\b(class|struct|record)\s+" + name + r"\b", text)]
    scenes = re.findall(r"enabled: 1\s+path: (.+)", (root / "ProjectSettings/EditorBuildSettings.asset").read_text())
    try:
        revision = subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=root, text=True).strip()
    except (OSError, subprocess.CalledProcessError):
        revision = "Unknown / Not verified"
    inventory = dict(files=files, fingerprint=fingerprint, test_build_fingerprint=test_build_fingerprint(root), revision=revision,
                     scripts=[p.relative_to(root).as_posix() for p in scripts],
                     scenes=scenes, missing_source_types=missing,
                     unity_version=re.search(r"m_EditorVersion: (.+)", (root / "ProjectSettings/ProjectVersion.txt").read_text())[1],
                     limitations="Inventory/hash + các rule kiểm tra tĩnh; không thay Unity compiler/Inspector/runtime.")
    inventory["source_api"] = [dict(path=p.relative_to(root).as_posix(),
        declarations=re.findall(r"\b(?:class|struct|enum|interface)\s+(\w+)", p.read_text(encoding="utf-8-sig")),
        methods=re.findall(r"\b(?:public|private|protected)\s+(?:static\s+)?[\w<>\[\]]+\s+(\w+)\s*\(", p.read_text(encoding="utf-8-sig"))) for p in scripts]
    inventory["scene_prefab_objects"] = {p.relative_to(root).as_posix(): re.findall(r"^  m_Name: (.+)$", p.read_text(encoding="utf-8-sig"), re.M)
        for pattern in ("*.unity", "*.prefab") for p in (root / "Assets").rglob(pattern)}
    guids = {}
    for meta in (root / "Assets").rglob("*.meta"):
        match = re.search(r"^guid: (\w+)", meta.read_text(encoding="utf-8-sig"), re.M)
        if match:
            guids[match[1]] = str(meta.relative_to(root))[:-5]
    inventory["script_references"] = []
    for pattern in ("*.unity", "*.prefab"):
        for path in (root / "Assets").rglob(pattern):
            for line, content in enumerate(path.read_text(encoding="utf-8-sig").splitlines(), 1):
                match = re.search(r"m_Script:.*guid: (\w+)", content)
                if match:
                    guid = match[1]
                    inventory["script_references"].append(dict(asset=path.relative_to(root).as_posix(), line=line, guid=guid,
                        local_asset=guids.get(guid), resolution="Assets source" if guid in guids else "Package or missing — verify Unity import"))
    actions = root / "Assets/Settings/InputSystem_Actions.inputactions"
    inventory["input_maps"] = [{"name": m["name"], "actions": [a["name"] for a in m["actions"]], "bindings": len(m["bindings"])}
                               for m in json.loads(actions.read_text())["maps"]] if actions.exists() else []
    inventory["package_dependencies"] = json.loads((root / "Packages/manifest.json").read_text())["dependencies"]
    return inventory


def save_inventory(out):
    inventory = inspect_project()
    (out / "inventory.json").write_text(json.dumps(inventory, ensure_ascii=False, indent=2), encoding="utf-8")
    return inventory
