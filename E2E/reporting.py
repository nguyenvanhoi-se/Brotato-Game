import html
import json
from collections import Counter
from datetime import datetime, timezone
from pathlib import Path
from xml.etree import ElementTree as ET

from catalog import CASES, NOT_APPLICABLE, COVERAGE_GAPS


def report(out, records, environment, exit_code):
    if len({r["id"] for r in records}) != len(records):
        raise ValueError("Duplicate execution records; refusing to hide earlier results")
    if any(r["id"] not in {c["id"] for c in CASES} for r in records):
        raise ValueError("Execution record has no catalog case")
    by_id = {r["id"]: r for r in records}
    rows = []
    coverage_names = {"PASS": "Tested", "FAIL": "Failed", "BLOCKED": "Blocked",
                      "SKIP": "Not Tested", "NOT TESTED": "Not Tested"}
    for case in CASES:
        r = dict(case)
        r.update(by_id.get(case["id"], dict(status="NOT TESTED", actual="Chưa được thực thi.")))
        r["coverage"] = coverage_names[r["status"]]
        rows.append(r)
    counts = Counter(r["status"] for r in rows)
    summary = {s: counts[s] for s in ("PASS", "FAIL", "SKIP", "BLOCKED", "NOT TESTED")}
    execution = dict(build_status=environment.get("build_status", "Unknown / Not verified"),
                     alt_tester="Connected" if any(r.get("alt_connected") for r in records) else "Disconnected",
                     runtime_cases_executed=sum(bool(r.get("call_executed")) for r in records),
                     natural_executed=sum(bool(r.get("call_executed")) and r["kind"] == "natural" for r in rows),
                     simulated_executed=sum(bool(r.get("call_executed")) and r["kind"] == "simulated" for r in rows))
    payload = dict(generated_utc=datetime.now(timezone.utc).isoformat(), exit_code=exit_code,
                   summary=summary, execution=execution, environment=environment, cases=rows,
                   coverage_gaps=[dict(feature=f, flow=flow, reason=why, coverage="Not Tested") for f, flow, why in COVERAGE_GAPS],
                   not_applicable=[dict(feature=f, coverage="Not Applicable", reason=why) for f, why in NOT_APPLICABLE])
    (out / "results.json").write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")
    suite = ET.Element("testsuite", name="Brotato AltTester E2E", tests=str(len(rows)),
                       failures=str(counts["FAIL"]), skipped=str(counts["SKIP"] + counts["BLOCKED"] + counts["NOT TESTED"]))
    suite_properties = ET.SubElement(suite, "properties")
    for key, value in execution.items():
        ET.SubElement(suite_properties, "property", name=key, value=str(value))
    for r in rows:
        node = ET.SubElement(suite, "testcase", name=r["id"], classname=r["feature"], time=str(r.get("duration", 0)))
        props = ET.SubElement(node, "properties")
        for key in ("status", "coverage", "kind"):
            ET.SubElement(props, "property", name=key, value=r[key])
        if r["status"] == "FAIL":
            ET.SubElement(node, "failure", message=r["actual"]).text = r["actual"]
        elif r["status"] != "PASS":
            ET.SubElement(node, "skipped", type=r["status"], message=r["actual"])
        ET.SubElement(node, "system-out").text = r["actual"]
    ET.ElementTree(suite).write(out / "junit.xml", encoding="utf-8", xml_declaration=True)
    esc = lambda value: html.escape(str(value))
    body = ""
    for r in rows:
        steps = "<ol>" + "".join("<li>" + esc(s) + "</li>" for s in r["steps"]) + "</ol>"
        artifacts = " ".join(f'<a href="{esc(p)}">{esc(Path(p).name)}</a>' for p in r.get("artifacts", []))
        body += (f'<tr><td>{esc(r["feature"])}</td><td>{esc(r["id"])}<br>{esc(r["kind"])}</td>'
                 f'<td>{esc(r["goal"])}<details><summary>Preconditions / Steps</summary>{esc(r["preconditions"])}{steps}</details></td>'
                 f'<td>{esc(r["expected"])}</td><td><pre>{esc(r["actual"])}</pre>{artifacts}</td>'
                 f'<td class="{r["status"]}">{esc(r["status"])}</td><td>{r["coverage"]}</td></tr>')
    na = "".join(f"<li>{esc(f)}: Not Applicable — {esc(why)}</li>" for f, why in NOT_APPLICABLE)
    gaps = "".join(f"<li>{esc(f)} → {esc(flow)}: Not Tested — {esc(why)}</li>" for f, flow, why in COVERAGE_GAPS)
    evidence = " · ".join(f'<a href="{name}">{name}</a>' for name in
        ("inventory.json", "junit.xml", "pytest.xml", "pytest.log", "unity-configure.log", "unity-build.log", "unity-launcher.log", "build-command.json", "verification-evidence.json", "desktop-license-required.png") if (out / name).exists())
    page = ('<!doctype html><html lang="vi"><meta charset="utf-8"><title>Brotato E2E audit</title>'
            '<style>body{font:15px system-ui;margin:24px}table{border-collapse:collapse;width:100%}td,th{border:1px solid #aaa;padding:8px;vertical-align:top}pre{white-space:pre-wrap;max-width:650px}.FAIL{background:#ffcccc}.BLOCKED{background:#ffe4ae}.PASS{background:#ccedcc}</style>'
            f'<h1>Brotato E2E — bằng chứng thực thi</h1><p>{esc(summary)}; exit={exit_code}</p><p>{esc(execution)}</p><p>{evidence}</p>'
            '<p>Simulated = scenario qua hook, không tính là hành trình người chơi. BLOCKED được ánh xạ skipped trong JUnit, giữ status gốc trong properties/JSON. Không suy ra 100% coverage từ số case PASS.</p>'
            f'<details open><summary>Môi trường / giới hạn</summary><pre>{esc(json.dumps(environment, ensure_ascii=False, indent=2))}</pre></details>'
            '<table><tr><th>Feature</th><th>Test ID / loại</th><th>User flow / mục tiêu</th><th>Expected Results</th><th>Actual Results</th><th>Status</th><th>Coverage Status</th></tr>'
            + body + f'</table><h2>Coverage gaps đã biết</h2><ul>{gaps}</ul><h2>Feature chưa tồn tại / ngoài applicability hiện tại</h2><ul>{na}</ul></html>')
    (out / "report.html").write_text(page, encoding="utf-8")
    return payload
