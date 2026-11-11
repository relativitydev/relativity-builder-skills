---
name: event-handler-builder
description: Use when someone wants to create, scaffold, deploy, attach, or troubleshoot a custom event handler for Relativity aiR, meaning code that runs when an object is saved, deleted, mass-deleted, or opened in a layout, when an application is installed or uninstalled, when a workspace is created from a template, or a custom mass operation or Relativity Forms page script. Picks the right handler type, produces a compiling .NET Framework class library from templates, and walks through upload and attaching the handler to an object type. Also use when a handler "doesn't fire".
---

# Relativity aiR Event Handler Builder

You are helping a developer build a custom **event handler** for Relativity aiR: a class Relativity calls when something happens to an object, a page, or an application.

**Relativity aiR only.** This covers the cloud platform, not Relativity Server. If the user is on Server, say so.

**What this skill does and does not do.** It produces a class library that compiles, starting from the templates in this skill. It does **not** deploy or attach anything. Uploading the DLL and attaching the handler are steps the user performs in Relativity; walk them through and never say a handler is live.

## Step 1: Pick the handler type

Choosing the wrong type is the most common reason a handler "doesn't fire". Ask what should trigger the code, then pick from this table. Each row has a template in `templates/`.

| Trigger | Base class | Template | Notes |
|---|---|---|---|
| Before a Document or RDO saves (validate, adjust values) | `PreSaveEventHandler` | `SamplePreSaveEventHandler.cs` | `Success = false` cancels the save and shows `Message` |
| After a Document or RDO saves | `PostSaveEventHandler` | `SamplePostSaveEventHandler.cs` | Cannot undo the save |
| Before one object is deleted (clean-up) | `PreDeleteEventHandler` | `SamplePreDeleteEventHandler.cs` | **Cannot block** the delete |
| Before an object with dependents is deleted (block it) | `PreCascadeDeleteEventHandler` | `SamplePreCascadeDeleteEventHandler.cs` | Block with `Success = false` **and** `Exception` set |
| Once before a set of objects is deleted | `PreMassDeleteEventHandler` | `SamplePreMassDeleteEventHandler.cs` | IDs are in a temp SQL table |
| Before a layout opens in View or Edit | `PreLoadEventHandler` | `SamplePreLoadEventHandler.cs` | Does **not** run for new objects under Relativity Forms |
| Change how a form looks or behaves in the browser | `PageInteractionEventHandler` + JavaScript | `SamplePageInteractionEventHandler.cs`, `sample-forms-script.js` | Relativity Forms; see Step 3 |
| A custom action in an object list's mass operations menu | `MassOperationHandler` | `SampleMassOperationHandler.cs` | Different base class and attribute; see Step 3 |
| After the application installs or upgrades | `PostInstallEventHandler` | `SamplePostInstallEventHandler.cs` | Seed data, create queue tables |
| Before the application installs | `PreInstallEventHandler` | (same shape as Post Install) | |
| Before the application is uninstalled | `PreUninstallEventHandler` | `SamplePreUninstallEventHandler.cs` | |
| A workspace is created from a template that has the app | `PostWorkspaceCreateEventHandlerBase` | `SamplePostWorkspaceCreateEventHandler.cs` | Only for template-based creation |

**Relativity Forms is the default.** Relativity's documentation says all objects now default to Relativity Forms; an older object type may still have **Use Relativity Forms** set to No. Under Relativity Forms, `ConsoleEventHandler` and `PageInteractionEventHandler.PopulateScriptBlocks()` are ignored, and script returned in a Pre Load `Response.Message` does not run. Do not build a Console handler; add console buttons from a Page Interaction script with the Forms API `CREATE_CONSOLE` event instead (the template script shows how). Console buttons only appear on **View** mode layouts, on full page loads.

**Documents are a special case.** Reviewers usually code documents in the Review Interface, not on a standalone layout page, so a console button or form script on the Document object may not appear where they work. Confirm with the user where the behavior needs to show up, and test it there.

