---
name: air-readiness-scanner
description: Scan a Relativity Server custom application (a local source folder, a .rap file, or a GitHub repository link) for code that will not work the same way in Relativity aiR, and produce a report that points at each file and line, explains what changes in aiR, and proposes the supported replacement with links to Relativity's documentation. Use when someone is moving a custom app, agent, event handler, custom page, Kepler service, Relativity Script, or integration from Relativity Server to Relativity aiR (RelativityOne), asks whether their app is "aiR ready" or "cloud ready", asks what will break after migrating, or wants a compatibility or readiness review of Relativity code.
argument-hint: "[folder path, .rap file, or GitHub repository URL]"
---

# Relativity aiR readiness scanner

You are helping a developer find what in their Relativity Server custom application will break or
behave differently in Relativity aiR, and what to use instead.

The work has two halves. A script (`scripts/scan.py`) does the pattern matching against a catalog of
known Server to aiR differences. You then read the code around every match, throw out the false
positives, and write the report. The script is fast and literal; your judgement is what makes the
report worth reading.

## Ground rules

- **Read-only.** Never build, run, restore, install, or execute anything from the scanned code.
  Never modify the scanned files. The script only reads files and, for a GitHub link, clones into a
  temporary folder that it deletes afterward.
- **Keep the code local.** Don't send the customer's code to any web service, paste it into a web
  search, or upload it anywhere. Quoting a line or two of it in the report is fine.
- **Stay inside the catalog and public docs.** What you say about aiR comes from the catalog entry
  (`customer_wording`, `replacement`, `docs`) and from pages on `platform.relativity.com` or
  `help.relativity.com` that you actually opened. Don't guess at dates, roadmaps, or future
  removals, and don't promise anything on Relativity's behalf. If you're not sure something applies,
  say so and point to the doc.
- **The report is a starting point, not a sign-off.** Say so in the report. The scan can miss things
  (code built at run time, reflection, configuration stored in Relativity rather than in the repo)
  and the suggestions have not been tested against a live aiR instance.
- **Writing style.** Plain, direct sentences. Say "Relativity aiR" or "aiR", not "RelativityOne" or
  "R1". No em dashes.

## Step 1: Get the target

Ask for whichever of these the user has, if they haven't said:

| Input | Example | Notes |
|---|---|---|
| Local folder | `C:\src\ContosoApp` | Source code. Best results. |
| RAP or zip file | `C:\drops\ContosoApp.rap` | The script opens it and scans scripts, XML, custom page files, and the compiled assemblies inside. Compiled hits have no line numbers. |
| GitHub link | `https://github.com/contoso/cleanup-app` or `.../tree/main/src` | Needs `git`. A private repo needs the user signed in to GitHub (`gh auth login` or Git Credential Manager). If cloning fails, ask them to clone it and give you the folder. |

Having both the source and the RAP is the best case: scan both, since the RAP shows what actually
ships (bundled DLLs, Relativity Scripts, custom page files) and the source gives line numbers.

Also ask where to save the report. Default to `air-readiness-report.md` in the current working
directory.

## Step 2: Run the scanner

Requires Python 3.8 or later, standard library only. Find the interpreter with `python --version`
or `python3 --version`. Write the raw results to a scratch or temp folder, not into the scanned
repo:

```bash
python "<this skill folder>/scripts/scan.py" "<target>" --out "<temp>/air-scan.json" --md "<temp>/air-scan.md"
```

The script prints a one-line summary. `air-scan.md` is a raw, unverified listing grouped by catalog
entry. `air-scan.json` has every hit with these fields:

