#!/usr/bin/env python3
"""Scan a Relativity Server custom application for things that work differently in Relativity aiR.

Read-only and offline (except cloning when you pass a GitHub URL). Uses only the Python standard
library. Never builds, runs, or installs anything from the scanned code.

Usage:
    python scan.py <folder | .rap/.zip file | GitHub URL> [--out results.json] [--md results.md]
                   [--catalog ../catalog/catalog.json] [--keep-clone]

Rules come from catalog/catalog.json next to this script. Each catalog entry has detection items:
    kind         namespace | type | method | package | sql | config | file-pattern
    pattern      regular expression
    level        finding (default) | review | ignore (signal only, never reported)
    with_file    the same file must also match this, otherwise else_level applies
    with_line    the same line must also match this, otherwise else_level applies
    else_level   finding | review | drop (default drop)
    unless_repo  drop the hit if anything scanned matches this
    if_repo_lacks / hint_if_lacking   add the hint when nothing scanned matches
    hint         extra sentence for the finding
"""
import argparse
import bisect
import datetime as dt
import hashlib
import io
import json
import os
import re
import shutil
import subprocess
import sys
import tempfile
import zipfile
from pathlib import Path

HERE = Path(__file__).resolve().parent
DEFAULT_CATALOG = HERE.parent / "catalog" / "catalog.json"

SKIP_DIRS = {".git", ".hg", ".svn", ".vs", ".idea", "bin", "obj", "node_modules", "packages",
             "TestResults", "__pycache__", ".nuget"}
TEXT_EXT = {
    ".cs", ".vb", ".fs", ".js", ".mjs", ".cjs", ".ts", ".tsx", ".jsx", ".html", ".htm", ".aspx",
    ".ascx", ".asmx", ".ashx", ".asax", ".master", ".cshtml", ".vbhtml", ".razor", ".config",
    ".json", ".xml", ".csproj", ".vbproj", ".fsproj", ".props", ".targets", ".nuspec", ".sln",
    ".sql", ".ps1", ".psm1", ".psd1", ".py", ".sh", ".bat", ".cmd", ".yml", ".yaml", ".ini",
    ".settings", ".resx", ".xaml", ".css", ".java", ".go", ".rb", ".php",
}
DOC_EXT = {".map", ".md", ".txt", ".rst", ".adoc", ".pdf", ".docx", ".png", ".jpg", ".gif", ".svg", ".ico"}
BINARY_EXT = {".dll", ".exe"}
ARCHIVE_EXT = {".rap", ".zip", ".nupkg"}
MAX_TEXT = 5 * 1024 * 1024
MAX_BINARY = 50 * 1024 * 1024
MAX_PER_FILE = 20  # findings per detection item per file
# Assemblies from Relativity, Microsoft, and common libraries: match on file name only,
# don't read their strings (they mention Relativity types by design).
VENDOR_DLL = re.compile(
    r"(?i)^(system|microsoft|newtonsoft|polly|castle|grpc|google|autofac|"
    r"automapper|dapper|structuremap|nlog|log4net|serilog|opentelemetry|azure|aws|bouncycastle|"
    r"nunit|xunit|moq|fluentassertions|entityframework|owin|antlr|webgrease|oracle|npgsql|"
    r"mono|netstandard|mscorlib|ionic|icsharpcode|sharpziplib|htmlagilitypack|restsharp|"
    r"oi\.|outsidein|aspera|faspmanager|ninject|unity|simpleinjector|humanizer|csvhelper)")
SQL_WORDS = re.compile(r"\b(SELECT|FROM|INSERT|UPDATE|DELETE|JOIN|EXEC|EXECUTE|WHERE|CREATE|ALTER|"
                       r"DROP|TRUNCATE|MERGE|BEGIN|DECLARE)\b")
