# Contributing a skill

1. Clone this repo and create a branch.
2. Add a folder under `skills/` named for your skill, containing a `SKILL.md`:

   ```
   skills/
   └── your-skill-name/
       └── SKILL.md
   ```

3. Give it frontmatter with at least `name` and `description` — the description is what Claude reads to
   decide when to use the skill, so be specific about the situation it applies to:

   ```yaml
   ---
   name: your-skill-name
   description: What this skill does and when Claude should use it.
   ---

   Instructions Claude follows when the skill runs.
   ```

4. Test it locally before opening a PR:

   ```
   claude --plugin-dir /path/to/your/clone/relativity-builder-skills
   ```

   Confirm your skill shows up under `/help` → Custom commands, and that invoking it does what you expect.

5. Open a PR. Once merged, everyone with the plugin installed gets the change on their next background
   refresh or manual `/plugin marketplace update` + `/plugin update` (see
   [Getting updates](README.md#getting-updates) in the README).

   Don't add a `version` field to `.claude-plugin/plugin.json`. Its absence is what makes every merge to
   `main` count as the latest version automatically, with nothing to bump and no merge conflicts between
   concurrent PRs — this is the documented behavior for git-sourced marketplaces: with no `version`
   declared, Claude Code uses the resolved commit SHA as the update signal. Declaring one would *pin*
   the plugin, so installs would keep their cached copy until someone remembered to bump the field.

## This repo is public

Everything merged here is visible to anyone on the internet, including other Relativity customers and
competitors. Before opening a PR, double check that a skill contains no:

- Customer data, ticket contents, or anything specific to a real support case
- Internal-only tool names, endpoints, credentials, or Slack/Jira/Asana references
- Assumptions that only hold inside Relativity's internal environment

If a skill is genuinely useful but has internal-only bits mixed in, generalize it or leave it in the
internal [developer-services-plugins](https://github.com/relativityone/developer-services-plugins) repo
instead.

## Guidelines

- Keep `SKILL.md` focused — move detailed reference material Claude only sometimes needs into a separate
  file in the same folder (e.g. `reference.md`) and link to it, rather than inlining everything.
- Write the `description` for Claude's benefit, not a human reader's: lead with when to use the skill,
  not what it's called.
- If a skill should only ever be run deliberately (side effects, irreversible actions) rather than
  triggered automatically, add `disable-model-invocation: true` to its frontmatter.
