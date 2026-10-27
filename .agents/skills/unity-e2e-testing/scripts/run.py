"""Run the repository's maintained independent Unity E2E framework from any cwd."""
from pathlib import Path
import os
import runpy
import sys

root = next((parent for parent in Path(__file__).resolve().parents
             if (parent / "Assets").is_dir() and (parent / "E2E/run.py").is_file()), None)
if root is None:
    raise SystemExit("Unity E2E framework not found above this skill")
sys.path.insert(0, str(root / "E2E"))
os.chdir(root)
runpy.run_path(str(root / "E2E/run.py"), run_name="__main__")
