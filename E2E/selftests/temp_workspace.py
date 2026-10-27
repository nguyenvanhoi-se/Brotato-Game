from contextlib import contextmanager
from pathlib import Path
import tempfile


@contextmanager
def temporary_workspace():
    parent = Path(__file__).resolve().parents[2] / ".e2e/selftests"
    parent.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory(dir=parent) as folder:
        # Validate the generated absolute target before TemporaryDirectory recursively cleans it.
        if not Path(folder).resolve().is_relative_to(parent.resolve()):
            raise RuntimeError("Temporary cleanup escaped the intended selftest workspace")
        yield folder