**What triggers save handlers.** Saves in the UI fire Pre Save and Post Save. Through the Object Manager API, single `CreateAsync`/`UpdateAsync` calls fire them **only when the request passes a `CallingContext`** (unless the handler implements `ICanExecuteWithLimitedContext`, in which case `ActiveLayout` is null and `PageMode` is Unknown). Object Manager **mass create and mass update do not fire them**, and neither does `UpdateLongTextFromStreamAsync`. If the user expects a handler to enforce a rule on every write, say plainly which paths bypass it.

**Keep handlers fast.** Pre Save, Post Save, and Pre Load run while a user waits. Relativity's guidance is to move anything over about half a second to an agent: queue the work in the handler, process it in an agent (see the agent-builder skill).

## Step 2: Scaffold from the templates

Copy `templates/MyCompany.MyApp.EventHandlers.csproj` and the template file(s) for the chosen type into the user's solution. One project can hold many handlers.

1. Rename the namespace, project, and assembly from `MyCompany.MyApp` to the user's own, and rename the class.
2. Set the `Description` attribute to a clear name. It is what users pick from when attaching the handler, so prefix it with the application name.
3. **Give every handler class a new GUID** in its `Guid` attribute: replace the `00000000-0000-0000-0000-000000000000` with `[guid]::NewGuid()` (PowerShell) or `uuidgen` output. Never reuse one. **Field and object type GUIDs are different**: they must be the real GUIDs from the user's workspace, never generated. If the user doesn't have them yet, leave clearly named constants marked `TODO` and tell them how to get the values (below).
4. Build: `dotnet build -c Release`. It must build with no errors or warnings.

Keep these exactly as the templates have them:

- **`[kCura.EventHandler.CustomAttributes.Description("...")]` and `[System.Runtime.InteropServices.Guid("...")]`** on every handler. Fully qualify the Guid attribute: with `using System;`, a bare `[Guid(...)]` fails with `CS0616`.
- **Mass operation handlers are the exception:** Relativity reads their name from **`[System.ComponentModel.Description("...")]`**. With the kCura attribute, the mass operation registers with a blank name (confirmed on a live Relativity aiR instance).
- **`net462`**, packages `Relativity.EventHandler`, `Relativity.API`, `Relativity.Services.Interfaces.SDK` (Object Manager: `Relativity.Services.Objects`) and `Relativity.Kepler`, all with **`ExcludeAssets="runtime"`** so Relativity's own DLLs stay out of `bin` (Relativity's copy wins in event handlers anyway). The Object Manager package's DLLs (`Relativity.Services.Interfaces.dll`, `Relativity.Services.DataContracts.dll`) are on Relativity's list of assemblies it provides at runtime, so there is nothing extra to upload. Do not use the newer `Relativity.ObjectManager.SDK` (namespace `Relativity.ObjectManager.V1`) in server-side code: its DLLs are not on that list. Never use RSAPI (`kCura.Relativity.Client`).
- **Reference fields and object types by GUID**, not by name or Artifact ID. GUIDs are the same in every workspace the application is installed into; names and IDs are not. An object type, field, or choice gets a GUID when it is added to the application. To read it, query the field (Artifact Type ID 14), object type (25), or choice (7) with Object Manager; each result's `Guids` holds it. For a Document field, add the field to the application first so it has a GUID.

## Step 3: Write the handler

General rules for every type:

