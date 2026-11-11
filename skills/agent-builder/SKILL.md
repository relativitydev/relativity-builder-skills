---
name: agent-builder
description: Use when someone wants to create, scaffold, build, deploy, scale, or troubleshoot a custom agent (background process) for Relativity aiR, including a scheduled or recurring job, a queue worker, a manager/worker pattern, or a Workload Discovery endpoint that scales agents dynamically. Produces a compiling .NET Framework class library from tested templates and walks through the resource-file deployment. Not for code that must react instantly to a user action (use an event handler) or answer a request immediately (use a Kepler service).
---

# Relativity aiR Agent Builder

You are helping a developer build a custom **agent** for Relativity aiR: a class that Relativity runs in the background on an interval, with no user signed in.

**Relativity aiR only.** This covers the cloud platform, not Relativity Server. If the user is on Server, say so; deployment and hosting differ.

**What this skill does and does not do.** It produces a class library that compiles, from templates that are built and checked on every change to this repository. It does **not** deploy anything. Uploading the DLL and checking the agent runs are steps the user performs in Relativity; walk them through those steps and never say the agent has been deployed.

## Step 1: Confirm an agent is the right tool

An agent fits work that is slow, recurring, or queued: processing a backlog, syncing data on a schedule, cleaning up records. Check before building:

| The user wants... | Use instead |
|---|---|
| Code that runs when someone saves, deletes, or opens an object | An event handler (only for work under about a second; hand longer work to an agent) |
| An endpoint that returns an answer right away | A Kepler service |
| A job that starts the moment a user clicks something | A Kepler service or event handler that queues the work, plus this agent to process it |

Agents start with about 30 seconds of delay on average, so they are never the right tool for an immediate response.

## Step 2: Scaffold from the templates

Copy `templates/Agents/` from this skill into the user's solution. It contains:

- `MyCompany.MyApp.Agents.csproj`: an SDK-style project targeting `net462`
- `SampleAgent.cs`: the agent class

Then make it theirs:

1. Rename the namespace, project, and assembly from `MyCompany.MyApp` to the user's own (for example `Contoso.Discovery`).
2. Rename the class and set the `[Name("...")]` attribute and the `Name` property to the display name the user wants in the Agents tab.
3. **Replace every `00000000-0000-0000-0000-000000000000` with a new GUID.** Generate one per agent (`[guid]::NewGuid()` in PowerShell, `uuidgen` on macOS/Linux, or `System.Guid.NewGuid()`). The GUID identifies the agent type. Never reuse a GUID across agents, and never change it after the agent has been deployed.
4. Build: `dotnet build -c Release`. It must build with no errors or warnings before moving on.

Keep these exactly as the template has them. Each one fixes a failure people hit:

- **`[System.Runtime.InteropServices.Guid("...")]`, fully qualified.** With `using System;` in scope, a bare `[Guid(...)]` binds to the `System.Guid` struct and fails with `CS0616: 'Guid' is not an attribute class`.
- **`<TargetFramework>net462</TargetFramework>`.** Relativity's documentation specifies .NET Framework 4.6.2 for agents, event handlers, and Kepler services. Do not move to .NET 6/8; the host is .NET Framework.
- **Packages from nuget.org:** `Relativity.Agent`, `Relativity.API`, and, for calling Relativity APIs, `Relativity.Services.Interfaces.SDK` (Object Manager is `Relativity.Services.Objects.IObjectManager`, models in `Relativity.Services.Objects.DataContracts`). Its DLLs (`Relativity.Services.Interfaces.dll`, `Relativity.Services.DataContracts.dll`) are on Relativity's list of assemblies it provides at runtime, so there is nothing extra to upload. Do not use the newer `Relativity.ObjectManager.SDK` (namespace `Relativity.ObjectManager.V1`) in server-side code: its DLLs are not on that list. Never use RSAPI (`kCura.Relativity.Client`).
- **`ExcludeAssets="runtime"` on every Relativity package.** Relativity supplies those DLLs at runtime, and Relativity's documentation says not to ship your own copies. Excluding them keeps them out of `bin`, so everything left in `bin/Release/net462` is exactly what to upload.
- **`Microsoft.NETFramework.ReferenceAssemblies`** lets the project build on any machine with the .NET SDK, including macOS, Linux, and CI, without a .NET Framework targeting pack.

## Step 3: Write `Execute()` the way Relativity runs it

