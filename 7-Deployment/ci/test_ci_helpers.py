#!/usr/bin/env python3
"""Unit tests for 7-Deployment/ci/ci_helpers.py."""

import pathlib
import sys
import tempfile
import unittest

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import ci_helpers  # noqa: E402


class ValidateDocsTests(unittest.TestCase):
    def _make_repo(self, root: pathlib.Path, *, with_agents: bool = True, with_todo_readme: bool = True) -> None:
        if with_agents:
            (root / "AGENTS.md").write_text("# Agents\n[Guide](6-Docs/guide.md)\n", encoding="utf-8")
        docs = root / "6-Docs"
        todo = docs / "todo"
        todo.mkdir(parents=True)
        (docs / "guide.md").write_text("# Guide\n", encoding="utf-8")
        if with_todo_readme:
            (todo / "README.md").write_text("# Todos\n", encoding="utf-8")

    def test_valid_links_with_titles_queries_angles_and_schemes_pass(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            root = pathlib.Path(tmp)
            self._make_repo(root)
            (root / "6-Docs" / "guide.md").write_text(
                "\n".join(
                    [
                        '[With title](../AGENTS.md "Agents title")',
                        "[With single title](../AGENTS.md 'Single')",
                        "[With query](../AGENTS.md?plain=1#top)",
                        "[With angles](<../AGENTS.md>)",
                        "[Telephone](tel:+15550100)",
                        "[FTP](ftp://files.example.com/archive.tar.gz)",
                        "[Mail](mailto:dev@example.com)",
                        "[HTTPS](https://example.com/path?a=1#b)",
                        "[Anchor only](#section)",
                    ]
                ),
                encoding="utf-8",
            )
            checked, errors = ci_helpers.validate_docs(root)
            self.assertEqual(errors, [])
            self.assertEqual(checked, 3)

    def test_broken_link_is_reported(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            root = pathlib.Path(tmp)
            self._make_repo(root)
            (root / "6-Docs" / "guide.md").write_text('[Missing](missing.md "title")', encoding="utf-8")
            _, errors = ci_helpers.validate_docs(root)
            self.assertEqual(len(errors), 1)
            self.assertIn("missing.md", errors[0])

    def test_links_with_parentheses_are_not_truncated(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            root = pathlib.Path(tmp)
            self._make_repo(root)
            (root / "6-Docs" / "file_(v2).md").write_text("# V2\n", encoding="utf-8")
            (root / "6-Docs" / "guide.md").write_text(
                "\n".join(
                    [
                        "[Parens](file_(v2).md)",
                        '[Parens with title](file_(v2).md "Title (v2)")',
                        "[External parens](https://en.wikipedia.org/wiki/Foo_(bar))",
                    ]
                ),
                encoding="utf-8",
            )
            _, errors = ci_helpers.validate_docs(root)
            self.assertEqual(errors, [])

    def test_todo_index_does_not_match_substrings(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            root = pathlib.Path(tmp)
            self._make_repo(root)
            (root / "6-Docs" / "todo" / "1.md").write_text("# 1\n", encoding="utf-8")
            (root / "6-Docs" / "todo" / "11.md").write_text("# 11\n", encoding="utf-8")
            (root / "6-Docs" / "todo" / "README.md").write_text(
                "# Todos\n- [Todo 11](11.md)\n",
                encoding="utf-8",
            )
            _, errors = ci_helpers.validate_docs(root)
            self.assertTrue(any("1.md" in err for err in errors))

    def test_missing_agents_returns_error_instead_of_traceback(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            root = pathlib.Path(tmp)
            self._make_repo(root, with_agents=False)
            _, errors = ci_helpers.validate_docs(root)
            self.assertTrue(any("AGENTS.md" in err for err in errors))

    def test_missing_todo_readme_returns_error_instead_of_traceback(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            root = pathlib.Path(tmp)
            self._make_repo(root, with_todo_readme=False)
            (root / "6-Docs" / "todo" / "001.md").write_text("# Item", encoding="utf-8")
            _, errors = ci_helpers.validate_docs(root)
            self.assertTrue(any("todo/README.md" in err for err in errors))


class NuGetVulnerabilityTests(unittest.TestCase):
    def test_clean_report_returns_no_findings(self) -> None:
        report = {
            "version": 1,
            "projects": [{"path": "/repo/App.csproj"}],
        }
        problems, vulns = ci_helpers.find_nuget_vulnerabilities(report)
        self.assertEqual(problems, [])
        self.assertEqual(vulns, [])

    def test_detects_top_level_and_transitive_vulnerabilities_and_problems(self) -> None:
        report = {
            "version": 1,
            "projects": [
                {
                    "path": "/repo/App.csproj",
                    "problems": [{"text": "Unable to resolve project"}],
                    "frameworks": [
                        {
                            "framework": "net10.0",
                            "topLevelPackages": [
                                {
                                    "id": "Pkg.A",
                                    "resolvedVersion": "1.0.0",
                                    "vulnerabilities": [{"severity": "High", "advisoryurl": "https://example.test/1"}],
                                }
                            ],
                            "transitivePackages": [
                                {
                                    "id": "Pkg.B",
                                    "resolvedVersion": "2.1.0",
                                    "vulnerabilities": [{"severity": "Critical", "advisoryurl": "https://example.test/2"}],
                                }
                            ],
                        }
                    ],
                }
            ],
        }
        problems, vulns = ci_helpers.find_nuget_vulnerabilities(report)
        self.assertEqual(len(problems), 1)
        self.assertEqual(len(vulns), 2)
        self.assertEqual({v["id"] for v in vulns}, {"Pkg.A", "Pkg.B"})

    def test_json_null_package_lists_do_not_crash(self) -> None:
        report = {
            "version": 1,
            "projects": [
                {
                    "path": "/repo/App.csproj",
                    "problems": [{"text": "Restore failed"}],
                    "frameworks": [
                        {
                            "framework": "net10.0",
                            "topLevelPackages": None,
                            "transitivePackages": None,
                        }
                    ],
                }
            ],
        }
        problems, vulns = ci_helpers.find_nuget_vulnerabilities(report)
        self.assertEqual(len(problems), 1)
        self.assertEqual(vulns, [])


class ClassifyReleasePathsTests(unittest.TestCase):
    def test_docs_only_and_dashboard_only_do_not_deploy(self) -> None:
        self.assertEqual(
            ci_helpers.classify_release_paths(["6-Docs/README.md", "README.md"], "workflow_run"),
            (False, False),
        )
        self.assertEqual(
            ci_helpers.classify_release_paths(["1-Presentation/admin-dashboard/src/App.tsx"], "workflow_run"),
            (False, False),
        )

    def test_api_change_deploys_api_without_infra(self) -> None:
        self.assertEqual(
            ci_helpers.classify_release_paths(["1-Presentation/HotshotLogistics.Api/Program.cs"], "workflow_run"),
            (False, True),
        )

    def test_terraform_change_deploys_infra_and_api(self) -> None:
        self.assertEqual(
            ci_helpers.classify_release_paths(["7-Deployment/Azure-deploy/solution/HotshotLogistics/main.tf"], "workflow_run"),
            (True, True),
        )

    def test_dispatch_respects_skip_terraform(self) -> None:
        self.assertEqual(
            ci_helpers.classify_release_paths([], "workflow_dispatch", skip_terraform=True),
            (False, True),
        )
        self.assertEqual(
            ci_helpers.classify_release_paths([], "workflow_dispatch", skip_terraform=False),
            (True, True),
        )

    def test_diff_unavailable_fails_safe(self) -> None:
        self.assertEqual(
            ci_helpers.classify_release_paths(None, "workflow_run"),
            (True, True),
        )


if __name__ == "__main__":
    unittest.main()
