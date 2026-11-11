# agent-builder

Builds a custom agent for Relativity aiR: a class that Relativity runs in the background on an
interval, for queued, scheduled, or long-running work. Optionally adds a Workload Discovery
endpoint so the agent can run as often as every minute and scale with its queue.

> **Relativity aiR only.** Not for Relativity Server.

## What you get

- `templates/Agents/`: a .NET Framework 4.6.2 class library with a sample agent that processes a
  bounded batch per run, stops cleanly when Relativity asks it to, and logs correctly.
- `templates/WorkloadDiscovery/`: the Kepler endpoint Relativity polls to decide how many agent
  processes to run.

Every Relativity package comes from nuget.org and is excluded from the build output, so
everything in `bin/Release/net462` is exactly the set of files to upload as resource files.

**It does not deploy anything for you.** The skill builds the DLLs and walks you through
uploading them and checking the agent runs.

## What it saves you from

- `CS0616: 'Guid' is not an attribute class` from a bare `[Guid]` attribute
- Targeting .NET 6/8, or packages that only exist on private feeds
- Uploading Relativity's own DLLs alongside yours
- Agents that never return, hold state in memory, or ignore a stop request
- A Workload Discovery path with a leading slash, the wrong GUID, or a slow response
- Looking for a "New Agent" button or an interval setting that Relativity aiR doesn't have

## Using it

Installed via the plugin, Claude picks this up when you describe agent work. To invoke it
directly:

```
/relativity-builder-skills:agent-builder
```

See the [repository README](../../README.md) for installation, and
[`SKILL.md`](SKILL.md) for the instructions themselves.
