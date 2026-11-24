# relativity-builder-skills

AI skills for developers building on the Relativity platform, maintained by Relativity's Developer
Services team. This is the public counterpart to our internal skills catalog: things Claude (or another
AI coding assistant) should know how to do when you're working against Relativity's APIs and SDKs, so
you don't have to rediscover them yourself.

## Available skills

Just describe what you want in plain language and Claude picks the right skill. You can also call
one directly with the command shown. Each skill's folder has its own README with the details.

| I want to... | Skill | Direct command |
|---|---|---|
| Check whether my Relativity Server custom app will work in Relativity aiR | [air-readiness-scanner](skills/air-readiness-scanner/README.md) | `/relativity-builder-skills:air-readiness-scanner` |
| Turn an old SQL script into Relativity REST API calls | [sql-to-api-converter](skills/sql-to-api-converter/README.md) | `/relativity-builder-skills:sql-to-api-converter` |
| Build a background job that runs on a schedule or works through a queue | [agent-builder](skills/agent-builder/README.md) | `/relativity-builder-skills:agent-builder` |
| Run code when something is saved, deleted, or opened, or when an app is installed | [event-handler-builder](skills/event-handler-builder/README.md) | `/relativity-builder-skills:event-handler-builder` |
| Add my own REST endpoint to Relativity | [kepler-service-builder](skills/kepler-service-builder/README.md) | `/relativity-builder-skills:kepler-service-builder` |
| Build a custom page (HTML and JavaScript) and add it as a tab | [javascript-custom-page-builder](skills/javascript-custom-page-builder/README.md) | `/relativity-builder-skills:javascript-custom-page-builder` |

### Examples of what to ask

- "Is my app ready for Relativity aiR?" (point it at your source folder or GitHub link)
- "Convert this SQL script to API calls."
- "Create a Relativity agent that processes a queue every few minutes."
- "I need a pre-save event handler that blocks saves when a field is empty."
- "Scaffold a Kepler service with one GET and one POST endpoint."
- "Help me build my first custom page."

### Good to know

- **The builder skills create the code, but you deploy it.** They produce compiling projects (or a
  zip, for custom pages) and walk you through uploading them in Relativity.
- **They target Relativity aiR**, the cloud platform, not Relativity Server.
- **Treat generated output as a starting point.** Test it in a non-production environment first.

## Using the skills

How you consume this repo depends on your AI client.

### Claude Code

Skills here are distributed as a [plugin](https://docs.claude.com/en/docs/claude-code/plugins) via this
repo's marketplace.

#### One-time setup

In the Claude Code terminal, run:

```
/plugin marketplace add relativitydev/relativity-builder-skills
/plugin install relativity-builder-skills@relativity-builder-skills
```

From the Desktop app, you can do the same thing without the terminal: click the **+** button next to the
prompt box, open the **plugins** section, and add the marketplace/install the plugin from there.

That's it — the skills are now available. Claude will use them automatically when relevant, or you can
invoke one directly, e.g. `/relativity-builder-skills:some-skill`.

If a command you just installed or updated doesn't show up right away, run `/reload-plugins`. It works in
the Desktop app as well as the terminal, though you have to type it into the session yourself. If it warns
that reloading would re-read the conversation, rerun it as `/reload-plugins --force`. Restarting Claude
Code (fully quit and reopen the Desktop app, or start a new terminal session) also works if the reload
doesn't pick the change up.

#### Getting updates

- **Manual (works for everyone, no setup)**: run this whenever you want the latest skills —
  ```
  /plugin marketplace update relativity-builder-skills
  /plugin update relativity-builder-skills@relativity-builder-skills
  ```
- **Automatic (optional, one-time setup)**: Claude Code enables auto-update by default only for official
  Anthropic marketplaces. Third-party ones like this repo ship with it **disabled**, so turn it on with
  `/plugin` → **Marketplaces** → select `relativity-builder-skills` → **Enable auto-update**. Because this
  repo is public, that's the only setup step — unlike a private marketplace, background refreshes can
  fetch it with no credential.

  Once enabled, Claude Code checks for updates shortly after a session starts, with a random delay of up
  to ten minutes. The session you're in keeps the versions it loaded at launch, so a new skill shows up
  either when you run `/reload-plugins` or in your next session, not mid-turn.

  Whichever you skip, `/plugin marketplace update relativity-builder-skills` always works as the fallback.

#### Uninstalling

```
/plugin uninstall relativity-builder-skills@relativity-builder-skills
```

### Other AI clients

The plugin/marketplace mechanism above is specific to Claude Code. Until we add first-class support for
other clients, the best way to use these skills elsewhere is to pull down the repo and point your tool at
it directly:

```
git clone https://github.com/relativitydev/relativity-builder-skills.git
```

Each skill lives under `skills/<skill-name>/SKILL.md` as a self-contained Markdown file: frontmatter
(`name`, `description`) followed by instructions. Most AI coding assistants can be pointed at a specific
file, or have its contents pasted into context, and will follow it for the rest of the session. Check
your client's docs for how it prefers to ingest custom instructions or context files.

## License

See [LICENSE](LICENSE).
