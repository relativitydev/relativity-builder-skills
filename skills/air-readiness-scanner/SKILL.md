---
name: air-readiness-scanner
description: Scan a Relativity Server custom application (a local source folder, a .rap file, or a GitHub repository link) for code that will not work the same way in Relativity aiR, and produce a report that points at each file and line, explains what changes in aiR, and proposes the supported replacement with links to Relativity's documentation. Use when someone is moving a custom app, agent, event handler, custom page, Kepler service, Relativity Script, or integration from Relativity Server to Relativity aiR (RelativityOne), asks whether their app is "aiR ready" or "cloud ready", asks what will break after migrating, or wants a compatibility or readiness review of Relativity code.
argument-hint: "[source folder or GitHub repository URL; a .rap only if there's no source]"
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
  removals, and don't promise anything on Relativity's behalf. When a date in the catalog has
  already passed, say what was announced for that date ("was scheduled to be removed on ...")
  unless a page you opened says it actually happened. If you're not sure something applies,
  say so and point to the doc.
- **Get the user's OK before scanning.** Step 1 ends with a disclaimer you show word for word
  and an explicit yes you wait for. Nothing runs before that.
- **The report is a starting point, not a sign-off.** Say so in the report. The scan can miss things
  (code built at run time, reflection, configuration stored in Relativity rather than in the repo)
  and the suggestions have not been tested against a live aiR instance.
- **Writing style.** Plain, direct sentences. Say "Relativity aiR" or "aiR", not "RelativityOne" or
  "R1". No em dashes.

## Step 1: Get the target

Ask for the app's **source code**, as a local folder or a GitHub link:

| Input | Example | Notes |
|---|---|---|
| Local folder | `C:\src\ContosoApp` | Source code. Best results. |
| GitHub link | `https://github.com/contoso/cleanup-app` or `.../tree/main/src` | Source code. Needs `git`. A private repo needs the user signed in to GitHub (`gh auth login` or Git Credential Manager). If cloning fails, ask them to clone it and give you the folder. |
| RAP or zip file | `C:\drops\ContosoApp.rap` | Fallback only. See below. |

**Prefer source over a RAP.** Most of a RAP is compiled code: agents, event handlers, and Kepler
services arrive as DLLs. The scanner can still find type, method, and namespace names and SQL text
inside them, but it can't give line numbers or read the code around a hit, so it can't rule out
false positives or say how a call is used. Only custom page files and Relativity Scripts in a RAP
are readable the way source is.

- If the user offers only a RAP, ask once whether they have the source (a folder, a repo, or the
  Visual Studio solution) before you use the RAP. Explain in a sentence that source gives line-level
  results and a RAP mostly doesn't.
- If they have the source, scan the source. If they also have the RAP, you may scan it as well to
  see what actually ships (bundled DLLs, Relativity Scripts, custom page files), but report
  line-level findings from the source.
- If the RAP is all they have, go ahead with it, and say in the report's summary that the results
  are limited because most of the app was scanned as compiled code.

Also ask where to save the report. Default to `air-readiness-report.md` in the current working
directory.

Then, before you scan anything, put this to the user verbatim and wait for an answer:

> **Before we proceed, please review and confirm.**
>
> This review is produced by an AI assistant. It compares your code against a catalog of
> differences between Relativity Server and Relativity aiR that Relativity has documented
> publicly, then reads the matching code to judge what applies. It may therefore be incomplete
> or inaccurate: it can miss code that will behave differently in Relativity aiR, it can flag
> code that is actually fine, and the replacements it suggests can be stated confidently and
> still be wrong. It only sees the code you give it, not settings or objects in your Relativity
> instance, and nothing it suggests will have been run against a live Relativity aiR
> environment. It is not a certified, supported or warranted assessment of your application.
>
> What you receive is a set of **recommendations**: a starting checklist for your own
> development and testing, not a sign-off that your application is ready. Each change must be
> validated in a non-production environment before it is relied upon, shared onward, or
> promoted to production. Responsibility for validating, testing, securing and operating your
> application in Relativity aiR remains with you.
>
> Your code is read in place and is not changed, built, run, or sent anywhere. A GitHub link is
> cloned to a temporary folder on this machine and deleted afterward.
>
> Do you accept that and want me to proceed?

