# Git workflow

## Repository values

Verified at inspection:

- Remote `origin`: `https://github.com/nguyenvanhoi-se/Brotato-Game.git`
- Current branch: `main`
- User-requested primary branch: `main`

History and tree can change; inspect `git remote -v`, `git branch --show-current`, and `git status --short` before acting.

## Default sequence for an authorized Git task

```text
git status
↓
(confirm clean working tree and intended branch)
↓
git pull origin main
↓
work
↓
git status
↓
git add <intended paths>
↓
git commit
↓
git push origin main
```

This is a workflow description, not blanket permission to sync, stage, commit, or publish. Follow the requested delivery mode. Never stage unrelated files.

## Dirty-tree handling

If status shows changes before work:

- Inspect enough diff/status to determine whether task files are already modified.
- Do not overwrite, revert, reset, clean, stash, stage, commit, or pull over changes on your own.
- Limit edits to new/unmodified requested files where possible. Tell the user about conflicts before any action that would overwrite their work.
- Recheck status after editing and distinguish pre-existing changes from files created for the task.

Working-tree state changes over time; always use the current `git status` and diff instead of relying on a prior snapshot or report.

## Prohibited without explicit request

Do not run `git reset --hard`, `git push --force`, `git clean -fd`, delete branches, rewrite history, or equivalent destructive commands unless the user explicitly requests that action. A normal feature request is not authorization.

Do not silently resolve conflicts by discarding one side. Explain the conflict and preserve both sides or ask for direction when necessary.