- Return a `Response` with `Success = true` on the happy path. On failure set `Success = false`, a user-readable `Message`, and `Exception`.
- `ActiveArtifact.Fields` contains only the fields **on the current layout plus those in `RequiredFields`**. List every field `Execute()` reads in `RequiredFields`, or it will be missing when the save comes from another layout or the API. To find a field by GUID, match `field.Guids.Contains(guid)` as the Pre Save template does.
- If one file uses both `kCura.EventHandler` and `Relativity.Services.Objects.DataContracts`, `Field` is ambiguous (`CS0104`). Write `kCura.EventHandler.Field` explicitly.
- Call APIs through `Helper.GetServicesManager().CreateProxy<T>(...)`. Use `ExecutionIdentity.CurrentUser` when the user's own permissions should apply, and `ExecutionIdentity.System` only for writes the user should not need rights for. `Execute()` is synchronous, so block with `.GetAwaiter().GetResult()`.
- Get the workspace with `Helper.GetActiveCaseID()` and the user with `Helper.GetAuthenticationManager().UserInfo` (`ActiveUser` is obsolete).
- **Avoid save loops.** An Object Manager update with a `CallingContext` to the object being saved fires its save handlers again. The Post Save template writes to a separate log object instead.
- Log with message templates: `logger.LogError(ex, "Failed for {ArtifactID}", id)`, never an interpolated string.

Type-specific notes:

