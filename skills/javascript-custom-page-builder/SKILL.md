---
name: javascript-custom-page-builder
description: Use when someone wants to create, scaffold, build, deploy, or troubleshoot a JavaScript/HTML custom page in Relativity aiR — a first "hello world" page, packaging and uploading the zip, wiring it into a tab, or diagnosing custom-page permission and sandbox caching problems. Covers the client-side JS/HTML workflow specifically; server-side ASP.NET Web Forms and MVC pages are pointed at the platform's migration requirements rather than walked through.
---

# Relativity aiR JavaScript Custom Page Builder

You are guiding a Relativity developer through building a Relativity aiR Custom Page using the **JavaScript/HTML** workflow — the fastest path to a working page, with the lightest footprint and no server-side runtime.

**Relativity aiR only.** This covers the cloud platform, not Relativity Server (2024 / 2025 / 2026). If the user is on Server, say so rather than walking them through these steps — deployment and platform requirements differ.

**What this skill does and does not do.** It scaffolds the page files and bundles them into a correctly structured zip. It does **not** deploy anything to Relativity. That zip is the file the user uploads themselves via the **Custom Pages** tab. Steps 4 onward describe actions the user performs in the UI.

## When to use this skill

Trigger when the user mentions:
- Building, creating, scaffolding, or deploying a Relativity aiR custom page
- "Hello world" custom page in Relativity aiR
- Packaging or uploading a custom page zip
- Custom page permissions or sandbox deployment issues

For server-side .NET pages, see **Server-side custom pages** at the end — that is a supported but different workflow.

## Step 1 — Confirm this is the right path

This skill covers the **JavaScript/HTML** flavor of custom pages only.

If the user has not said which technology they are using, confirm once:

> This skill walks through the **JavaScript/HTML** flavor of custom pages — fastest to build, lightest footprint, no server-side runtime. If you need server-side .NET (ASP.NET Web Forms or MVC), that's a supported but different workflow with its own platform requirements, and I'd point you at those docs rather than walk you through it here. Good to continue with JS/HTML?

If the user does not push back, proceed. If they do need server-side, **do not improvise a .NET walkthrough** — send them to the resources in **Server-side custom pages** below.

## Step 2 — Scaffold the JS/HTML "Hello World" page

Create a folder for the page and two files inside it. Relativity serves the contents of the zip flat, so all asset paths must be relative and `index.html` must be at the root of the zip (not nested in a subfolder).

**`index.html`**
```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Hello Relativity aiR</title>
    <script src="index.js" defer></script>
</head>
<body>
    <h1>Hello Relativity aiR</h1>
    <div id="output"></div>
</body>
</html>
```

**`index.js`**
```js
(function () {
    console.log("Custom Page loaded");
    // REST calls go here. The browser session carries the user's auth cookies,
    // so calls to /Relativity.REST/... run as the signed-in user.
    // Required headers for most REST endpoints:
    //   X-CSRF-Header: -
    //   Content-Type: application/json
})();
```

**Bundle your own assets.** Relativity no longer hosts most assets for custom pages by default — including images, stylesheets, and JavaScript files. If the page references a file that lives inside Relativity but outside your own custom page, copy it into the page so it stays available. Likewise, ASMX services and WebMethods are no longer hosted; use Relativity's Platform APIs or an application's Kepler services instead.

## Step 3 — Package the zip correctly

The single most common mistake is zipping the **folder** instead of the **contents**. `index.html` must sit at the root of the archive.

- Select `index.html`, `index.js`, and any subfolders (e.g. `css/`, `img/`) — **not** the parent folder.
- Zip the selection. The resulting `.zip` should list `index.html` at the top level when opened.
- The zip's filename becomes the resource file name in Relativity. Use something descriptive like `helloCustomPage.zip`.

**One zip per application.** An application may contain only a **single** Custom Page zip. That does not stop one application from surfacing several pages in the Relativity UI — it means the code for all of them must live in the same zip. If you are adding a page to an application that already has one, extend the existing zip rather than uploading a second.

## Step 4 — Upload the zip and wire it up

These are steps **the user performs in the Relativity UI**. This skill produces the zip; it
does not deploy anything. Walk them through the steps — never state that the page has been
deployed.

1. **Pick or create a Relativity Application** (workspace-level or system-level) that the page belongs to. In Relativity aiR the page must live inside an application — workspace-level Custom Pages tabs are not always present, and deploying via an application is the supported path.
2. Navigate to the **Custom Pages** tab and click **New Custom Page**. The Custom Page Information dialog opens; the application is selected inside that dialog rather than by opening the application first.
3. Set:
   - **Name** — display name for the page.
   - **Application** — the application from step 1. (See known issue below.)
   - **File** — upload the zip from Step 3. (Remember: one zip per application — see Step 3.)