| Field | Meaning |
|---|---|
| `entry_id`, `title`, `status_label` | Which catalog entry matched |
| `severity` | `high` (removed or replaced in aiR), `medium` (restricted or works differently), `low` (same, but in test, sample, vendored, or generated code), `review` (needs a human look; may be fine) |
| `file`, `line`, `permalink` | Where. `file` uses `!/` for paths inside an archive (`App.rap!/bin/Foo.dll`). `permalink` is a GitHub link to the exact commit, set when the target is a GitHub link, or a local clone whose current commit is already pushed. Link locations with it when present; otherwise use plain `file:line` |
| `matched`, `snippet` | The matched text and the whole line |
| `source` | `text` (source file), `path` (the file's name or location matched), `compiled` (string inside a DLL) |
| `context` | `production`, `test`, `sample`, `vendored`, or `generated` |
| `notes` | Extra sentences from the catalog, plus "in a comment" and "compiled" markers |
| `also_in` | Other paths holding a byte-identical copy of the same file |

The JSON also carries `catalog_entries` (wording, replacement, and doc links for every entry that
matched) and `plan_for` (catalog items with nothing to scan for).

If the scan returns nothing, check `stats` before you call the app clean: zero `text_files` usually
means the wrong folder, or a folder with only build output in it.

## Step 3: Triage every hit

Open the file at each hit and read enough around it (usually 10 to 20 lines, sometimes the whole
method) to decide. Classify each one:

- **Confirmed**: the code really does the thing the entry describes.
- **Needs review**: plausible, but it depends on something you can't see (how the app is deployed,
  what a config value is set to, where a DLL comes from). Say what the user should check.
- **Dismissed**: a false positive. Drop it from the report, but count dismissals in the summary so
  the user knows they were looked at.

Rules of thumb:

- **One finding per place, not per match.** Several patterns often hit the same line or method.
  Report the place once, under the most specific entry, and count the rest as merged (not
  dismissed). Some entries overlap on purpose: API-019 shares `kCura.Relativity.Client` with
  API-001 (RSAPI), and API-019 also overlaps API-002 and API-003. Report the overlap under the more
  specific entry. When a file appears in `also_in`, report the first copy and say how many other
  copies exist.
- **Promoting a review hit.** A `review` hit becomes a finding when the code around it shows the
  problem the entry describes (for example an agent whose `Execute` calls files or services with no
  retry). Otherwise it stays under "worth a look".
- **Large volumes.** If one entry hits dozens of places, give the count, list the files, and show
  two or three representative locations instead of every line.
- **`review` hits** are deliberately loose (for example `IWorkspaceManager`, `IAgentManager`, an
  `.aspx` page, a `TargetFrameworkVersion` of 4.6). Confirm them only when the surrounding code
  shows the actual problem the entry describes; otherwise list them under "worth a look" with one
  line each.
- **Commented-out code** (marked in `notes`): mention it only if it looks like it might be
  re-enabled; otherwise dismiss.
- **Test, sample, vendored, generated code**: keep confirmed hits but put them in their own short
  section. Test projects matter for TEST-001 only.
- **Compiled hits** (`source: compiled`): the DLL references the type or namespace. If the user gave
  you source too, find the source line and report that instead. If not, report the DLL and ask for
  the project that builds it. DLLs from Relativity or well-known libraries are matched by file name
  only, never read.
- **File access (FILE-001)**: local temp files and paths built from `Path.GetTempPath()` are fine.
  UNC paths, fileshare roots, and `File.Location` values from Relativity are the problem.
- **Hosted libraries (PAGE-002)**: a script tag pointing at Relativity's own copy (`/Relativity/...`,
  or `../../Scripts/...` climbing out of the page folder) is the problem. A copy the app bundles in
  its own folder is fine.
- **Agents (AGENT-001)**: every agent is a review item. Read the `Execute` method and look for the
  patterns the entry's docs warn about: long single runs, no retry on HTTP or file calls, work that
  assumes only one instance runs at a time.

## Step 4: Get the replacement right

For each confirmed finding:

1. Start from the entry's `replacement` and `customer_wording`.
2. Open the entry's `docs` links (they're public). Prefer the `Relativity aiR` edition links for
   "what to do now". Use the Server edition links only for deprecation and migration background.
3. When the replacement names a general area ("Object Manager API and the other Kepler services"),
   get specific to what this code does. Find the right page through Relativity's documentation
   index and cite that page:
   ```bash
   curl -sSL -o "<temp>/platform-llms.txt" https://platform.relativity.com/llms.txt
   grep -i "object manager" "<temp>/platform-llms.txt" | head
   ```
   Use pages under `platform.relativity.com/RelativityaiR/` or `help.relativity.com/RelativityaiR/`.