COMMENT_LINE = re.compile(r"^\s*(//|/\*|\*|--|#(?!(region|endregion|if|else|endif|define))|'|<!--|@\*|REM\s)")
PACKAGE_RX = [
    re.compile(r"<PackageReference\s+[^>]*?(?:Include|Update)\s*=\s*\"([^\"]+)\"", re.I),
    re.compile(r"<PackageVersion\s+[^>]*?Include\s*=\s*\"([^\"]+)\"", re.I),
    re.compile(r"<package\s+[^>]*?id\s*=\s*\"([^\"]+)\"", re.I),
    re.compile(r"<dependency\s+[^>]*?id\s*=\s*\"([^\"]+)\"", re.I),
    re.compile(r"^\s*nuget\s+([\w.\-]+)", re.I | re.M),
    re.compile(r"<Reference\s+[^>]*?Include\s*=\s*\"([\w.\-]+)", re.I),
]
CONTEXT_RX = [
    ("test", re.compile(r"(?i)(^|/)(tests?|unittests?|integrationtests?|specs?)(/|$)|[.\-_]tests?(/|\.csproj$|\.vbproj$)|tests?\.(cs|vb|js|ts)$|\.spec\.(js|ts)$")),
    ("sample", re.compile(r"(?i)(^|/)(samples?|examples?|demos?)(/|$)")),
    ("vendored", re.compile(r"(?i)(^|/)(vendor|third[-_]?party|external|lib|libs|bower_components)(/|$)|\.min\.(js|css)$")),
    ("generated", re.compile(r"(?i)\.(designer|g|g\.i)\.(cs|vb)$|(^|/)(service references|connected services)/|reference\.cs$")),
]
SEVERITY_ORDER = ["high", "medium", "low", "review"]
# Well-known third-party browser libraries. Their own source is never the app's code.
VENDOR_JS = re.compile(
    r"(?i)(^|/)(jquery|jquery-ui|bootstrap|angular|angular-[a-z]+|knockout|modernizr|popper|lodash|"
    r"underscore|moment|react|react-dom|vue|d3|chart|toastr|mousetrap|respond|signalr|kendo|"
    r"select2|datatables|q|require|backbone|handlebars|mustache)([.\-][\w.\-]*)?\.js$")


# ----------------------------------------------------------------------------------- matching

def is_plain(p):
    return re.fullmatch(r"[A-Za-z_][\w.]*", p) is not None


def wrap(p):
    """Add identifier boundaries to patterns that start or end with a word character."""
    pre = r"(?<![\w])" if re.match(r"[A-Za-z_]", p) else ""
    end_word = re.search(r"[A-Za-z0-9_]$", p) and not re.search(r"\\[A-Za-z]$", p)
    post = r"(?![\w])" if end_word else ""
    return pre + "(?:" + p + ")" + post


class Rule:
    def __init__(self, entry, idx, d):
        self.entry = entry
        self.idx = idx
        self.kind = d["kind"]
        self.raw = d["pattern"]
        self.level = d.get("level", "finding")
        self.else_level = d.get("else_level", "drop")
        self.with_file = re.compile(d["with_file"]) if d.get("with_file") else None
        self.with_line = re.compile(d["with_line"]) if d.get("with_line") else None
        self.unless_repo = d.get("unless_repo")
        self.if_repo_lacks = d.get("if_repo_lacks")
        self.hint_if_lacking = d.get("hint_if_lacking")
        self.hint = d.get("hint")
        flags = re.M
        if self.kind in ("type", "namespace", "method"):
            self.rx = re.compile(wrap(self.raw), flags)
        else:
            self.rx = re.compile(self.raw, flags)
        # Dotted plain type names also match "using Namespace;" + short name.
        self.short = None
        if self.kind == "type" and is_plain(self.raw) and "." in self.raw:
            ns, short = self.raw.rsplit(".", 1)
            # Short names are common words (File, Directory), so only count code-shaped use:
            # member access, a call, generics, a base class, or a declaration.
            self.short = re.compile(
                r"(?<![\[\w.])" + re.escape(short)
                + r"(?=\s*([.(<{,]|$)|\s+[A-Za-z_]\w*\s*[=;,)])", re.M)
            self.ns_import = re.compile(
                r"^\s*(using|Imports|import)\s+(static\s+)?" + re.escape(ns) + r"\s*;?\s*$", re.M)


def line_index(text):
    starts = [0]
    for m in re.finditer(r"\n", text):
        starts.append(m.end())
    return starts


