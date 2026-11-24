# air-readiness-scanner

Checks a Relativity Server custom application for code that won't work the same way in Relativity
aiR, and writes a report that points at each file and line, explains what changes, and suggests the
supported replacement with links to Relativity's documentation.

Point it at your app's source code:

- a local source folder, or
- a GitHub repository link (cloned to a temporary folder and deleted afterward).

It can also read a `.rap` (or `.zip`) application package if that's all you have, but expect
thinner results. Most of a RAP is compiled code, so the scan can tell you what a DLL references
but not the file, line, or how it's used. Use the source whenever you can.

Inside Claude Code, Claude picks the skill up when you ask whether an app is ready for Relativity
aiR. To invoke it directly:

```
/relativity-builder-skills:air-readiness-scanner C:\src\MyRelativityApp
```

See the [repository README](../../README.md) for plugin installation.

## How it works

1. `scripts/scan.py` matches the code against `catalog/catalog.json`, a list of documented
   differences between Relativity Server and Relativity aiR (removed APIs, replaced SDK packages,
   restricted database and fileshare access, custom page and agent changes, and so on).
2. Claude reads the code around every match, drops false positives, looks up the current
   documentation for each replacement, and writes the report.
3. Where a finding needs real conversion work, the report points at the matching skill in this
   plugin (for example `sql-to-api-converter` for direct SQL).

The scanner is read-only and offline. It never builds or runs the scanned code, never changes it,
and never sends it anywhere. It needs Python 3.8 or later and nothing else; `git` too if you pass a
GitHub link. You can run it on its own:

```
python scripts/scan.py <folder | .rap | GitHub URL> --md results.md --out results.json
```

The raw output from the script is a list of pattern matches, not verified findings.

**The report is a starting checklist, not a guarantee.** A scan can't see settings stored in your
Relativity instance, code built at run time, or behavior that only shows up under load, and the
suggested replacements haven't been tested against your environment. Test every change before
relying on it.

## What's in the catalog

`catalog/catalog.md` lists every entry in readable form. The catalog only includes changes that
Relativity has documented publicly, and every entry links to its source. As Relativity announces
more changes in the release notes and Developer Changelog, they're added here, so update the plugin
before a big migration review.

Found a false positive or a miss? Open an issue with the pattern or a minimal example (not your
code).