4. Only include a link you opened in this session and that loaded. If a catalog link is broken,
   say so in the report instead of dropping the finding.
   The Developer Changelog table on the aiR `Release_notes.htm` page is filled in by the browser, so
   fetching the page returns an empty table. To check a dated changelog claim, read the same data
   from the public feed instead (rows are under `developer-release-notes`), and keep citing
   `Release_notes.htm` as the link:
   ```bash
   curl -sSL -o "<temp>/release-notes.json" https://help.relativity.com/RelativityaiRReleaseNotes.json
   ```
5. A short code sketch of the replacement is welcome when it's straightforward (swapping a package,
   reading a value from `window.relativity`, adding a header). Label it as a sketch to test, not a
   tested fix.

Hand off the deep conversions to the sibling skills in this plugin instead of writing them inline.
Name the skill in the report so the user can run it next:

| Finding | Hand off to |
|---|---|
| SQL against Relativity databases (DATA-*, SCR-001, AUTH-002 SQL, ADMIN-001 SQL) | `sql-to-api-converter` |
| Agents that need restructuring or Workload Discovery (AGENT-001) | `agent-builder` |
| Classic event handlers and Forms scripts (EH-001) | `event-handler-builder` |
| Custom pages that should become HTML/JS pages (PAGE-001, PAGE-002) | `javascript-custom-page-builder` |
| A custom REST endpoint to replace direct database or file access | `kepler-service-builder` |

## Step 5: Write the report

Write it as Markdown to the path from Step 1. Use this shape:

```markdown
# Relativity aiR readiness report: <app or repo name>

Scanned <target> (<commit if known>) on <date> against the Server to aiR catalog dated <catalog_generated>.

> This report comes from an automated scan plus a code read. It can miss things and its suggestions
> haven't been run against Relativity aiR. Treat it as a starting checklist and test every change.

## Summary
<2 to 4 sentences: overall readiness, the biggest pieces of work, anything that blocks the move.>

| # | Severity | Area | What we found | Where |
|---|---|---|---|---|
| 1 | High | <entry title> | <one line> | <file:line, linked> |

<n> pattern matches were reviewed: <f> became the findings below, <r> are listed under "worth a
look", <g> were merged into a finding for the same place, and <m> were dismissed as false positives.

## Findings

### 1. <entry title> (<entry id>)
**Where:** <file:line links, or a count plus examples>
**What we found:** <what this code does, in a sentence or two, quoting the line if it helps>
**What changes in aiR:** <the entry's customer_wording, tightened to this code>
**Use instead:** <the specific replacement for this code>
**Documentation:** <links you opened>
**Next step:** <a concrete action, or "Run the sql-to-api-converter skill on <file>">

## Worth a look
<review-level items, one line each with location and what to check>

## Test, sample, and generated code
<only if there are confirmed hits there>

## Things to plan for
<the plan_for items, one short paragraph each, phrased as planning steps>

## What this scan doesn't cover
- Settings, scripts, and objects that live in your Relativity instance rather than in this code.
- Code built at run time, reflection, and dynamically loaded assemblies.
- Behavior under aiR load, timeouts, and rate limits, which only testing shows.
- Changes Relativity hasn't documented publicly yet.
```

Order findings by severity, then by how much work they are. Keep each finding short. After writing
the file, give the user the summary table and the path, and offer to start one of the handoff skills
on the biggest finding.

## The catalog

`catalog/catalog.json` is the rule set, and `catalog/catalog.md` is the same list for reading. Both
are generated by the maintainers from Relativity's public documentation; don't edit them by hand.
Each entry has an id, title, status, the Server versions it applies to, the replacement, the
customer-facing explanation, documentation links, and detection rules. Detection rule fields are
described at the top of `scripts/scan.py`.

If the user reports a false positive or a miss, note it in the report and suggest they open an issue
on the `relativitydev/relativity-builder-skills` repository with the pattern (not their code).