def classify(path):
    for name, rx in CONTEXT_RX:
        if rx.search(path):
            return name
    return "production"


# ------------------------------------------------------------------------------------ scanner

class Scanner:
    def __init__(self, catalog):
        self.catalog = catalog
        self.rules = []
        for e in catalog["entries"]:
            for i, d in enumerate(e["detection"]):
                self.rules.append(Rule(e, i, d))
        self.repo_rx = {}
        for r in self.rules:
            for p in (r.unless_repo, r.if_repo_lacks):
                if p and p not in self.repo_rx:
                    self.repo_rx[p] = re.compile(p, re.M)
        self.repo_seen = set()
        self.hits = []
        self.stats = {"text_files": 0, "duplicates": 0, "vendor_libraries": 0, "archives": 0,
                      "assemblies_read": 0, "assemblies_name_only": 0, "skipped": 0}
        self.seen_content = {}
        self.duplicates = {}
        self.skipped = []

    # ---- input walking

    def scan_folder(self, root):
        root = Path(root)
        for dirpath, dirnames, filenames in os.walk(root):
            dirnames[:] = [d for d in dirnames if d not in SKIP_DIRS]
            for fn in filenames:
                full = Path(dirpath) / fn
                rel = full.relative_to(root).as_posix()
                try:
                    size = full.stat().st_size
                    ext = full.suffix.lower()
                    if ext in ARCHIVE_EXT:
                        if size <= 500 * 1024 * 1024:
                            with open(full, "rb") as fh:
                                self.scan_archive(fh.read(), rel, 0)
                        else:
                            self.skip(rel, "archive over 500 MB")
                        continue
                    if ext in BINARY_EXT:
                        if size <= MAX_BINARY:
                            self.scan_binary(full.read_bytes(), rel)
                        continue
                    self.scan_path_rules(rel)
                    if ext in DOC_EXT or size > MAX_TEXT:
                        continue
                    data = full.read_bytes()
                    self.scan_bytes(data, rel, ext)
                except OSError as ex:
                    self.skip(rel, f"unreadable ({ex.__class__.__name__})")

    def scan_archive(self, data, label, depth):
        self.stats["archives"] += 1
        if depth > 3:
            self.skip(label, "nested archive too deep")
            return
        try:
            zf = zipfile.ZipFile(io.BytesIO(data))
        except zipfile.BadZipFile:
            self.skip(label, "not a readable zip/RAP")
            return
        self.scan_path_rules(label)
        for info in zf.infolist():
            if info.is_dir():
                continue
            inner = f"{label}!/{info.filename.replace(os.sep, '/')}"
            ext = Path(info.filename).suffix.lower()
            if info.file_size > MAX_BINARY:
                self.skip(inner, "too large")
                continue
            try:
                blob = zf.read(info)
            except Exception as ex:  # corrupt member, encrypted, etc.
                self.skip(inner, f"unreadable ({ex.__class__.__name__})")
                continue
            if ext in ARCHIVE_EXT or (blob[:4] == b"PK\x03\x04" and ext not in BINARY_EXT
                                      and ext not in TEXT_EXT):
                self.scan_archive(blob, inner, depth + 1)
            elif ext in BINARY_EXT:
                self.scan_binary(blob, inner)
            else:
                self.scan_path_rules(inner)
                if ext not in DOC_EXT and len(blob) <= MAX_TEXT:
                    self.scan_bytes(blob, inner, ext)

    def skip(self, path, why):
        self.stats["skipped"] += 1
        self.skipped.append({"file": path, "reason": why})

    # ---- per file

    def scan_bytes(self, data, rel, ext):
        if ext not in TEXT_EXT:
            return
        text = decode(data)
        if text is None:
            return
        low = rel.lower()
        if low.endswith((".min.js", ".min.css")) or VENDOR_JS.search(low):
            self.stats["vendor_libraries"] += 1
            return  # third-party libraries: their source isn't the app's code
        digest = hashlib.sha1(data).hexdigest()
        first = self.seen_content.get(digest)
        if first is not None:
            self.duplicates.setdefault(first, []).append(rel)
            self.stats["duplicates"] += 1
            return
        self.seen_content[digest] = rel
        self.stats["text_files"] += 1
        self.scan_text(text, rel, ext, source="text")

    def scan_binary(self, data, rel):
        name = rel.rsplit("/", 1)[-1]
        self.scan_path_rules(rel)
        if VENDOR_DLL.match(name):
            self.stats["assemblies_name_only"] += 1
            return
        ascii_strings = re.findall(rb"[\x20-\x7e]{4,}", data)
        utf16_strings = re.findall(rb"(?:[\x20-\x7e]\x00){4,}", data)
        lines = [s.decode("ascii") for s in ascii_strings]
        lines += [s.decode("utf-16-le", "replace") for s in utf16_strings]
        if is_relativity_library(name, lines):
            self.stats["assemblies_name_only"] += 1
            return
        self.stats["assemblies_read"] += 1
        self.scan_text("\n".join(lines), rel, ".dll", source="compiled")

    def scan_path_rules(self, rel):
        for r in self.rules:
            if r.kind == "file-pattern" and r.rx.search(rel):
                self.add(r, rel, None, rel.rsplit("/", 1)[-1], rel, "", source="path")

    def scan_repo_signals(self, text):
        for p, rx in self.repo_rx.items():
            if p not in self.repo_seen and rx.search(text):
                self.repo_seen.add(p)

    def scan_text(self, text, rel, ext, source):
        self.scan_repo_signals(text)
        starts = line_index(text)
        lines = text.split("\n")
        is_sql_file = ext == ".sql"
        packages = []
        if ext in (".csproj", ".vbproj", ".fsproj", ".props", ".targets", ".config", ".nuspec") \
                or rel.lower().endswith(("packages.config", "paket.dependencies")):
            for prx in PACKAGE_RX:
                for m in prx.finditer(text):
                    packages.append((m.group(1), m.start()))
        for r in self.rules:
            if r.kind == "package":
                for pkg, pos in packages:
                    if r.rx.search(pkg):
                        ln = bisect.bisect_right(starts, pos)
                        self.add(r, rel, ln if source != "compiled" else None, pkg,
                                 lines[ln - 1], text, source=source)
                continue
            if r.kind == "file-pattern" and r.raw.endswith("$"):
                continue  # anchored at the end: a file name rule, handled by scan_path_rules
            count = 0
            seen_lines = set()
            for m in r.rx.finditer(text):
                ln = bisect.bisect_right(starts, m.start())
                if ln in seen_lines:
                    continue
                line = lines[ln - 1]
                if r.kind in ("type", "method", "namespace") and \
                        text[max(0, m.start() - 2):m.start()].endswith(("<", "</")):
                    continue  # an XML element name (RAP application.xml metadata), not code
                if r.kind == "sql" and not is_sql_file:
                    window = "\n".join(lines[max(0, ln - 4):ln + 3])
                    if not SQL_WORDS.search(window):
                        continue
                seen_lines.add(ln)
                self.add(r, rel, ln if source != "compiled" else None, m.group(0), line, text,
                         source=source)
                count += 1
                if count >= MAX_PER_FILE:
                    break
            if r.short is not None and r.ns_import.search(text):
                for m in r.short.finditer(text):
                    ln = bisect.bisect_right(starts, m.start())
                    if ln in seen_lines:
                        continue
                    line = lines[ln - 1]
                    if r.ns_import.match(line):
                        continue
                    seen_lines.add(ln)
                    self.add(r, rel, ln if source != "compiled" else None, m.group(0), line, text,
                             source=source)
                    count += 1
                    if count >= MAX_PER_FILE:
                        break

    def add(self, rule, rel, line_no, matched, line, text, source):
        level = rule.level
        if level == "ignore":
            return
        if rule.with_file is not None and not rule.with_file.search(text or ""):
            level = rule.else_level
        if level != "drop" and rule.with_line is not None and not rule.with_line.search(line or ""):
            level = rule.else_level
        if level == "drop":
            return
        in_comment = bool(line and COMMENT_LINE.match(line))
        self.hits.append({
            "rule": rule, "file": rel, "line": line_no, "matched": matched[:200],
            "snippet": (line or "").strip()[:240], "level": level, "source": source,
            "in_comment": in_comment,
        })

    # ---- results

    def results(self):
        out = []
        dedupe = set()
        for h in self.hits:
            r = h["rule"]
            if r.unless_repo and r.unless_repo in self.repo_seen:
                continue
            e = r.entry
            key = (e["id"], h["file"], h["line"], h["matched"] if h["line"] is None else "")
            if key in dedupe:
                continue
            dedupe.add(key)
            context = classify(h["file"])
            level = h["level"]
            notes = []
            if h["in_comment"]:
                level = "review"
                notes.append("Appears in a comment or commented-out code.")
            if h["source"] == "compiled":
                notes.append("Found inside a compiled assembly, so there is no line number. "
                             "Check the source project that builds it.")
            if r.hint:
                notes.append(r.hint)
            if r.if_repo_lacks and r.if_repo_lacks not in self.repo_seen:
                notes.append(r.hint_if_lacking)
            severity = severity_for(e["status"], level, context, e["id"])
            out.append({
                "entry_id": e["id"], "title": e["title"], "status": e["status"],
                "status_label": e["status_label"], "severity": severity, "level": level,
                "file": h["file"], "line": h["line"], "matched": h["matched"],
                "snippet": h["snippet"], "kind": r.kind, "pattern": r.raw, "source": h["source"],
                "context": context, "notes": notes,
                "also_in": self.duplicates.get(h["file"], []),
            })
        out.sort(key=lambda f: (SEVERITY_ORDER.index(f["severity"]), f["entry_id"], f["file"],
                                f["line"] or 0))
        return out


