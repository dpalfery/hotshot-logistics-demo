#!/usr/bin/env python3
"""Shared CI and release workflow helper checks."""

from __future__ import annotations

import argparse
import json
import pathlib
import re
import sys
import urllib.parse
from typing import Iterable

LINK_PATTERN = re.compile(r"\[[^\]]*\]\(([^)]+)\)")
SCHEME_PATTERN = re.compile(r"^[a-zA-Z][a-zA-Z0-9+.-]*:")
INFRA_PATH_PATTERN = re.compile(
    r"^(?:7-Deployment/Azure-deploy/|\.github/workflows/release\.yml$)"
)
API_PATH_PATTERN = re.compile(
    r"^(?:0-Base/|1-Presentation/HotshotLogistics\.Api/|2-Application/|3-Domain/|"
    r"4-Persistence/|7-Deployment/Azure-deploy/|\.github/workflows/release\.yml$|"
    r"Directory\.Build\.props$|global\.json$|nuget\.config$|HotshotLogistics\.slnx?$)"
)


def _extract_local_link_path(raw_destination: str) -> str | None:
    """Return the local relative path from a markdown link destination, or None to skip."""
    dest = raw_destination.strip()
    if not dest:
        return None

    if dest.startswith("<") and ">" in dest:
        target = dest[1 : dest.index(">")].strip()
    else:
        target = dest.split(None, 1)[0]

    if not target or target.startswith("#") or SCHEME_PATTERN.match(target):
        return None

    target = target.split("#", 1)[0].split("?", 1)[0].strip()
    return urllib.parse.unquote(target) if target else None


def validate_docs(repo_root: pathlib.Path) -> tuple[int, list[str]]:
    """Validate local markdown links and 6-Docs/todo/README.md coverage."""
    errors: list[str] = []
    agents_file = repo_root / "AGENTS.md"
    docs_root = repo_root / "6-Docs"

    files: list[pathlib.Path] = []
    if agents_file.is_file():
        files.append(agents_file)
    else:
        errors.append("Missing required governance file: AGENTS.md")

    if docs_root.is_dir():
        files.extend(
            path for path in docs_root.rglob("*.md") if "archive" not in path.parts
        )
    else:
        errors.append("Missing required documentation directory: 6-Docs/")

    for md_file in files:
        text = md_file.read_text(encoding="utf-8")
        for match in LINK_PATTERN.finditer(text):
            local_target = _extract_local_link_path(match.group(1))
            if local_target is None:
                continue
            resolved = (md_file.parent / local_target).resolve()
            if not resolved.exists():
                errors.append(
                    f"Broken link in {md_file.relative_to(repo_root)}: {match.group(1)}"
                )

    todo_dir = docs_root / "todo"
    if todo_dir.is_dir():
        todo_readme = todo_dir / "README.md"
        if not todo_readme.is_file():
            errors.append("Missing required todo index: 6-Docs/todo/README.md")
        else:
            todo_index = todo_readme.read_text(encoding="utf-8")
            missing_todos = [
                path.name
                for path in sorted(todo_dir.glob("*.md"))
                if path.name != "README.md" and path.name not in todo_index
            ]
            if missing_todos:
                errors.append(
                    f"Todos missing from 6-Docs/todo/README.md: {missing_todos}"
                )

    return len(files), errors


def find_nuget_vulnerabilities(report: dict) -> tuple[list[str], list[dict]]:
    """Extract restore problems and vulnerable packages from dotnet list JSON output."""
    problems: list[str] = []
    findings: list[dict] = []

    for project in report.get("projects", []):
        project_path = project.get("path", "<unknown>")
        for prob in project.get("problems", []):
            text = prob.get("text") if isinstance(prob, dict) else str(prob)
            problems.append(f"{project_path}: {text}")

        for fw in project.get("frameworks", []):
            fw_name = fw.get("framework", "<unknown>")
            packages = fw.get("topLevelPackages", []) + fw.get("transitivePackages", [])
            for pkg in packages:
                vulns = pkg.get("vulnerabilities") or []
                for vuln in vulns:
                    findings.append(
                        {
                            "project": project_path,
                            "framework": fw_name,
                            "id": pkg.get("id", "<unknown>"),
                            "version": pkg.get("resolvedVersion", "<unknown>"),
                            "severity": vuln.get("severity", "<unknown>"),
                            "advisory": vuln.get("advisoryurl", ""),
                        }
                    )

    return problems, findings


def classify_release_paths(
    changed_files: Iterable[str] | None,
    event_name: str,
    skip_terraform: bool = False,
) -> tuple[bool, bool]:
    """Return (deploy_infra, deploy_api) for Release workflow triggers."""
    if event_name == "workflow_dispatch":
        return (not skip_terraform, True)
    if event_name == "push":
        return (True, True)
    if changed_files is None:
        return (True, True)

    files = [path.strip() for path in changed_files if path.strip()]
    deploy_infra = any(INFRA_PATH_PATTERN.search(path) for path in files)
    deploy_api = deploy_infra or any(API_PATH_PATTERN.search(path) for path in files)
    return (deploy_infra, deploy_api)


def _main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="CI helper commands")
    sub = parser.add_subparsers(dest="command", required=True)

    docs_cmd = sub.add_parser("check-docs")
    docs_cmd.add_argument("--root", default=".", type=pathlib.Path)

    sub.add_parser("check-nuget-json")

    rel_cmd = sub.add_parser("classify-release")
    rel_cmd.add_argument("--event", required=True)
    rel_cmd.add_argument("--skip-terraform", action="store_true")
    rel_cmd.add_argument("--diff-unavailable", action="store_true")
    rel_cmd.add_argument("--files", nargs="*", default=[])

    args = parser.parse_args(argv)
    if args.command == "check-docs":
        checked, errors = validate_docs(args.root.resolve())
        if errors:
            for err in errors:
                print(err, file=sys.stderr)
            return 1
        print(f"Checked {checked} governed markdown files: 0 errors.")
        return 0

    if args.command == "check-nuget-json":
        report = json.load(sys.stdin)
        problems, vulns = find_nuget_vulnerabilities(report)
        for prob in problems:
            print(f"::error::NuGet vulnerability scan problem: {prob}", file=sys.stderr)
        for vuln in vulns:
            print(
                f"::error::Vulnerable package in {vuln['project']} ({vuln['framework']}): "
                f"{vuln['id']} {vuln['version']} - {vuln['severity']} ({vuln['advisory']})",
                file=sys.stderr,
            )
        return 1 if (problems or vulns) else 0

    if args.command == "classify-release":
        files = None if args.diff_unavailable else args.files
        infra, api = classify_release_paths(files, args.event, args.skip_terraform)
        print(f"deploy_infra={'true' if infra else 'false'}")
        print(f"deploy_api={'true' if api else 'false'}")
        return 0

    return 2


if __name__ == "__main__":
    raise SystemExit(_main())