If they decline, or answer with a condition you cannot meet, stop and say what you cannot promise.
Do not soften the warning to get a yes, and do not skip it because the app looks small or because
they have used this skill before. A fresh run is a fresh review.

## Step 2: Run the scanner

Requires Python 3.8 or later, standard library only. Find the interpreter with `python --version`
or `python3 --version`. Write the raw results to a scratch or temp folder, not into the scanned
repo:

```bash
python "<this skill folder>/scripts/scan.py" "<target>" --out "<temp>/air-scan.json" --md "<temp>/air-scan.md"
```

The script prints a one-line summary. `air-scan.md` is a raw, unverified listing grouped by catalog
entry. `air-scan.json` lists every hit in its `findings` array (they're still unverified hits at
this point), each with these fields:

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

- **One finding per problem, not per match.** Several patterns often hit the same line or method,
  and one entry often hits several related lines (a package reference in the project file plus the
  code that uses it). Report that as one finding listing every location, under the most specific
  entry, and count the extra hits as merged (not dismissed). Some entries overlap on purpose: API-019 shares `kCura.Relativity.Client` with
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
  the project that builds it. Well-known third-party libraries, and assemblies Relativity publishes
  (a `Relativity.*` or `kCura.*` name plus Relativity's company or copyright stamp), are matched by
  file name only, never read. The app's own DLLs are read even when they're named `Relativity.*`.
- **File access (FILE-001)**: local temp files and paths built from `Path.GetTempPath()` are fine.
  UNC paths, fileshare roots, and `File.Location` values from Relativity are the problem.
- **Hosted libraries (PAGE-002)**: a script tag pointing at Relativity's own copy (`/Relativity/...`,
  or `../../Scripts/...` climbing out of the page folder) is the problem. A copy the app bundles in
  its own folder is fine. If a tag points inside the page folder but the file isn't in what you
  scanned, list it under "worth a look" and ask the user to confirm the file ships with the page.
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
   Some pages, including the aiR release notes page
   (`https://platform.relativity.com/RelativityaiR/Content/What_s_new/Release_notes.htm`), return
   404 to scripts that don't identify as a browser. Before you call any link broken, retry it with
   a browser user agent:
   ```bash
   curl -sSL -A "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0 Safari/537.36" -o "<temp>/page.htm" "<url>"
   ```
   Dated changes (deprecations, removals, breaking changes) come from the Developer Changelog tab
   of that release notes page. The tab's table is filled in by the browser, so a fetched copy shows
   it empty. To check a dated claim, read the same rows from the public feed (under
   `developer-release-notes`; the file starts with a byte order mark, so decode it as
   `utf-8-sig`), and keep citing the release notes page as the link:
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
| SQL against Relativity databases (DATA-*, SCR-001, AUTH-002 SQL) | `sql-to-api-converter` |
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
<2 to 4 sentences: overall readiness, the biggest pieces of work, anything that blocks the move.
If only a RAP was scanned, add: "This scan used the application package (RAP) only, so most of the
app was read as compiled code. Findings in DLLs show what the code references, not where or how
it's used. Scanning the source code would give file and line results.">


| # | Severity | Area | What we found | Where |
|---|---|---|---|---|
| 1 | High | <entry title> | <one line> | <file:line, linked> |

<n> pattern matches were reviewed: <f> became the findings below, <r> are listed under "worth a
look", <g> were merged into a finding for the same place, and <m> were dismissed as false positives.
<!-- Count hits, one per finding or worth-a-look item, so f + r + g + m = n. -->


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
