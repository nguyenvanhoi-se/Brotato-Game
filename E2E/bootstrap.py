"""Install the sole external test dependency into the workspace; no Unity packages."""
from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]

if __name__ == "__main__":
    subprocess.run([sys.executable, "-m", "pip", "install", "--upgrade", "--target", str(ROOT / ".e2e/deps"),
                    "-r", str(ROOT / "E2E/requirements.txt")], check=True)
    print("Ready: python E2E/run.py --build-game")