Relativity calls `Execute()` on every run. Fill in `GetPendingWork()` and `ProcessWorkItem()` in the template, and keep to these rules (from Relativity's agent best practices and resiliency guidelines):

- **Do a bounded batch, then return.** Loop while there is work, but return when there is none, and cap each run (the template's `BatchSize`). The agent runs again on its next interval. Join any threads you start before returning; the process can be destroyed after `Execute()` returns.
- **Expect to be stopped at any time.** Agents are ephemeral and can be pre-empted. Check `ToBeRemoved` between items and stop at a safe point. Keep no long-lived state in memory or on local disk. Store progress in a queue table or on Relativity objects so the next run can pick up.
- **Don't rely on `AgentID`.** It is not stable. Use your queue for persistence.
- **No user context.** Create API proxies with `ExecutionIdentity.System`: `Helper.GetServicesManager().CreateProxy<IObjectManager>(ExecutionIdentity.System)`.
- **`Execute()` is synchronous.** Call async APIs with `.GetAwaiter().GetResult()`.
- **Retries.** Relativity provides Polly 5.7.0 to agents. Reference it if you want it, but do not upload `Polly.dll`. When retries are exhausted, set `DidWork = false` so the agent exits and a fresh one can start.
- **Fileshare paths.** Use fully qualified (FQDN) UNC paths. The fileshare mounts on the agent's first file operation, so call `Directory.Exists(path)` before handing a path to anything else.
- **Don't** create AppDomains, host HTTP endpoints from an agent (use a Kepler service), or use direct SQL where an API exists.
- **Logging.** Use `Helper.GetLoggerFactory().GetLogger().ForContext<T>()` with message templates (`"Processed {Count} items"`, never string interpolation in log calls). Production Log Extractor shows Warning and above by default.
- **Visibility.** `RaiseMessage` / `RaiseError` report to the agent framework, but in testing on Relativity aiR the Agents tab **Message** column showed `Completed` after a successful run rather than the last `RaiseMessage` text. If the user needs a visible run history, write a row per run to a custom object (RDO) with Object Manager.

### Where the work comes from

An agent runs at the instance level: it has **no current workspace**. Decide up front how it finds work, because every Object Manager call needs a workspace ID:

- **A job queue table**: Relativity's guidance is one table in the EDDS (instance) database, not per workspace, created by a Post Install event handler (see the event-handler-builder skill). Each row records the workspace and the item. Whatever queues the work (an event handler, a Kepler service, a user action) writes the row.
- **Every workspace the application is installed in**: ask Relativity which workspaces have the application, as Relativity's "Build a cron job" tutorial does: `IApplicationInstallManager.SearchApplicationAsync(-1, applicationGuid, ...)` from `Relativity.Services.Interfaces.LibraryApplication` (same platform-provided package), then query each workspace.
- **A fixed workspace**: only for single-workspace tools. Read the ID from configuration, never a hard-coded constant.

### When more than one process runs

Once Workload Discovery scales the agent beyond `One`, several processes run `Execute()` at the same time and will see the same pending items. Make each item **claimed** before it is processed: move it to an in-progress state with an update that fails if someone else changed it first (read the item with Object Manager `ReadAsync`, then pass the result's `ObjectVersionToken` on the `UpdateRequest` to turn on overwrite protection), or take rows from a queue table with a locking query. Make processing idempotent so an item picked up twice after a crash is harmless. Items stuck in-progress after a crash need a timeout that returns them to pending.

### Object Manager quick reference

Checked against a live Relativity aiR instance. Package `Relativity.Services.Interfaces.SDK`, namespaces `Relativity.Services.Objects` and `Relativity.Services.Objects.DataContracts`.

| Need | How |
|---|---|
| Object type | `new ObjectTypeRef { Guid = ... }` for your own objects; built-ins by `ArtifactTypeID`: Workspace 8 (query workspace `-1`), Document 10, Saved Search 15 |
| Count only | `QueryAsync(workspaceId, query, 1, 1)` and read `TotalCount` |
| Paging | `start` is **1-based** |
| A choice value | `"'<field guid>' IN CHOICE [<choice guid>]"` (IDs work too) |
| A list of IDs | `"'ArtifactID' IN [1001, 1002]"` |
| Many updates | one mass update (`MassUpdateByObjectIdentifiersRequest`) per batch, not one call per item |

An invalid condition throws `ValidationException` showing the parse position.

## Step 4 (optional): Workload Discovery for scaling and short intervals

Without Workload Discovery an agent runs one process, with a **minimum run interval of 60 minutes** (also the default). With a Workload Discovery endpoint the minimum drops to **1 minute** and Relativity runs more processes when the queue is large. Relativity recommends it for new agents. Each process gets 1 vCPU and 1 GB RAM; that is not configurable.

The endpoint is a small Kepler service. `templates/WorkloadDiscovery/` has a ready one (Interfaces and Services projects). If the user's application already has Kepler projects, add these files to them instead of creating new ones.

1. Copy `templates/WorkloadDiscovery/` and rename it as in Step 2. Keep `ServiceAudience(Audience.Private)`.
2. Rename `ISampleAgentWorkloadService` / `SampleAgentWorkloadService` after the user's agent (for example `ICustodianSyncWorkloadService`). **Never use a generic name like `IWorkloadDiscoveryService`**: if another service in the same application has the same interface name, Kepler refuses to host it, and in testing on Relativity aiR that stopped every Kepler service in the application.
3. In `SampleAgentWorkloadService.cs`, set `SampleAgentGuid` to **the agent's own GUID** and replace `CountPendingWorkAsync()` with a fast count of the real queue. It must answer within **2 seconds** and return `WorkloadSize.None` when there is no work. Sizes are `None`, `One`, `S`, `M`, `L`, `XL`, `XXL`; agents with light or steady workloads should not scale beyond `One`.
4. In `SampleAgent.cs`, uncomment the `[WorkloadDiscovery.CustomAttributes.Path(...)]` line and put the agent's GUID in the path. The path starts with `relativity.rest/api/` and has **no leading slash** (a leading slash causes errors). Add `<PackageReference Include="Relativity.Platform.Agent.WorkloadDiscovery.SDK" Version="0.4.1" />` to the agent project **without** `ExcludeAssets`; `WorkloadDiscovery.dll` is not a Relativity platform DLL and must be uploaded.
5. The count logic is needed by both the agent and the endpoint. Put it in a class both projects can use (a shared source file or a small shared library) rather than writing it twice.

Keep the `Interfaces` and `Services` folder names, or update the `ProjectReference` path in the Services project if you rename them.

**Run interval.** Relativity sets a new agent's interval to 60 minutes. A custom interval is the agent's `DefaultInterval` (in seconds) in the application's `application.xml`; Relativity's documentation says it cannot be changed in the UI and that intervals under 60 minutes need Workload Discovery. In testing on Relativity aiR, an agent with a Workload Discovery path showed a run interval of 5 seconds in the Agents tab: with Workload Discovery, how often work actually runs is driven by the endpoint.

## Step 5: Deploy (the user does this in Relativity)

Walk the user through these; do not claim to have done them.

1. In **Relativity Applications**, open or create the application the agent belongs to, and make sure it has been **published to the Application Library** before adding resource files.
2. In **Resource Files**, upload each DLL found in the projects' `bin/Release/net462` folders **once** (the same DLL, such as `WorkloadDiscovery.dll` or the Interfaces DLL, shows up in more than one folder), each linked to that application (not `Default`). Upload `.pdb` files too for readable stack traces. Never upload Relativity's own DLLs (the templates already keep them out of `bin`).
3. The agent type registers itself from the DLL and appears on the **Agents** tab within a few minutes. In testing on Relativity aiR, an agent instance was also created and running within about three minutes of upload, with no manual step. Agents cannot be removed from the Agents tab.
4. **Updating:** upload the new DLL over the existing resource file. **Removing** an agent: upload a version of the DLL without the agent class. If it was the application's last agent, wait 5 minutes before deleting the resource file. Deleting the application does not remove its resource files.

## Step 6: Verify

- **Agents** tab: the agent appears and **Time of Last Check-in** advances.
- The work itself happened (objects updated, queue drained, run-history rows written).
- With Workload Discovery: call the endpoint (see the kepler-service-builder skill for a test client) and confirm it returns quickly with the expected size.

## Troubleshooting

- **`CS0616: 'Guid' is not an attribute class`**: use the fully qualified `System.Runtime.InteropServices.Guid` attribute.
- **`CS8630` or nullable/`ImplicitUsings` errors**: the project was created from a .NET 6+ template. Use the template's csproj (`net462`, `LangVersion` latest, no `Nullable`/`ImplicitUsings`).
- **Agent type never appears**: the resource file is linked to the wrong application (check it is not `Default`), or the application was not in the Application Library first. Wait a few minutes after upload.
- **Agent runs but nothing happens**: check logs (Warning and above in Log Extractor), then confirm the System identity can see the objects the agent queries.
- **Every Kepler service in the application returns 404 at once** after adding the Workload Discovery DLLs: another service in the application has the same interface name. Give the interface a name specific to the agent and re-upload.
- **Agent never runs with Workload Discovery**: call the endpoint yourself. A 404 or a response slower than 2 seconds means Relativity sees no work. Check the `[Path]` has no leading slash and contains the agent's own GUID.
- **Opening a support case**: include the Workload Discovery URI with its response code and body, the run interval, and logs. Relativity's documentation lists these as required.

## Reference documentation

- Basic concepts for agents: https://platform.relativity.com/RelativityaiR/Content/Background_processing/Basic_concepts_for_agents.htm
- Best practices for agents: https://platform.relativity.com/RelativityaiR/Content/Background_processing/Best_practices_for_agents.htm
- Agent resiliency guidelines: https://platform.relativity.com/RelativityaiR/Content/Background_processing/Agent_resiliency_guidelines.htm
- Scaling guidelines (Workload Discovery): https://platform.relativity.com/RelativityaiR/Content/Background_processing/Scaling_guidelines_for_Agents.htm
- Execution patterns for agents: https://platform.relativity.com/RelativityaiR/Content/Background_processing/Execution_patterns_for_agents.htm
- Build a cron job (tutorial): https://platform.relativity.com/RelativityaiR/Content/Get_started_RelativityaiR/Build_a_cron_job.htm
- Assemblies provided by Relativity (do not upload): https://platform.relativity.com/RelativityaiR/Content/Building_Relativity_applications/Best_practices_for_application_building.htm

## Output style

- Generate the files, build them, and fix any error before showing the result. Show the build output.
- Then list exactly which DLLs to upload: each DLL in the projects' `bin/Release/net462` folders, once.
- In a conversation, walk through deployment one step at a time and confirm before moving on. When producing a single written answer, give every step in order.
