"""Regression tests for scan.py. Run: python tests/run_tests.py (standard library only).

- server-app: a synthetic Server app that should trip a known set of catalog entries.
- server-app packed as a .rap: the same results should come back from inside the archive.
- clean-air-app: an aiR-ready app that should produce no hits at all.
"""
import json
import subprocess
import sys
import tempfile
import zipfile
from pathlib import Path

HERE = Path(__file__).resolve().parent
SCAN = HERE.parent / "scripts" / "scan.py"
FIX = HERE / "fixtures"

EXPECT_SERVER = {
    # entry id: minimum severity that must appear ("review" counts for review-only entries)
    "AGENT-001": "review", "AGENT-002": "review", "API-001": "high", "API-008": "high",
    "API-019": "high",  # overlaps API-001 on kCura.Relativity.Client by design
    "API-010": "medium", "DATA-001": "medium", "DATA-002": "high", "DATA-007": "high",
    "EH-001": "medium", "FILE-001": "medium", "PAGE-001": "medium", "PAGE-002": "high",
    "SCR-001": "high", "SDK-001": "high",
}
ORDER = ["high", "medium", "low", "review"]


def scan(target):
    with tempfile.TemporaryDirectory() as tmp:
        out = Path(tmp) / "r.json"
        proc = subprocess.run([sys.executable, str(SCAN), str(target), "--out", str(out)],
                              capture_output=True, text=True)
        if proc.returncode != 0:
            raise SystemExit(f"scan failed on {target}:\n{proc.stdout}\n{proc.stderr}")
        return json.loads(out.read_text(encoding="utf-8"))


def check_server(res, label):
    failures = []
    by_entry = {}
    for f in res["findings"]:
        by_entry.setdefault(f["entry_id"], []).append(f)
    for eid, worst in EXPECT_SERVER.items():
        hits = by_entry.get(eid, [])
        if not hits:
            failures.append(f"{label}: expected {eid}, got nothing")
            continue
        best = min(ORDER.index(f["severity"]) for f in hits)
        if best > ORDER.index(worst):
            failures.append(f"{label}: {eid} best severity {ORDER[best]}, expected {worst} or louder")
    # Specific behaviors.
    agent = by_entry.get("AGENT-001", [])
    if agent and not any("Workload Discovery" in n for f in agent for n in f["notes"]):
        failures.append(f"{label}: AGENT-001 should note the missing Workload Discovery endpoint")
    commented = [f for f in by_entry.get("DATA-001", []) if "kept for reference" in f["snippet"]
                 or f["snippet"].startswith("//")]
    if any(f["severity"] != "review" for f in commented):
        failures.append(f"{label}: commented-out GetDBContext should be review only")
    page2 = by_entry.get("PAGE-002", [])
    bundled = [f for f in page2 if "lib/knockout" in f["snippet"]]
    if any(f["severity"] != "review" for f in bundled):
        failures.append(f"{label}: a bundled library copy should be review, not a finding")
    api010 = by_entry.get("API-010", [])
    if not any(f["matched"] in ("CreateAsync", "UpdateAsync") for f in api010):
        failures.append(f"{label}: API-010 should flag CreateAsync/UpdateAsync next to IWorkspaceManager")
    unexpected = set(by_entry) - set(EXPECT_SERVER)
    if unexpected:
        failures.append(f"{label}: unexpected entries {sorted(unexpected)}")
    return failures


def main():
    failures = []
    res = scan(FIX / "server-app")
    failures += check_server(res, "server-app folder")

    with tempfile.TemporaryDirectory() as tmp:
        rap = Path(tmp) / "Contoso.Cleanup.rap"
        with zipfile.ZipFile(rap, "w") as zf:
            for p in (FIX / "server-app").rglob("*"):
                if p.is_file():
                    zf.write(p, p.relative_to(FIX / "server-app").as_posix())
        failures += check_server(scan(rap), "server-app .rap")

    clean = scan(FIX / "clean-air-app")
    for f in clean["findings"]:
        failures.append(f"clean-air-app: unexpected {f['entry_id']} {f['severity']} at "
                        f"{f['file']}:{f['line']} `{f['snippet'][:80]}`")

    if failures:
        print("\n".join("FAIL " + x for x in failures))
        return 1
    print(f"OK: server-app {len(res['findings'])} hits across "
          f"{len({f['entry_id'] for f in res['findings']})} entries, rap matches, clean app has 0 hits")
    return 0


if __name__ == "__main__":
    sys.exit(main())