RELATIVITY_DLL_NAME = re.compile(r"(?i)^(kcura|relativity)\.")
RELATIVITY_PUBLISHER = re.compile(r"(?i)\b(relativity ODA LLC|kcura (LLC|corporation))\b")


def is_relativity_library(name, strings):
    """True for assemblies Relativity publishes (SDK, platform, Relativity apps).

    Customers often name their own DLLs Relativity.* or kCura.*, and apps generated from old
    Relativity templates keep a kCura copyright, so neither the name nor the publisher alone is
    enough. A Relativity library has an SDK-style dotted name AND Relativity's company or
    copyright attribute. Everything else is read as app code.
    """
    if not RELATIVITY_DLL_NAME.match(name):
        return False
    return any(RELATIVITY_PUBLISHER.search(s) for s in strings)


def _force_remove(func, path, _exc):
    """Git marks pack files read-only, which stops rmtree on Windows."""
    try:
        os.chmod(path, 0o700)
        func(path)
    except OSError:
        pass


def severity_for(status, level, context, entry_id):
    if level == "review":
        return "review"
    sev = "high" if status in ("removed", "replaced") else "medium"
    if context != "production" and not (entry_id == "TEST-001" and context == "test"):
        sev = {"high": "medium", "medium": "low"}.get(sev, sev)
    return sev


