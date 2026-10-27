import os
import subprocess


def stop_owned(process):
    """Only terminate a Popen PID created by this harness; never kill by process name."""
    if process is None or process.poll() is not None:
        return
    if os.name == "nt":
        subprocess.run(["taskkill", "/PID", str(process.pid), "/T", "/F"],
                       stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, timeout=15)
    else:
        process.terminate()
    try:
        process.wait(timeout=10)
    except subprocess.TimeoutExpired:
        process.kill()
        process.wait(timeout=5)


def run_logged(command, log, timeout, env=None, cwd=None):
    with open(log, "w", encoding="utf-8") as stream:
        process = subprocess.Popen(command, stdout=stream, stderr=subprocess.STDOUT, env=env, cwd=cwd)
        try:
            return process.wait(timeout=timeout)
        finally:
            stop_owned(process)
