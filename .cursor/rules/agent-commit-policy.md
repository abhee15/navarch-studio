# Agent Commit Policy

Purpose: Prevent agents from committing other agents' work. Each agent must only commit files they themselves modified for the current task.

Required behavior
- Stage only explicit file paths that you edited in this session.
- Never run broad staging commands like `git add .` or `git add -A`.
- Make one focused commit per cohesive change; do not batch unrelated edits.
- Use descriptive commit messages: `type(scope): summary`.
- Do not modify or commit files unrelated to the current task.

Pre-commit checks
- Run all project pre-commit checks relevant to modified areas (frontend/backend/infra).

Non-code files
- Documentation/analysis should go under `temp/` (ignored by git) unless explicitly requested to add a rule under `.cursor/rules/`.

Examples
- Good: `git add -- frontend/src/components/AppHeader.tsx frontend/src/pages/DashboardPage.tsx`
- Bad: `git add .`

