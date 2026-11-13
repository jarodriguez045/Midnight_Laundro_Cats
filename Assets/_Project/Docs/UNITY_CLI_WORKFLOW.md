# Unity CLI workflow (Midnight Laundro-Cats)

Editor: **6000.4.6f1** (Unity 6). CLI binary (Hub):  
`/Applications/Unity Hub.app/Contents/Resources/unity`

If `unity` is missing from PATH in a normal terminal, either open a Unity Hub terminal or install the CLI:

```bash
curl -fsSL https://unity.com/install.sh | bash
```

Project-local agent skill (installed for Cursor): `.cursor/skills/unity-cli/`.

## Daily commands

```bash
# Prefer JSON when scripting / agents parse output
unity --no-banner --format json status
unity --no-banner editors
unity --no-banner open .
unity --no-banner status --until-ready --project-path .

# Search commands (do not grep --help)
unity commands --grep recompile
unity commands --grep test

# After C# edits with Editor open
unity recompile --project-path .

# EditMode / PlayMode tests
unity test --project-path .

# Live Editor control (requires Pipeline package)
unity pipeline install --project-path .
unity command --project-path .
unity command editor.playmode start --project-path .
```

Pin the project whenever more than one Editor may be open:

```bash
export UNITY_PROJECT_PATH="/Users/jarodriguez/Desktop/Our_Games/Dads_Games/Midnight_Laundro_Cats"
```

## Rules for agents

1. Prefer `unity command` / CLI over hand-editing `.unity` / `.prefab` when an Editor is ready.
2. If `unity status` shows Safe Mode, fix compile errors first — Pipeline will not connect until then.
3. Sandboxed agent shells can hide a running Editor; ask whether Unity is open before assuming it is down.
4. Do not install catalog plugins without asking (`unity plugin install …`).
5. Git remote for this project is already GitHub: `origin` → `https://github.com/jarodriguez045/Midnight_Laundro_Cats.git`. Use normal `git` (or `unity vcs` helpers); do not create a second remote.

## Git hygiene

- Ignore `Library/`, `Temp/`, `Logs/`, `UserSettings/` (see root `.gitignore`).
- Large binaries use Git LFS via `.gitattributes`.
- Optional Unity worktrees with shared Library cache: `unity vcs git worktree add <branch> --output-dir <path>`.

## Next setup step (once)

With the project open in the Editor (or via CLI), install Pipeline so agents can drive scenes live:

```bash
unity pipeline install --project-path .
```