def decode(data):
    if data.startswith(b"\xef\xbb\xbf"):
        return data[3:].decode("utf-8", "replace")
    if data.startswith((b"\xff\xfe", b"\xfe\xff")):
        try:
            return data.decode("utf-16")
        except UnicodeDecodeError:
            return None
    try:
        return data.decode("utf-8")
    except UnicodeDecodeError:
        return data.decode("latin-1")


# ----------------------------------------------------------------------------------- git input

GH_URL = re.compile(r"^(?:https?://)?(?:www\.)?github\.com/([^/\s]+)/([^/\s#?]+?)(?:\.git)?"
                    r"(?:/tree/([^/\s]+)(?:/(.*))?)?/?$")


def clone(url, dest):
    m = GH_URL.match(url.strip())
    if m:
        owner, repo, ref, sub = m.groups()
        clone_url = f"https://github.com/{owner}/{repo}.git"
    else:
        owner = repo = ref = sub = None
        clone_url = url
    env = dict(os.environ, GIT_TERMINAL_PROMPT="0")
    cmd = ["git", "-c", "core.hooksPath=", "clone", "--depth", "1", "--no-tags", "--quiet"]
    if ref:
        cmd += ["--branch", ref]
    cmd += [clone_url, str(dest)]
    proc = subprocess.run(cmd, env=env, capture_output=True, text=True)
    if proc.returncode != 0:
        sys.exit("git clone failed. If the repository is private, sign in first "
                 "(for example `gh auth login`, or clone it yourself and pass the folder).\n"
                 + proc.stderr.strip())
    sha = subprocess.run(["git", "-C", str(dest), "rev-parse", "HEAD"], capture_output=True,
                         text=True).stdout.strip()
    web = f"https://github.com/{owner}/{repo}" if owner else None
    return sha, web, sub