4. **Save.**
5. **⚠️ Known issue — re-check the Application field.** After saving, Relativity may silently reset the **Application** field to **System**. Re-open the saved record, set **Application** back to your target app, and save again. Confirm it sticks before moving on.
6. **Wire the page into the UI** — typically by adding a **Tab** that points at the custom page, or by linking from an Object Type / event handler.
7. **Push or install the application** so the page is reachable in the target environment (Application → Push to Library, or deploy via a RAP file).

## Step 5 — Sandbox development tip: hard refresh

When iterating in a Relativity aiR **sandbox**, the browser caches the zip's contents aggressively. After re-uploading the page or editing `index.html`/`index.js`, updates will not appear until you **hard refresh**:

- Windows/Linux: `Ctrl + F5` (or `Ctrl + Shift + R`)
- macOS: `Cmd + Shift + R`

If a hard refresh still shows stale content, clear cached site data in dev tools (Application → Storage → Clear site data) or use a private/incognito window. **Surface this tip early in any troubleshooting conversation** — it resolves a large fraction of "my change didn't deploy" reports.

## Step 6 — Permissions reminder

Relativity REST APIs run as the **signed-in user**. There is no service-account elevation on a custom page — whatever the user can see in the UI is what `fetch` calls can see. If a call returns 401/403:

1. Verify the user has the relevant object/tab/field/workspace permissions.
2. Verify the application is installed and the user has access to it.
3. Only then suspect the code.

## Server-side custom pages (out of scope for this skill)

Server-side .NET custom pages are **supported** in Relativity aiR — not deprecated. MVC in particular is still supported and does not require ASP.NET Session State. They are out of scope here only because the scaffolding and packaging steps above are JS/HTML-specific.

Custom pages run on Relativity aiR Compute, which imposes requirements a server-side page must meet:

- **Be stateless** — sticky sessions are gone; requests may be served by different instances of the page
- **No ASP.NET Session State** — refactor it out
- **Tolerate cold in-memory cache hits** — per-user in-memory caching is no longer reliable
- **One Custom Page zip per application** — same rule as Step 3
- **No SignalR** — not supported on the new platform
- **No admin permissions on the container** — the page runs as a reduced-privilege Windows user

When a user needs this path, point them at the migration checklist and the Visual Studio tutorial below rather than improvising a .NET walkthrough.

## Reference documentation

Always link the official docs when guiding the user:

- Lesson 5 — Create a custom page (end-to-end walkthrough incl. zip and upload): https://platform.relativity.com/RelativityOne/Content/Get_started_RelativityOne/Create_a_custom_page.htm
- Customizing the UI (overview): https://platform.relativity.com/RelativityOne/Content/Customizing_the_UI/Customizing_the_UI.htm
- Basic concepts for custom pages: https://platform.relativity.com/RelativityOne/Content/Customizing_the_UI/Basic_concepts_for_custom_pages.htm
- Best practices for custom pages: https://platform.relativity.com/RelativityOne/Content/Customizing_the_UI/Best_practices_for_custom_pages.htm
- Publishing and uploading custom pages: https://platform.relativity.com/RelativityOne/Content/Customizing_the_UI/Publishing_and_uploading_custom_pages.htm
- Advanced functionality for custom pages: https://platform.relativity.com/RelativityOne/Content/Customizing_the_UI/Advanced_functionality_for_custom_pages.htm
- Build your first custom page (Visual Studio / .NET tutorial): https://platform.relativity.com/RelativityOne/Content/Customizing_the_UI/Building_your_first_custom_page.htm
- Custom Pages migration checklist (aiR Compute requirements): https://platform.relativity.com/RelativityOne/Content/Customizing_the_UI/Custom_Pages_migration.htm
- Custom Pages .NET API reference (server-side paths only): https://platform.relativity.com/RelativityOne/Content/Customizing_the_UI/Custom_Pages_NET_API_Reference.htm
- Permissions in Relativity: https://help.relativity.com/RelativityOne/Content/Relativity/Security_permissions/Managing_security.htm

## Output style

- Walk the user through the steps in order; do not dump the entire skill at once.
- After each step, confirm before continuing — especially after deployment, where the Application-reset issue bites first-time users.
- When the user reports a problem, ask first whether they hard-refreshed and whether the Application field saved correctly. These two checks resolve the majority of reported issues.
