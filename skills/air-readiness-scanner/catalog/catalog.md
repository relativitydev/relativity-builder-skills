# Server to Relativity aiR compatibility catalog

Generated 2026-11-24 from Relativity's public documentation. Used by the `air-readiness-scanner` skill as its rule set. Do not edit by hand; changes come from the maintainers' export.

39 entries with code signatures, 1 planning items with nothing to scan for.

| ID | Title | Status | Use instead |
| --- | --- | --- | --- |
| DATA-001 | Direct SQL access through IDBContext | Restricted in aiR | Object Manager API and the other Kepler services |
| DATA-002 | Direct SQL access to Processing datastores (INV databases) | Removed in aiR | Processing API (v2) |
| DATA-003 | Direct SQL access to the EDDSArchiving database | Removed in aiR | ARM REST API V3 |
| DATA-004 | Custom SQL triggers in Relativity databases | Removed in aiR | Event handlers, or polling through Object Manager |
| DATA-005 | Reading logs from EDDSLogging or local log files | Works differently in aiR | External Logging (preferred) or IAPILog |
| FILE-001 | Direct fileshare and UNC path access | Restricted in aiR | Relativity Transfer SDK, Import Service API, Export Service API |
| SDK-001 | Server SDK packages (Relativity.Server.* from Artifactory) | Replaced in aiR | Matching Relativity.* packages on nuget.org, normal aiR packaging |
| API-001 | RSAPI (Relativity Services API) | Removed in aiR | Object Manager API and the per-domain Kepler services (see RSAPI migration matrix) |
| API-002 | Unversioned Audit API | Replaced in aiR | Versioned Audit API (REST and .NET) |
| API-003 | Unversioned Production API (Relativity.Productions.Client) | Replaced in aiR | Relativity.Productions.SDK, or the versioned REST URLs |
| API-004 | Classic List Page API (Relativity.ListPage) | Replaced in aiR | Relativity Lists API |
| API-005 | Analytics Classification API | Removed in aiR | none listed |
| API-006 | Processing API v0 and v1 document/filter managers | Replaced in aiR | Processing API v2 |
| API-007 | Writing to the Errors tab / Errors API | Removed in aiR | Job-level error surfacing, IAPILog |
| API-008 | Legacy Relativity.ObjectManager NuGet package | Replaced in aiR | Relativity.ObjectManager.SDK |
| API-009 | Legacy Document Viewer Kepler endpoints | Removed in aiR | New Document Viewer REST endpoints |
| API-010 | Workspace Manager deprecated endpoints and fields | Works differently in aiR | See developer group posts linked from the changelog |
| API-011 | Batch Sets Manager and Batches Manager APIs | Removed in aiR | TBD (see developer group post) |
| API-012 | Keyboard Shortcuts Manager API | Works differently in aiR | TBD (see developer group post) |
| API-013 | Cold Storage endpoints and the workspace Status field | Works differently in aiR | Workspace State field, current Cold Storage API |
| TEST-001 | Relativity Testing Framework (RTF) | Removed in aiR | Call the Relativity APIs directly from tests |
| IMP-001 | Legacy Import API (WinEDDS / DataExchange based) | Replaced in aiR | Import Service API (REST) / Relativity.Import.SDK; Export Service API for exports |
| XFER-001 | Legacy Transfer API (TAPI, Relativity.Transfer.Client) and old Transfer SDK versions | Replaced in aiR | Relativity.Transfer.SDK (current version) |
| AUTH-002 | Legacy permissions API writes and SQL writes to permission tables | Restricted in aiR | Access Control API / Next-Gen permissions API |
| PAGE-001 | Server-side custom pages (ASP.NET Web Forms and MVC) | Works differently in aiR | Same page, refactored to the migration checklist; or a static HTML/JS page calling REST APIs |
| EH-001 | Classic Forms customizations under Relativity Forms | Works differently in aiR | Relativity Forms API (JavaScript pipelines via ScriptFileNames) |
| SCR-001 | Relativity Scripts (custom SQL scripts) | Removed in aiR | APIs or native features (see sql-to-api-converter skill); Automated Workflows for some |
| AGENT-001 | Agents on aiR's compute platform | Works differently in aiR | Retry plus shutdown pattern from the resiliency guidelines; Relativity.Platform.Agent.WorkloadDiscovery.SDK for scaling |
| INFRA-001 | Managing agents, servers, and resource pools from code | Restricted in aiR | none (Relativity manages agents) |
| API-014 | Invariant API | Removed in aiR | Processing API |
| API-016 | Object Query Manager and View Fields Manager APIs | Replaced in aiR | Object Manager API (query) |
| API-017 | Legacy unversioned Imaging and Tab Manager methods | Replaced in aiR | Versioned Imaging API and Tab Manager API |
| API-018 | Integration Points kCura.* namespaces | Replaced in aiR | Relativity.IntegrationPoints.* namespaces and current DLLs |
| PAGE-002 | Removed main-window JavaScript functions and hosted libraries | Removed in aiR | Bundle libraries in the page, use window.relativity.* APIs and native browser APIs |
| AGENT-002 | Shipping assemblies that clash with platform-provided ones | Restricted in aiR | Use the platform-provided version, or internalize the dependency |
| API-019 | Pre-2022 unversioned Kepler API namespaces and URLs | Replaced in aiR | The versioned (V1+) namespace and REST URL for each API |
| API-020 | Object Manager request rate limits and data egress limits | Restricted in aiR | Pace with RateLimit headers, retry on Retry-After, send X-Kepler-Referrer, use Export Service API for bulk text |
| DATA-007 | Audit and login data read straight from SQL | Replaced in aiR | Audit API (relativity-audit/v1) |
| API-021 | ARM API V1, Relativity.ARM.SDK, and unversioned ARM URLs | Replaced in aiR | ARM REST API V3 |
| UI-001 | Classic Viewer extensions | Replaced in aiR | Review Interface extension (Review API) |