def local_git_info(folder):
    try:
        url = subprocess.run(["git", "-C", str(folder), "remote", "get-url", "origin"],
                             capture_output=True, text=True, timeout=10).stdout.strip()
        sha = subprocess.run(["git", "-C", str(folder), "rev-parse", "HEAD"],
                             capture_output=True, text=True, timeout=10).stdout.strip()
        top = subprocess.run(["git", "-C", str(folder), "rev-parse", "--show-toplevel"],
                             capture_output=True, text=True, timeout=10).stdout.strip()
    except (OSError, subprocess.SubprocessError):
        return None, None, None
    m = GH_URL.match(url.replace("git@github.com:", "https://github.com/")) if url else None
    if not (m and sha and top):
        return None, None, None
    # Only link to commits that exist on the remote; a local-only commit gives dead links.
    try:
        pushed = subprocess.run(["git", "-C", str(folder), "branch", "-r", "--contains", sha],
                                capture_output=True, text=True, timeout=10).stdout.strip()
    except (OSError, subprocess.SubprocessError):
        pushed = ""
    if not pushed:
        return None, None, None
    try:
        prefix = Path(folder).resolve().relative_to(Path(top).resolve()).as_posix()
    except ValueError:
        prefix = ""
    return sha, f"https://github.com/{m.group(1)}/{m.group(2)}", "" if prefix == "." else prefix


def permalink(web, sha, prefix, f):
    if not (web and sha) or f["source"] == "compiled" or "!/" in f["file"]:
        return None
    path = f"{prefix}/{f['file']}" if prefix else f["file"]
    link = f"{web}/blob/{sha}/{path.replace(' ', '%20')}"
    return f"{link}#L{f['line']}" if f["line"] else link


# ------------------------------------------------------------------------------------ markdown

def render_md(res):
    out = [f"# aiR readiness scan: raw results", "",
           f"Target: `{res['target']}`" + (f" at commit `{res['commit'][:12]}`" if res.get("commit") else ""),
           f"Scanned {res['scanned_at']} with catalog {res['catalog_generated']}. "
           f"{res['stats']['text_files']} text files, {res['stats']['archives']} archives, "
           f"{res['stats']['assemblies_read']} assemblies read.", "",
           "These are pattern matches, not verified findings. Each one needs a look at the code "
           "around it before it goes in a report.", "",
           "| Entry | Title | Status | High | Medium | Low | Review |",
           "| --- | --- | --- | --- | --- | --- | --- |"]
    by_entry = {}
    for f in res["findings"]:
        by_entry.setdefault(f["entry_id"], []).append(f)
    for eid, fs in sorted(by_entry.items(), key=lambda kv: min(SEVERITY_ORDER.index(f["severity"]) for f in kv[1])):
        c = {s: sum(1 for f in fs if f["severity"] == s) for s in SEVERITY_ORDER}
        out.append(f"| {eid} | {fs[0]['title']} | {fs[0]['status_label']} | {c['high']} | "
                   f"{c['medium']} | {c['low']} | {c['review']} |")
    entries = {e["id"]: e for e in res["catalog_entries"]}
    for eid, fs in by_entry.items():
        e = entries[eid]
        out += ["", f"## {eid}: {e['title']}", "", e["customer_wording"], "",
                f"Use instead: {e['replacement']}", ""]
        for d in e["docs"]:
            out.append(f"- Docs: [{d['title']}]({d['url']}) ({d['edition']})")
        out.append("")
        for f in fs[:40]:
            loc = f"{f['file']}:{f['line']}" if f["line"] else f["file"]
            if f.get("permalink"):
                loc = f"[{loc}]({f['permalink']})"
            extra = f" [{f['context']}]" if f["context"] != "production" else ""
            out.append(f"- **{f['severity']}**{extra} {loc} matched `{f['matched']}`: `{f['snippet']}`")
            for n in f["notes"]:
                out.append(f"  - {n}")
            if f.get("also_in"):
                out.append(f"  - Same file also at {len(f['also_in'])} other path(s), "
                           f"for example {f['also_in'][0]}")
        if len(fs) > 40:
            out.append(f"- ...and {len(fs) - 40} more in the JSON results.")
    if res["plan_for"]:
        out += ["", "## Things to plan for (nothing to scan for)", ""]
        for e in res["plan_for"]:
            out.append(f"- **{e['id']}: {e['title']}.** {e['customer_wording']}")
    if res["skipped"]:
        out += ["", "## Skipped", ""] + [f"- {s['file']}: {s['reason']}" for s in res["skipped"][:50]]
    return "\n".join(out) + "\n"


