"""Behavior checks for the read-only baseline verifier, using isolated copies."""
import hashlib
import json
import shutil
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
VERIFIER = Path(__file__).with_name("verify_baseline.py")


class BaselineVerifierTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.storage = tempfile.TemporaryDirectory(prefix="infinity-baseline-")
        cls.base = Path(cls.storage.name)
        cls.fixture = cls.base / "project"
        for name in ("Assets", "Packages", "ProjectSettings"):
            shutil.copytree(ROOT / name, cls.fixture / name)

    @classmethod
    def tearDownClass(cls):
        cls.storage.cleanup()

    def verify(self):
        result = subprocess.run(
            [sys.executable, str(VERIFIER), "--project-root", str(self.fixture), "--skip-git"],
            text=True, capture_output=True, check=False,
        )
        return result.returncode, json.loads(result.stdout)

    def mutate(self, relative, transform, message):
        path = self.fixture / relative
        original = path.read_bytes()
        try:
            path.write_bytes(transform(original))
            code, result = self.verify()
            self.assertEqual(1, code)
            self.assertFalse(result["ok"])
            self.assertTrue(any(message in error for error in result["errors"]), result)
        finally:
            path.write_bytes(original)

    def test_valid_project_passes_without_mutating_source(self):
        before = {str(p): hashlib.sha256(p.read_bytes()).hexdigest()
                  for p in self.fixture.rglob("*") if p.is_file()}
        code, result = self.verify()
        after = {str(p): hashlib.sha256(p.read_bytes()).hexdigest()
                 for p in self.fixture.rglob("*") if p.is_file()}
        self.assertEqual((0, []), (code, result["errors"]))
        self.assertEqual(before, after)

    def test_manifest_version_drift_fails(self):
        self.mutate("Packages/manifest.json",
                    lambda b: b.replace(b'"2.11.2"', b'"2.11.3"'),
                    "Manifest requires com.unity.addressables")

    def test_lock_version_drift_fails(self):
        self.mutate("Packages/packages-lock.json",
                    lambda b: b.replace(b'"2.11.2"', b'"2.11.3"'),
                    "Lock must resolve direct dependency com.unity.addressables")

    def test_dangling_transitive_dependency_fails(self):
        def change(raw):
            data = json.loads(raw)
            data["dependencies"]["com.unity.addressables"]["dependencies"]["missing.package"] = "1.0.0"
            return json.dumps(data).encode()
        self.mutate("Packages/packages-lock.json", change, "Unresolved lock dependency")

    def test_invalid_json_fails_with_diagnostic(self):
        self.mutate("Packages/manifest.json", lambda _: b"{", "manifest.json:")

    def test_registry_override_fails(self):
        def change(raw):
            data = json.loads(raw)
            data["scopedRegistries"] = [{"name": "other", "url": "https://unapproved.invalid",
                                         "scopes": ["com.unity"]}]
            return json.dumps(data).encode()
        self.mutate("Packages/manifest.json", change, "Unapproved manifest options")

    def test_transitive_version_drift_fails(self):
        def change(raw):
            data = json.loads(raw)
            data["dependencies"]["com.unity.splines"]["version"] = "0.0.1"
            return json.dumps(data).encode()
        self.mutate("Packages/packages-lock.json", change, "Resolved lock graph")

    def test_lock_registry_source_drift_fails(self):
        def change(raw):
            data = json.loads(raw)
            data["dependencies"]["com.unity.splines"]["url"] = "https://unapproved.invalid"
            return json.dumps(data).encode()
        self.mutate("Packages/packages-lock.json", change, "Resolved lock graph")

    def test_orphan_lock_package_fails(self):
        def change(raw):
            data = json.loads(raw)
            data["dependencies"]["unapproved.package"] = {
                "version": "1.0.0", "depth": 0, "source": "registry", "dependencies": {}}
            return json.dumps(data).encode()
        self.mutate("Packages/packages-lock.json", change, "Resolved lock graph")

    def test_missing_meta_fails(self):
        path = self.fixture / "Assets/Game.meta"
        parked = self.base / "parked.meta"
        path.rename(parked)
        try:
            code, result = self.verify()
            self.assertEqual(1, code)
            self.assertIn("Missing Unity metadata: Assets/Game.", result["errors"])
        finally:
            parked.rename(path)

    def test_duplicate_guid_fails(self):
        self.mutate("Assets/Game/Runtime.meta",
                    lambda _: (self.fixture / "Assets/Game.meta").read_bytes(),
                    "Duplicate Unity GUID")

    def test_editor_pin_drift_fails(self):
        self.mutate("ProjectSettings/ProjectVersion.txt",
                    lambda b: b.replace(b"6000.6.0f1", b"6000.6.1f1"),
                    "ProjectVersion.txt: required setting")


if __name__ == "__main__":
    unittest.main()
