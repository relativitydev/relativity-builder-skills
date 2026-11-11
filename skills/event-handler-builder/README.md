# event-handler-builder

Builds custom event handlers for Relativity aiR: code that runs when an object is saved,
deleted, or opened, when an application is installed or uninstalled, when a workspace is
created from a template, or as a custom mass operation or Relativity Forms page script.

> **Relativity aiR only.** Not for Relativity Server.

## What you get

A .NET Framework 4.6.2 class library with a template for each handler type:

| Template | Handler |
|---|---|
| `SamplePreSaveEventHandler.cs` | Validate before save (can cancel the save) |
| `SamplePostSaveEventHandler.cs` | Follow-up work after save |
| `SamplePreDeleteEventHandler.cs` | Clean-up before delete |
| `SamplePreCascadeDeleteEventHandler.cs` | Block deleting objects that have dependents |
| `SamplePreMassDeleteEventHandler.cs` | One-time work before a mass delete |
| `SamplePreLoadEventHandler.cs` | Prepare a layout before it opens |
| `SamplePageInteractionEventHandler.cs` + `sample-forms-script.js` | Relativity Forms browser script |
| `SampleMassOperationHandler.cs` | Custom mass operation |
| `SamplePostInstallEventHandler.cs` / `SamplePreUninstallEventHandler.cs` | Application install lifecycle |
| `SamplePostWorkspaceCreateEventHandler.cs` | New workspace from a template |

The skill starts by working out which of these you actually need. Picking the wrong type is the
most common reason a handler "doesn't fire".

**It does not deploy or attach anything for you.** It builds the DLL and walks you through
uploading it and attaching each handler where it belongs.

## What it saves you from

- Console handlers and `PopulateScriptBlocks`, which Relativity Forms (the default) ignores
- Expecting a Pre Delete handler to block a delete, or a Pre Load handler to run for new objects
- Expecting save handlers to run for Object Manager mass updates or calls without a `CallingContext`
- Fields missing from `ActiveArtifact.Fields` because they weren't in `RequiredFields`
- A mass operation that registers with a blank name because it used the wrong `Description` attribute
- Save handlers that trigger themselves in a loop

## Using it

```
/relativity-builder-skills:event-handler-builder
```

See the [repository README](../../README.md) for installation, and
[`SKILL.md`](SKILL.md) for the instructions themselves.
