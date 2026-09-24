# javascript-custom-page-builder

Scaffolds and packages a JavaScript/HTML custom page for Relativity aiR — producing the zip
you upload through the **Custom Pages** tab — and covers the handful of things that reliably
trip people up the first time.

> **Relativity aiR only.** This skill targets the cloud platform, not Relativity Server
> (2024 / 2025 / 2026). Deployment steps and platform requirements differ between them.

## What it's for

Getting from an empty folder to a custom page running in a Relativity tab.

The skill handles the build side: it scaffolds `index.html` and `index.js`, then bundles them
into a correctly structured zip. **It does not deploy anything for you.** That zip is the file
you upload yourself in Relativity — navigate to the **Custom Pages** tab, click **New Custom
Page**, and select it in the **File** field. The skill walks you through those UI steps and the
application wiring that follows, but you perform them.

Most of the time lost on a first custom page goes to four things that aren't obvious from the
docs:

- Zipping the **folder** instead of its **contents**, so `index.html` isn't at the archive root
- The **Application** field resetting to **System** after you save the custom page record
- Browser caching in a sandbox, where a re-uploaded page keeps serving the old code
- Assuming REST calls run with elevated rights — they run as the **signed-in user**

Each is raised at the point it actually bites rather than buried in a troubleshooting section.

## When it triggers

- Building, creating, scaffolding, or deploying a Relativity aiR custom page
- A "hello world" custom page in Relativity aiR
- Packaging or uploading a custom page zip
- Custom page permission or sandbox deployment problems

## Scope

**In scope:** the client-side JavaScript/HTML workflow.

**Not in scope:** server-side ASP.NET Web Forms and MVC. Those are still fully supported on
the platform — MVC explicitly so — but they are a different build and packaging path. Rather
than walking through them, the skill states the Relativity aiR Compute requirements a
server-side page has to meet (stateless, no ASP.NET Session State, tolerant of cold cache
hits, one zip per application, no SignalR, no container admin permissions) and points at the
migration checklist and Relativity's Visual Studio tutorial.

## Using it

Installed via the plugin, Claude picks this up automatically when you describe custom page
work. To invoke it directly:

```
/relativity-builder-skills:javascript-custom-page-builder
```

See the [repository README](../../README.md) for plugin installation.

## Reference

Relativity's own end-to-end walkthrough, including the zip-and-upload steps this skill
automates the first half of:

- [Lesson 5 — Create a custom page](https://platform.relativity.com/RelativityOne/Content/Get_started_RelativityOne/Create_a_custom_page.htm)

[`SKILL.md`](SKILL.md) is the source of truth for the actual instructions — this file is
orientation for people browsing the repo. Every documentation link in the skill points at
official Relativity platform or help documentation.
