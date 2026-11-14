#!/usr/bin/env python3
"""Append line and branch coverage metrics from a Cobertura XML report to GitHub Actions."""
import os
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

report = Path(sys.argv[1])
summary_path = os.environ.get("GITHUB_STEP_SUMMARY")

if not report.is_file():
    message = "Cobertura report was not generated. Check the test step above.\n"
else:
    root = ET.parse(report).getroot()
    line_rate = float(root.attrib.get("line-rate", 0)) * 100
    branch_rate = float(root.attrib.get("branch-rate", 0)) * 100
    lines_valid = int(root.attrib.get("lines-valid", 0))
    lines_covered = int(root.attrib.get("lines-covered", 0))
    branches_valid = int(root.attrib.get("branches-valid", 0))
    branches_covered = int(root.attrib.get("branches-covered", 0))
    message = (
        "## Test coverage\n\n"
        "| Metric | Covered | Total | Coverage |\n"
        "|---|---:|---:|---:|\n"
        f"| Lines | {lines_covered} | {lines_valid} | {line_rate:.2f}% |\n"
        f"| Branches | {branches_covered} | {branches_valid} | {branch_rate:.2f}% |\n"
    )

if summary_path:
    with open(summary_path, "a", encoding="utf-8") as summary:
        summary.write(message)
else:
    print(message)
