"""Install dependencies into workspace only; never changes Unity's source project."""
from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
SDK_COMMIT = "0c432cb67bef7930b4a080467cf128de07f1f10d"


def main():
    deps = ROOT / ".e2e/deps"
    subprocess.run([sys.executable, "-m", "pip", "install", "--upgrade", "--target", str(deps),
                    "-r", str(ROOT / "E2E/requirements.txt")], check=True)
    sdk = ROOT / ".e2e/sdk"
    if not sdk.exists():
        subprocess.run(["git", "clone", "--depth", "1", "--branch", "2.3.2", "--filter=blob:none", "--sparse",
                        "https://github.com/alttester/AltTester-Unity-SDK.git", str(sdk)], check=True)
    actual = subprocess.check_output(["git", "-C", str(sdk), "rev-parse", "HEAD"], text=True).strip()
    if actual != SDK_COMMIT:
        raise SystemExit(f"SDK commit mismatch {actual}; expected {SDK_COMMIT}. Existing checkout was preserved.")
    subprocess.run(["git", "-C", str(sdk), "sparse-checkout", "set", "Assets/AltTester"], check=True)
    print("Dependencies ready. Install/start licensed AltTester Desktop 2.3.x, then python E2E/run.py --build-game")


if __name__ == "__main__":
    main()
