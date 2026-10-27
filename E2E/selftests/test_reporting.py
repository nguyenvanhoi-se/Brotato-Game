"""Infrastructure checks only. No Unity, driver, or game outcome is simulated as a PASS."""
import json
from pathlib import Path
import sys
import unittest
from temp_workspace import temporary_workspace
from xml.etree import ElementTree as ET

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from catalog import CASES
from reporting import report


class ReportingTests(unittest.TestCase):
    def test_blocked_is_never_pass_and_junit_preserves_reason(self):
        with temporary_workspace() as folder:
            out = Path(folder)
            rows = [dict(id=c["id"], status="BLOCKED", actual="Unity Editor unavailable <not a game failure>") for c in CASES]
            data = report(out, rows, {"blockers": ["missing Editor"]}, 2)
            self.assertEqual(data["summary"]["PASS"], 0)
            self.assertEqual(data["summary"]["BLOCKED"], len(CASES))
            self.assertEqual(ET.parse(out / "junit.xml").getroot().attrib["skipped"], str(len(CASES)))
            props = ET.parse(out / "junit.xml").findall(".//property[@name='status']")
            self.assertTrue(all(p.attrib["value"] == "BLOCKED" for p in props))
            self.assertIn("&lt;not a game failure&gt;", (out / "report.html").read_text(encoding="utf-8"))

    def test_failure_keeps_actual_results_and_unselected_are_not_tested(self):
        with temporary_workspace() as folder:
            out = Path(folder)
            report(out, [dict(id=CASES[0]["id"], status="FAIL", actual="camera missing")], {}, 1)
            data = json.loads((out / "results.json").read_text(encoding="utf-8"))
            self.assertEqual(data["summary"]["FAIL"], 1)
            self.assertEqual(data["summary"]["NOT TESTED"], len(CASES) - 1)
            self.assertEqual(data["cases"][0]["actual"], "camera missing")
            self.assertEqual(data["cases"][0]["coverage"], "Failed")
            self.assertEqual(len(ET.parse(out / "junit.xml").findall(".//failure")), 1)
            self.assertTrue(data["coverage_gaps"])

    def test_duplicate_cannot_overwrite_a_failure(self):
        with temporary_workspace() as folder:
            with self.assertRaises(ValueError):
                report(Path(folder), [dict(id=CASES[0]["id"], status="FAIL", actual="real failure"),
                                     dict(id=CASES[0]["id"], status="PASS", actual="later result")], {}, 1)


if __name__ == "__main__":
    unittest.main()
