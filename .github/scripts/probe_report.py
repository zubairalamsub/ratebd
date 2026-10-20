"""Prints one GitHub annotation per corridor snapshot in a directory, listing every provider's status and rates.

GitHub keeps at most 10 annotations of each level per step, so providers are grouped by corridor.
"""
import json
import pathlib
import sys

for path in sorted(pathlib.Path(sys.argv[1]).glob("*.json")):
    s = json.loads(path.read_text(encoding="utf-8"))
    rows = []
    for p in (s.get("providers") or []) + (s.get("postedRateProviders") or []):
        if p["name"] == "MidMarket":
            continue
        rates = [q["rate"] for q in (s.get("quotes") or []) + (s.get("postedRates") or []) if q["provider"] == p["name"]]
        text = f'{p["name"]} {p["status"]} {",".join(str(r) for r in rates)}'.rstrip()
        if p.get("message"):
            text += " (" + " ".join(p["message"].split()) + ")"
        rows.append((p["status"] == "ok", text))
    if rows:
        level = "notice" if all(ok for ok, _ in rows) else "error"
        print(f'::{level} title={s["corridor"]}::' + "; ".join(text for _, text in rows))
