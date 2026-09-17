---
name: example-skill
description: Placeholder skill that ships with the scaffolding so the plugin installs to something non-empty. Replace this folder with your first real skill, or remove it once one exists.
---

# Example skill

This is a template, not a functioning skill. It exists so contributors have a working example of the
`SKILL.md` format and so the plugin has at least one skill to install while the catalog is still empty.

## Structure

```
skills/
└── your-skill-name/
    ├── SKILL.md          # required: frontmatter (name, description) + instructions
    └── reference.md      # optional: detail Claude only sometimes needs, linked from SKILL.md
```

## Writing the frontmatter

- `name`: matches the folder name.
- `description`: written for Claude, not a human reader. Lead with *when* to use the skill (the
  situations, trigger phrases, or file types that should invoke it), then briefly what it does.

## Writing the body

Instructions Claude follows when the skill runs. Keep `SKILL.md` itself focused; move long reference
material into a separate file in the same folder and link to it instead of inlining everything.
