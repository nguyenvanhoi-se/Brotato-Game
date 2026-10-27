from pathlib import Path
import sys
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from identity import validate_identity


class IdentityTests(unittest.TestCase):
    def test_reject_different_process_session_or_build(self):
        for field, value in (("processId", 999), ("sessionNonce", "stale"),
                             ("sourceFingerprint", "old-source"), ("testBuildFingerprint", "old-hooks")):
            state = dict(processId=123, sessionNonce="current", sourceFingerprint="source", testBuildFingerprint="hooks")
            state[field] = value
            with self.subTest(field=field), self.assertRaises(AssertionError):
                validate_identity(state, 123, "current", "source", "hooks")