- **Pre Delete** cannot stop a delete: `Success = false` is ignored. Use **Pre Cascade Delete** to block. `Commit()` and `Rollback()` run around Relativity's delete transaction so you can finalize or undo your own work; for Pre Cascade Delete, `Commit()` is never called.
- **Pre Cascade Delete and Pre Mass Delete**: the Artifact IDs being deleted are in the SQL table named by `TempTableNameWithParentArtifactsToDelete`. Pre Cascade Delete only runs when the objects have dependents.
- **Page Interaction (Relativity Forms)**: the C# class only names files. `ScriptFileNames` lists JavaScript files Relativity Forms loads and runs; `AdditionalHostedFileNames` lists CSS, images, or helper scripts that are hosted but not run. `PopulateScriptBlocks()` must return an empty `Response`. The script (`sample-forms-script.js`) must be **ES5.1** (no arrow functions, `let`/`const`, classes, or template literals) and be a single function taking `(eventNames, convenienceApi)` that returns a map of event name to handler. Get a hosted file's URL with `convenienceApi.utilities.getRelativityPageBaseWindow().GetRelativityApplicationWebResourceFile(appGuid, 'latest', fileName)`. For every event and helper, see the Relativity Forms API reference below; do not guess event names.
- **Mass Operation**: Relativity calls `ValidateSelection`, `ValidateLayout`, `PreMassOperation`, then `DoBatch` once per batch (the selected IDs are in `BatchIDs`; size comes from the `MassCustomBatchAmount` instance setting), then `PostMassOperation`. Check `IsCancelled()` in `DoBatch` and call `IncrementCount`. To change the batch, send **one** Object Manager mass update (`MassUpdateByObjectIdentifiersRequest` with the batch's IDs) rather than one update per ID; remember a mass update does not fire save handlers. `Helper` here is an `IMOHelper`, which also has `GetActiveCaseID()`. The `Description` is the label users see in the mass operations menu, so make it the action name (for example "Mark for QC") rather than an application-prefixed name.
- **Install handlers**: set `[RunOnce(true)]` for one-time setup and always set `[RunTarget(...)]` to one of `kCura.EventHandler.Helper.RunTargets.Workspace`, `.Instance`, or `.InstanceAndWorkspace`. Make `Execute()` idempotent: a failed install runs it again. `CurrentVersion` is null on first install. `Success = false` fails the install and shows `Message` on the install status page.

## Step 4: Deploy and attach (the user does this in Relativity)

1. Make sure the application is **published to the Application Library**, then in **Resource Files** upload the DLL from `bin/Release/net462` (and `.pdb` for readable stack traces), linked to the application. For Page Interaction handlers, also upload **each script and hosted file as its own resource file** on the same application, named exactly as in `ScriptFileNames` / `AdditionalHostedFileNames`. Never upload `Relativity.*.dll` or `kCura.*.dll`.
2. Relativity reads the handlers from the DLL on upload; nothing is registered by hand.
3. **Attach** the handler. This is the step people miss:
   - **Object handlers** (save, delete, load, page interaction): in the workspace, open **Object Type** for the target object, then its **Event Handlers** list, then **New**, and pick the handler by its Description. To add it to the application, attach it to the object first; it then shows in the application's **Event Handler** list.
   - **Mass operations**: the object type's **Mass Operations** list, or **Link** on the application's Mass Operations list.
   - **Install and uninstall handlers**: the application's **Install Event Handlers** list, which also sets their order. They are not attached to object types.
   - **Post Workspace Create**: the application's **Application Event Handlers** list.
   - Scripted alternative: the Event Handler Manager API (`Relativity.Extensibility.SDK`, `IEventHandlerManager.AttachAsync`). Its `objectTypeID` parameter is the object type's **Artifact ID**, not its Artifact Type ID.
4. **Updating:** upload the new DLL over the existing resource file. A replaced script file reaches every web server within about 5 seconds; hard-refresh the browser (Ctrl+F5) to see it.

## Step 5: Verify

- Trigger the event in the UI (save, delete, open the layout) and check the effect: the validation message, the follow-up object, the log entry.
- From the API, send a `CallingContext` on single creates and updates if save handlers must run, and read `EventHandlerStatuses` on the result to see each handler's `Success` and `Message`.

## Troubleshooting "it doesn't fire"

Work through these in order:

1. Is the handler **attached** to the right object type in this workspace (or to the application's install list for install handlers)?
2. Does the base class match the event? A Post Save handler never runs for a delete or a mass operation.
3. Did the change come through a path that skips handlers: Object Manager mass create/update, a single API call without `CallingContext`, or `UpdateLongTextFromStreamAsync`?
4. Pre Load for a **new** object under Relativity Forms never fires; handle `eventNames.REPLACE_GET_NEW_OBJECT_INSTANCE` in a Page Interaction script instead.
5. Console handler or `PopulateScriptBlocks` code: ignored under Relativity Forms.
6. A field reads as null: add it to `RequiredFields`.
7. Page Interaction script not running: is each file uploaded as a resource file on the same application, named exactly as in `ScriptFileNames`? Is the script ES5.1 with no syntax errors (check the browser console)? Hard refresh.

## Reference documentation

- Developing object type event handlers: https://platform.relativity.com/RelativityaiR/Content/Customizing_workflows/Developing_object_type_event_handlers.htm
- Best practices for event handlers: https://platform.relativity.com/RelativityaiR/Content/Customizing_workflows/Best_practices_for_event_handlers.htm
- Adding event handlers to applications or objects: https://platform.relativity.com/RelativityaiR/Content/Customizing_workflows/Adding_event_handlers_to_applications_or_objects.htm
- Application event handlers: https://platform.relativity.com/RelativityaiR/Content/Building_Relativity_applications/Developing_application_event_handlers.htm
- Mass operation handlers: https://platform.relativity.com/RelativityaiR/Content/Customizing_workflows/Developing_Mass_Operation_handlers.htm
- Implementing Relativity Forms event handlers: https://platform.relativity.com/RelativityaiR/Content/Relativity_Forms/Implementing_Relativity_Forms_event_handlers.htm
- Relativity Forms load pipeline (every load event, including `CREATE_CONSOLE` and `replaceGetNewObjectInstance`): https://platform.relativity.com/RelativityaiR/Content/FormsAPI/Load_Pipeline.htm
- Relativity Forms console API: https://platform.relativity.com/RelativityaiR/Content/FormsAPI/Console_Api.htm
- Relativity Forms API reference (index): https://platform.relativity.com/RelativityaiR/Content/Relativity_Forms/Relativity_Forms_API_Reference.htm
- Event Handler Manager API: https://platform.relativity.com/RelativityaiR/Content/BD_Extensibility/Event_Handler_Manager_API.htm
- Object Manager API (which calls fire which handlers): https://platform.relativity.com/RelativityaiR/Content/BD_Object_Manager/Object_Manager_API.htm

## Output style

- Confirm the handler type with the user before generating code if the trigger is at all ambiguous.
- Generate the files, build, and fix any error before showing the result. Show the build output.
- Then give the exact upload list and the attach step for that handler type.
