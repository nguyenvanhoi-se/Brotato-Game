def validate_identity(state, pid, nonce, source_fingerprint, test_fingerprint):
    assert state.get("processId") == pid, f"Wrong player PID: {state.get('processId')} != {pid}"
    assert state.get("sessionNonce") == nonce, "Wrong player session nonce; server routed a different app"
    assert state.get("sourceFingerprint") == source_fingerprint, "Runtime game source fingerprint mismatch"
    assert state.get("testBuildFingerprint") == test_fingerprint, "Runtime test hook/build fingerprint mismatch"