## Details

### DATA-001: Direct SQL access through IDBContext

**Restricted in aiR.** Server versions: all.

Relativity aiR still documents IDBContext, but direct SQL from app code isn't a long-term path in the cloud. Relativity's scripts guidance notes that data stored in SQL may be moved to different storage in the future, and transactions opened inside the SQL text (BEGIN TRAN) aren't supported. For anything you're porting or building new, use Object Manager and the other Relativity APIs instead of GetDBContext.

**Use instead:** Object Manager API and the other Kepler services

**Documentation:**
- [Advanced code sample for database interactions](https://platform.relativity.com/RelativityaiR/Content/Relativity_API_Helpers/Advanced_code_sample_for_database_interactions.htm) (Relativity aiR)
- [Best practices for Relativity scripts](https://platform.relativity.com/RelativityaiR/Content/Scripts/Best_practices_for_Relativity_scripts.htm) (Relativity aiR)

### DATA-002: Direct SQL access to Processing datastores (INV databases)

**Removed in aiR.** Server versions: all.

Direct SQL access to the Processing datastores (the INV databases) was removed from aiR on March 18, 2026, per the Developer Changelog. If your code or scripts query those databases, that workflow needs to move to the Processing API.

**Use instead:** Processing API (v2)

**Documentation:**
- [Release notes](https://platform.relativity.com/RelativityaiR/Content/What_s_new/Release_notes.htm) (Relativity aiR)

### DATA-003: Direct SQL access to the EDDSArchiving database

**Removed in aiR.** Server versions: all.

Per the Developer Changelog, direct SQL access to the EDDSArchiving database is removed in aiR as of October 1, 2026. Integrations that read ARM job state from that database need to move to the ARM REST API V3.

**Use instead:** ARM REST API V3

**Documentation:**
- [Release notes](https://platform.relativity.com/RelativityaiR/Content/What_s_new/Release_notes.htm) (Relativity aiR)

### DATA-004: Custom SQL triggers in Relativity databases

**Removed in aiR.** Server versions: all.

The Developer Changelog (May 1, 2026) says custom SQL triggers in Relativity databases should no longer be used in aiR. If your app installs triggers to react to data changes, move that logic into event handlers, or poll for changes through Object Manager.

**Use instead:** Event handlers, or polling through Object Manager

**Documentation:**
- [Release notes](https://platform.relativity.com/RelativityaiR/Content/What_s_new/Release_notes.htm) (Relativity aiR)

### DATA-005: Reading logs from EDDSLogging or local log files

**Works differently in aiR.** Server versions: all.

The aiR docs don't cover reading Relativity's log tables or local log folders the way Server does. The documented approach for apps is External Logging, where your app sends its own logs (Serilog, ILogger, and so on) to a logging service you control, configured through your app's instance settings (LoggingLevel, LoggingEndpoint, LoggingToken). Plan on wiring that up as part of the move.

**Use instead:** External Logging (preferred) or IAPILog

**Documentation:**
- [External Logging](https://platform.relativity.com/RelativityaiR/Content/Logging/External_Logging.htm) (Relativity aiR)
- [Logging](https://platform.relativity.com/Server2026/Content/Logging/Logging.htm) (Server 2026)
- [SDK API Comparison](https://platform.relativity.com/Server2026/Content/Relativity_Platform/SDK_API_Comparison.htm) (Server 2026)

### FILE-001: Direct fileshare and UNC path access

**Restricted in aiR.** Server versions: all.

aiR doesn't give apps open access to the fileshare. Since June 2023 agents no longer connect to every fileshare on startup, and since September 2024 staging boundaries block reading or writing outside the defined staging area, with the FTA, Temp, Export, and dtSearch folders removed. Move file work off UNC paths and onto the Relativity Transfer SDK and the Import and Export Service APIs.

**Use instead:** Relativity Transfer SDK, Import Service API, Export Service API

**Documentation:**
- [Release notes](https://platform.relativity.com/RelativityaiR/Content/What_s_new/Release_notes.htm) (Relativity aiR)

### SDK-001: Server SDK packages (Relativity.Server.* from Artifactory)

**Replaced in aiR.** Server versions: 2023, 2024, 2025, 2026.

aiR apps build against the Relativity packages on nuget.org, not the Relativity.Server.* packages from the Server feeds, and Server RAP Builder doesn't build aiR packages. The SDK comparison page lists the matching aiR package for each Server one, and Relativity's guidance is to keep separate Server and aiR branches. For a lot of apps this ends up being mostly a package swap and a rebuild.

**Use instead:** Matching Relativity.* packages on nuget.org, normal aiR packaging

**Documentation:**
- [Server SDK Changes](https://platform.relativity.com/Server2026/Content/What_s_new/Server_SDK_Changes.htm) (Server 2026)
- [SDK API Comparison](https://platform.relativity.com/Server2026/Content/Relativity_Platform/SDK_API_Comparison.htm) (Server 2026)
- [Application Compatibility Verification in Relativity](https://platform.relativity.com/Server2026/Content/RAP_Builder/Application_Compatibility_Verification_in_Relativity.htm) (Server 2026)

### API-001: RSAPI (Relativity Services API)

**Removed in aiR.** Server versions: 10.3, 2021, 2022.

RSAPI is gone in aiR. It stopped working on October 23, 2021, and it was also removed from Server 2023. Code that uses kCura.Relativity.Client needs to move to Object Manager and the other Kepler services, and the RSAPI migration matrix maps each feature to its replacement.

**Use instead:** Object Manager API and the per-domain Kepler services (see RSAPI migration matrix)

**Documentation:**
- [Release notes](https://platform.relativity.com/RelativityaiR/Content/What_s_new/Release_notes.htm) (Relativity aiR)
- [RSAPI deprecation process](https://platform.relativity.com/Server2026/Content/What_s_new/RSAPI_deprecation_process.htm) (Server 2026)
- [RSAPI migration strategy](https://platform.relativity.com/Server2026/Content/What_s_new/RSAPI_migration_strategy.htm) (Server 2026)

### API-002: Unversioned Audit API

**Replaced in aiR.** Server versions: all.

The unversioned Audit API was deprecated in aiR in June 2023, so code that references Relativity.Audit.Services.Unversioned.Interfaces or calls the non-versioned URL needs to move to the versioned Audit API. The LuceneSearch keyword was also removed from Audit API conditions in 2025, with "is like" and "is not like" filtering on the Details field in its place, and the Audit Metrics count and size endpoint was retired October 1, 2025.

**Use instead:** Versioned Audit API (REST and .NET)

**Documentation:**
- [Release notes](https://platform.relativity.com/RelativityaiR/Content/What_s_new/Release_notes.htm) (Relativity aiR)

### API-003: Unversioned Production API (Relativity.Productions.Client)

**Replaced in aiR.** Server versions: all.

The unversioned Production APIs have been deprecated and unsupported in aiR since July 28, 2023. If your code references Relativity.Productions.Client or calls the non-versioned Production URL, switch to Relativity.Productions.SDK or the versioned REST endpoints.

**Use instead:** Relativity.Productions.SDK, or the versioned REST URLs

**Documentation:**
- [Release notes](https://platform.relativity.com/RelativityaiR/Content/What_s_new/Release_notes.htm) (Relativity aiR)

### API-004: Classic List Page API (Relativity.ListPage)

**Replaced in aiR.** Server versions: all.

The classic list page API (Relativity.ListPage) has been replaced by Relativity.Lists in aiR. Third-party apps with List Page Interaction event handlers needed equivalent Relativity.Lists handlers by July 31, 2025, and some events such as listPageChange and newItemButtonInit were removed. The Classic List Page API deprecation page walks through what changed and what to use instead.

**Use instead:** Relativity Lists API

**Documentation:**
- [Classic List Page API deprecation](https://platform.relativity.com/RelativityaiR/Content/What_s_new/Classic_List_Page_API_deprecation.htm) (Relativity aiR)
- [Release notes](https://platform.relativity.com/RelativityaiR/Content/What_s_new/Release_notes.htm) (Relativity aiR)

### API-005: Analytics Classification API

**Removed in aiR.** Server versions: all.

The Analytics Classification API (.NET and REST) was removed from aiR on December 20, 2024, and the changelog doesn't name a direct replacement. Any code that calls it needs to be redesigned before the move.

**Use instead:** none listed

**Documentation:**
- [Release notes](https://platform.relativity.com/RelativityaiR/Content/What_s_new/Release_notes.htm) (Relativity aiR)

### API-006: Processing API v0 and v1 document/filter managers

**Replaced in aiR.** Server versions: all.

aiR has moved the Processing API to v2. The v1 ProcessingDocumentManager and ProcessingFilterManager were removed January 6, 2026 in favor of v2, which uses a string LogicalID instead of the long FileID, the v0 APIs stopped working July 6, 2026, and the static filter endpoints (CreateExpressionFilterAsync, GetFiltersAsync, and the rest) were removed July 14, 2026. Port these calls to Processing API v2.

**Use instead:** Processing API v2

**Documentation:**
- [Release notes](https://platform.relativity.com/RelativityaiR/Content/What_s_new/Release_notes.htm) (Relativity aiR)

### API-007: Writing to the Errors tab / Errors API

**Removed in aiR.** Server versions: all.

The Errors tab was removed from aiR in July 2025, and as of January 2026 the Errors API is only a stub, so no error data gets written. Code that writes to the Errors tab won't fail, but nobody will ever see those errors. Send them to your app's own logging, or surface them in your job's status instead.

**Use instead:** Job-level error surfacing, IAPILog

**Documentation:**
- [Release notes](https://platform.relativity.com/RelativityaiR/Content/What_s_new/Release_notes.htm) (Relativity aiR)

### API-008: Legacy Relativity.ObjectManager NuGet package

**Replaced in aiR.** Server versions: 10.3, 2021, 2022.

The legacy Relativity.ObjectManager NuGet package is deprecated. Per the Developer Changelog it was marked deprecated June 22, 2026 and is scheduled to be removed from NuGet in October 2026. Switch the reference to Relativity.ObjectManager.SDK.

**Use instead:** Relativity.ObjectManager.SDK

**Documentation:**
- [Release notes](https://platform.relativity.com/RelativityaiR/Content/What_s_new/Release_notes.htm) (Relativity aiR)

### API-009: Legacy Document Viewer Kepler endpoints

**Removed in aiR.** Server versions: all.

The legacy Document Viewer Kepler API endpoints are deprecated in aiR and replaced with new REST API endpoints, per the June 18, 2026 Developer Changelog entry. Code that calls the old Document Viewer services should move to the new REST endpoints, and the developer group post linked from the changelog has the details.

**Use instead:** New Document Viewer REST endpoints

**Documentation:**
- [Release notes](https://platform.relativity.com/RelativityaiR/Content/What_s_new/Release_notes.htm) (Relativity aiR)
- [Persistent Highlight Service .NET](https://platform.relativity.com/Server2026/Content/BD_Document_Viewer/Persistent_Highlight_Service__.NET_.htm) (Server 2026)

### API-010: Workspace Manager deprecated endpoints and fields

**Works differently in aiR.** Server versions: all.

Several Workspace Manager endpoints were deprecated in aiR on August 3, 2026, and the UpdateAsync (PUT) endpoint is deprecated as of December 9, 2026. EnableDataGrid is also deprecated and is always set to true when creating or updating workspaces. The developer group posts linked from the Developer Changelog list the affected endpoints and what replaces them, so check any code that creates or updates workspaces against them.

**Use instead:** See developer group posts linked from the changelog

**Documentation:**
- [Release notes](https://platform.relativity.com/RelativityaiR/Content/What_s_new/Release_notes.htm) (Relativity aiR)

### API-011: Batch Sets Manager and Batches Manager APIs

**Removed in aiR.** Server versions: all.

The Batch Sets Manager and Batches Manager .NET APIs and REST endpoints are scheduled to be deprecated in aiR on March 1, 2027, per the Developer Changelog. If your app creates or reads batch sets through these APIs, plan the change now and check the developer group post linked from the changelog for the recommended path.

**Use instead:** TBD (see developer group post)

**Documentation:**
- [Release notes](https://platform.relativity.com/RelativityaiR/Content/What_s_new/Release_notes.htm) (Relativity aiR)

### API-012: Keyboard Shortcuts Manager API

**Works differently in aiR.** Server versions: all.

The Keyboard Shortcuts Manager .NET API and REST endpoints are scheduled to be deprecated in aiR on February 15, 2027, per the Developer Changelog. If your app manages keyboard shortcuts through this API, plan the change now and check the developer group post linked from the changelog.

**Use instead:** TBD (see developer group post)

**Documentation:**
- [Release notes](https://platform.relativity.com/RelativityaiR/Content/What_s_new/Release_notes.htm) (Relativity aiR)

### API-013: Cold Storage endpoints and the workspace Status field

**Works differently in aiR.** Server versions: all.

In aiR, Cold Storage is driven only by the system Workspace State field. Since July 1, 2025 the customer-defined Status field no longer affects Cold Storage or billing, and since September 20, 2026 moving a workspace to Cold Storage doesn't update Status either. CanManageColdStorageAsync and DeleteWorkspaceFromColdStorageAsync were removed July 1, 2026, so scripts and integrations should read Workspace State and use the current Cold Storage API.

**Use instead:** Workspace State field, current Cold Storage API

**Documentation:**
- [Release notes](https://platform.relativity.com/RelativityaiR/Content/What_s_new/Release_notes.htm) (Relativity aiR)

### TEST-001: Relativity Testing Framework (RTF)

**Removed in aiR.** Server versions: all.

The Relativity Testing Framework for aiR was removed on May 31, 2024, including the NuGet package and its docs. It's still available for Server, but tests that target aiR should call the Relativity APIs directly. This only affects test projects, not the app you ship.

**Use instead:** Call the Relativity APIs directly from tests

**Documentation:**
- [Release notes](https://platform.relativity.com/RelativityaiR/Content/What_s_new/Release_notes.htm) (Relativity aiR)
- [SDK API Comparison](https://platform.relativity.com/Server2026/Content/Relativity_Platform/SDK_API_Comparison.htm) (Server 2026)

### IMP-001: Legacy Import API (WinEDDS / DataExchange based)

**Replaced in aiR.** Server versions: all.

The legacy Import API (kCura.Relativity.ImportAPI, WinEDDS, DataExchange) doesn't work in aiR. It stopped functioning January 31, 2024, the DataExchange.Client.SDK was deprecated December 2, 2024, and the RDC hasn't worked in aiR since July 1, 2024. Use the Import Service API (REST) or Relativity.Import.SDK for loads, and the Export Service API for exports.

**Use instead:** Import Service API (REST) / Relativity.Import.SDK; Export Service API for exports

**Documentation:**
- [Release notes](https://platform.relativity.com/RelativityaiR/Content/What_s_new/Release_notes.htm) (Relativity aiR)
- [Server to RelativityOne for Administrators](https://help.relativity.com/RelativityaiR/Content/Getting_Started/Server_to_RelativityOne_for_Administrators.md) (Relativity aiR)
- [SDK API Comparison](https://platform.relativity.com/Server2026/Content/Relativity_Platform/SDK_API_Comparison.htm) (Server 2026)

### XFER-001: Legacy Transfer API (TAPI, Relativity.Transfer.Client) and old Transfer SDK versions

**Replaced in aiR.** Server versions: all.

The old Transfer API (Relativity.Transfer.Client with the Aspera and fileshare clients) doesn't work in aiR, and versions 7.4.10 and below stopped working January 31, 2024. The aiR Relativity.Transfer.SDK is a different API, versions before 4.2.2 stopped working August 3, 2026, and an update to the latest SDK was required on October 15, 2026. Build against the current Relativity.Transfer.SDK.

**Use instead:** Relativity.Transfer.SDK (current version)

**Documentation:**
- [Release notes](https://platform.relativity.com/RelativityaiR/Content/What_s_new/Release_notes.htm) (Relativity aiR)
- [SDK API Comparison](https://platform.relativity.com/Server2026/Content/Relativity_Platform/SDK_API_Comparison.htm) (Server 2026)

### AUTH-002: Legacy permissions API writes and SQL writes to permission tables

**Restricted in aiR.** Server versions: all.

In aiR, legacy SQL permissions for Staging Reports and Usage Reports were retired on November 10, 2026 as those features moved to the next-gen permission model, and the Permissions API no longer assigns them. Code that sets those permissions should switch to the matching next-gen role keys through the Access Control API.

**Use instead:** Access Control API / Next-Gen permissions API

**Documentation:**
- [Release notes](https://platform.relativity.com/RelativityaiR/Content/What_s_new/Release_notes.htm) (Relativity aiR)

### PAGE-001: Server-side custom pages (ASP.NET Web Forms and MVC)

**Works differently in aiR.** Server versions: all.

Server-side custom pages, including MVC, still run in aiR, but on Relativity's hosted compute with stricter rules. Pages have to be stateless with no ASP.NET Session State, handle cold caches, ship as one custom page zip per application, and can't use SignalR, ASMX web methods, or admin rights on the host, and most Relativity-hosted assets are gone. Work through the Custom Pages migration checklist, or rebuild simple pages as static HTML and JavaScript that call the REST APIs.

**Use instead:** Same page, refactored to the migration checklist; or a static HTML/JS page calling REST APIs

**Documentation:**
- [Custom Pages migration](https://platform.relativity.com/RelativityaiR/Content/Customizing_the_UI/Custom_Pages_migration.htm) (Relativity aiR)
- [Release notes](https://platform.relativity.com/RelativityaiR/Content/What_s_new/Release_notes.htm) (Relativity aiR)

### EH-001: Classic Forms customizations under Relativity Forms

**Works differently in aiR.** Server versions: all.

In aiR every object uses Relativity Forms, so classic layout customizations need rework. Script only loads from files listed in the handler's ScriptFileNames, HTML in a handler Message shows as plain text, script in custom text, labels, and HTML fields is sanitized, .NET handlers can't redirect, and PreLoad doesn't fire for new objects (use replaceGetNewObjectInstance in JavaScript). The Migrating to Relativity Forms guide covers each change, including Console event handlers.

**Use instead:** Relativity Forms API (JavaScript pipelines via ScriptFileNames)

**Documentation:**
- [Migrating To Relativity Forms](https://platform.relativity.com/RelativityaiR/Content/Relativity_Forms/Migrating_To_Relativity_Forms.htm) (Relativity aiR)

### SCR-001: Relativity Scripts (custom SQL scripts)

**Removed in aiR.** Server versions: all.

Relativity is retiring the scripts framework in aiR, so SQL-based Relativity Scripts, including ones packaged inside an app, aren't something to carry over. Admins also can't add custom scripts themselves in aiR. Move each script's logic to the Relativity APIs or a native feature as part of the migration.

**Use instead:** APIs or native features (see sql-to-api-converter skill); Automated Workflows for some

**Documentation:**
- [Scripts retirement](https://help.relativity.com/RelativityaiR/Content/System_Guides/Scripts_retirement.htm) (Relativity aiR)
- [Using scripts in Relativity](https://platform.relativity.com/RelativityaiR/Content/Scripts/Using_scripts_in_Relativity.htm) (Relativity aiR)
- [Server to RelativityOne for Administrators](https://help.relativity.com/RelativityaiR/Content/Getting_Started/Server_to_RelativityOne_for_Administrators.md) (Relativity aiR)

### AGENT-001: Agents on aiR's compute platform

**Works differently in aiR.** Server versions: all.

In aiR, Relativity runs and scales your agents, so there are no agent servers to manage. Each agent process gets 1 vCPU and 1 GB of RAM, and to scale out or run on an interval under 60 minutes you add a Workload Discovery endpoint. Agents can occasionally lose connectivity, so HTTP and fileshare calls need retries, and an agent that can't recover should exit so it gets a fresh host. Agents link to your app through Resource Files.

**Use instead:** Retry plus shutdown pattern from the resiliency guidelines; Relativity.Platform.Agent.WorkloadDiscovery.SDK for scaling

**Documentation:**
- [Scaling guidelines for Agents](https://platform.relativity.com/RelativityaiR/Content/Background_processing/Scaling_guidelines_for_Agents.htm) (Relativity aiR)
- [Agent resiliency guidelines](https://platform.relativity.com/RelativityaiR/Content/Background_processing/Agent_resiliency_guidelines.htm) (Relativity aiR)
- [Platform Changes](https://platform.relativity.com/RelativityaiR/Content/What_s_new/Platform_Changes.htm) (Relativity aiR)

### INFRA-001: Managing agents, servers, and resource pools from code

**Restricted in aiR.** Server versions: all.

Agents, servers, and resource pools are managed by Relativity in aiR. The Agent Manager, Server, and Server Utility APIs aren't documented for aiR, and the Agents, Servers, and Resource Pools tabs are secured by default. If your app creates agents or edits the AgentType table at install time, plan to drop that step and talk to Support about how the agents should be set up.

**Use instead:** none (Relativity manages agents)

**Documentation:**
- [Server to RelativityOne for Administrators](https://help.relativity.com/RelativityaiR/Content/Getting_Started/Server_to_RelativityOne_for_Administrators.md) (Relativity aiR)
- [Agent Manager](https://platform.relativity.com/Server2026/Content/BD_Infrastructure/Agent_Manager.htm) (Server 2026)

### API-014: Invariant API

**Removed in aiR.** Server versions: 10.3, 2021.

The Invariant API is private and isn't available to custom apps in aiR. Code that calls the processing engine directly needs to move to the Processing API.

**Use instead:** Processing API

**Documentation:**
- [Deprecated functionality](https://platform.relativity.com/Server2022/Content/What_s_new/Deprecated_functionality.htm) (Server 2022)

### API-016: Object Query Manager and View Fields Manager APIs

**Replaced in aiR.** Server versions: 10.3.

The Object Query Manager and View Fields Manager APIs were deprecated and removed, and they aren't available in aiR. Queries should go through the Object Manager API, and the Object Manager migration guide shows how to convert the code.

**Use instead:** Object Manager API (query)

**Documentation:**
- [Deprecated functionality](https://platform.relativity.com/Server2022/Content/What_s_new/Deprecated_functionality.htm) (Server 2022)
- [Migrating code to the Object Manager API](https://platform.relativity.com/Server2026/Content/BD_Object_Manager/Migrating_code_to_the_Object_Manager_API.htm) (Server 2026)

### API-017: Legacy unversioned Imaging and Tab Manager methods

**Replaced in aiR.** Server versions: 10.3, 2021, 2022.

The unversioned Imaging and Tab Manager methods (for example SaveAsync on the imaging profile and imaging set managers, and GetAvailableObjectTypesAsync, GetAvailableParentsAsync, and GetDependencyList on Tab Manager) were replaced by versioned APIs. Move to the versioned Imaging and Tab Manager APIs, which use separate Create and Update methods and renamed Tab Manager methods.

**Use instead:** Versioned Imaging API and Tab Manager API

**Documentation:**
- [Deprecated functionality](https://platform.relativity.com/Server2022/Content/What_s_new/Deprecated_functionality.htm) (Server 2022)

### API-018: Integration Points kCura.* namespaces

**Replaced in aiR.** Server versions: 10.3.

Custom Integration Points providers built on the kCura.IntegrationPoints.* namespaces need to move to Relativity.IntegrationPoints.* and the current DLLs. Also, as of August 29, 2025 the legacy load file import and export workflows in Integration Points are no longer supported in aiR, so use the Import and Export APIs for that work.

**Use instead:** Relativity.IntegrationPoints.* namespaces and current DLLs

**Documentation:**
- [Release notes](https://platform.relativity.com/RelativityaiR/Content/What_s_new/Release_notes.htm) (Relativity aiR)
- [Deprecated functionality](https://platform.relativity.com/Server2022/Content/What_s_new/Deprecated_functionality.htm) (Server 2022)

### PAGE-002: Removed main-window JavaScript functions and hosted libraries

**Removed in aiR.** Server versions: all.

Many helper functions on Relativity's main window (RelativityAjax, PopWindow, GetApplicationPath, confirmYesNo, and others) and the libraries Relativity used to host (AngularJS, jQuery, jQuery UI, Knockout, and so on) aren't available to custom pages in aiR. Bundle any libraries your page needs, use window.relativity.* for values like getApplicationPath and getKeplerApplicationPath, and switch to native browser APIs like fetch. The Removals page lists every item with its replacement.

**Use instead:** Bundle libraries in the page, use window.relativity.* APIs and native browser APIs

**Documentation:**
- [Removals](https://platform.relativity.com/RelativityaiR/Content/Customizing_the_UI/Removals.htm) (Relativity aiR)
- [Custom Pages migration](https://platform.relativity.com/RelativityaiR/Content/Customizing_the_UI/Custom_Pages_migration.htm) (Relativity aiR)

### AGENT-002: Shipping assemblies that clash with platform-provided ones

**Restricted in aiR.** Server versions: all.

aiR supplies a number of assemblies itself, and an app that ships its own copy of one can break, sometimes only after a platform update. For example, the agent framework supports Polly 5.7.0 only and the docs say not to add it to your app, and the External Logging docs warn that dependencies that conflict with the platform's will break the app. Use the platform-provided version, or internalize the dependency so it can't clash.

**Use instead:** Use the platform-provided version, or internalize the dependency

**Documentation:**
- [Agent resiliency guidelines](https://platform.relativity.com/RelativityaiR/Content/Background_processing/Agent_resiliency_guidelines.htm) (Relativity aiR)
- [External Logging](https://platform.relativity.com/RelativityaiR/Content/Logging/External_Logging.htm) (Relativity aiR)

### API-019: Pre-2022 unversioned Kepler API namespaces and URLs

**Replaced in aiR.** Server versions: 10.3, 2021.

Apps built for Server 10.3 or 2021 that use the old unversioned Kepler namespaces and URLs need to move to the versioned contracts, for example Relativity.Environment.V1.Workspace and the relativity-environment/v1 REST paths. Relativity moved to versioned namespaces API by API starting with Server 2022, and aiR uses the versioned forms.

**Use instead:** The versioned (V1+) namespace and REST URL for each API

**Documentation:**
- [Release notes](https://platform.relativity.com/Server2026/Content/What_s_new/Release_notes.htm) (Server 2026)

### API-020: Object Manager request rate limits and data egress limits

**Restricted in aiR.** Server versions: all.

Object Manager in aiR limits each application to 1,000 requests per minute per web server and returns 429 Too Many Requests above that, per the Developer Changelog. The StreamLongText endpoint is also rate limited as of September 1, 2026. Pace your calls, retry after a 429, and use the Export Service API for bulk text instead of streaming long text one field at a time.

**Use instead:** Pace with RateLimit headers, retry on Retry-After, send X-Kepler-Referrer, use Export Service API for bulk text

**Documentation:**
- [Release notes](https://platform.relativity.com/RelativityaiR/Content/What_s_new/Release_notes.htm) (Relativity aiR)
- [Object Manager service](https://platform.relativity.com/RelativityaiR/Content/BD_Object_Manager/Object_Manager_service.htm) (Relativity aiR)

### DATA-007: Audit and login data read straight from SQL

**Replaced in aiR.** Server versions: all.

Use the Audit API for audit and login data in aiR. The login history library scripts (Login History by User Report and Environment-Level User Login) are retired, and Relativity points to Audit for similar data.

**Use instead:** Audit API (relativity-audit/v1)

**Documentation:**
- [Scripts retirement](https://help.relativity.com/RelativityaiR/Content/System_Guides/Scripts_retirement.htm) (Relativity aiR)

### API-021: ARM API V1, Relativity.ARM.SDK, and unversioned ARM URLs

**Replaced in aiR.** Server versions: all.

ARM REST API V3 is the version to build against in aiR, and Relativity publishes a V1 to V3 migration guide. Integrations still calling ARM V1 or unversioned ARM URLs should follow that guide and move to V3.

**Use instead:** ARM REST API V3

**Documentation:**
- [ARM API V3 migration guide](https://platform.relativity.com/RelativityaiR/Content/BD_ARM/ARM_API_V3_migration_guide.htm) (Relativity aiR)

### UI-001: Classic Viewer extensions

**Replaced in aiR.** Server versions: 10.3, 2021, 2022.

The Classic Viewer was deprecated in October 2021 and removed from Server 2023, so it isn't available in aiR. Viewer extensions built for it need to be rebuilt as Review Interface extensions using the Review API.

**Use instead:** Review Interface extension (Review API)

**Documentation:**
- [Platform Changes](https://platform.relativity.com/RelativityaiR/Content/What_s_new/Platform_Changes.htm) (Relativity aiR)
- [Release notes](https://platform.relativity.com/Server2026/Content/What_s_new/Release_notes.htm) (Server 2026)