# ---------------------------------------------------------------------------------------- main

def main():
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("target", help="folder, .rap/.zip file, or GitHub URL")
    ap.add_argument("--catalog", type=Path, default=DEFAULT_CATALOG)
    ap.add_argument("--out", type=Path, help="write JSON results here")
    ap.add_argument("--md", type=Path, help="write a markdown summary here")
    ap.add_argument("--keep-clone", action="store_true", help="don't delete the temporary clone")
    args = ap.parse_args()

    catalog = json.loads(args.catalog.read_text(encoding="utf-8"))
    scanner = Scanner(catalog)
    target = args.target
    sha = web = prefix = None
    tmp = None
    try:
        if re.match(r"^(https?://|git@)", target) or GH_URL.match(target):
            tmp = Path(tempfile.mkdtemp(prefix="air-scan-"))
            sha, web, sub = clone(target, tmp / "repo")
            root = tmp / "repo" / sub if sub else tmp / "repo"
            prefix = sub or ""
            if not root.exists():
                sys.exit(f"Path '{sub}' not found in the repository.")
            scanner.scan_folder(root)
        else:
            p = Path(target).expanduser()
            if p.is_file() and p.suffix.lower() in ARCHIVE_EXT:
                scanner.scan_archive(p.read_bytes(), p.name, 0)
            elif p.is_dir():
                sha, web, prefix = local_git_info(p)
                scanner.scan_folder(p)
            else:
                sys.exit(f"Not a folder, .rap/.zip file, or GitHub URL: {target}")
        findings = scanner.results()
        for f in findings:
            f["permalink"] = permalink(web, sha, prefix, f)
        res = {
            "target": target, "commit": sha, "repo_web": web,
            "scanned_at": dt.datetime.now().isoformat(timespec="seconds"),
            "catalog_generated": catalog.get("generated"),
            "stats": scanner.stats, "skipped": scanner.skipped,
            "findings": findings,
            "catalog_entries": [e for e in catalog["entries"]
                                if any(f["entry_id"] == e["id"] for f in findings)],
            "plan_for": catalog.get("plan_for", []),
        }
        for e in res["catalog_entries"]:
            e.pop("detection", None)
        if args.out:
            args.out.write_text(json.dumps(res, indent=2, ensure_ascii=False), encoding="utf-8")
        if args.md:
            args.md.write_text(render_md(res), encoding="utf-8")
        counts = {s: sum(1 for f in findings if f["severity"] == s) for s in SEVERITY_ORDER}
        entries_hit = sorted({f["entry_id"] for f in findings})
        print(f"{len(findings)} hits across {len(entries_hit)} catalog entries "
              f"(high {counts['high']}, medium {counts['medium']}, low {counts['low']}, "
              f"review {counts['review']}). Files: {scanner.stats}.")
        print("Entries hit:", ", ".join(entries_hit) or "none")
        if tmp and args.keep_clone:
            print("Clone kept at", tmp / "repo")
    finally:
        if tmp and not args.keep_clone:
            shutil.rmtree(tmp, onerror=_force_remove)
    return 0


if __name__ == "__main__":
    sys.exit(main())
