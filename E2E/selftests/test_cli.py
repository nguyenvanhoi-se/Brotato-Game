"""Checks error propagation using real pytest and intentionally unavailable build; no game mocks."""
import json
from pathlib import Path
import subprocess
import sys
import unittest
from temp_workspace import temporary_workspace

ROOT = Path(__file__).resolve().parents[2]


class CLITests(unittest.TestCase):
    def test_unavailable_build_exits_nonzero_and_preserves_unselected(self):
        with temporary_workspace() as folder:
            out = Path(folder) / "run"
            result = subprocess.run([sys.executable, str(ROOT / "E2E/run.py"), "--case", "E2E-001",
                                     "--out", str(out)], cwd=ROOT,
                                    stdout=subprocess.PIPE, stderr=subprocess.STDOUT, timeout=45)
            self.assertEqual(result.returncode, 2, result.stdout)
            data = json.loads((out / "results.json").read_text(encoding="utf-8"))
            self.assertEqual(data["summary"]["PASS"], 0)
            self.assertEqual(data["summary"]["BLOCKED"], 1)
            self.assertEqual(data["summary"]["NOT TESTED"], len(data["cases"]) - 1)
            self.assertTrue((out / "junit.xml").exists())


if __name__ == "__main__":
    unittest.main()
