---
name: "Review Changes"
description: "Review current worktree changes, validate approved files, and commit only those changes."
agent: "agent"
---

Review all current worktree changes. Do not include unrelated changes. Run focused tests, stage the approved files, and commit with an appropriate message.

Mode selection:

- Normal mode is the default and follows the workflow below.
- If the user requests a dry run using wording such as `dry run` or
  `--dry-run`, do not stage, commit, amend, push, or modify any files. In
  dry-run mode, follow this workflow instead:
  1. Inspect `git status --short`, the current branch, and the complete
     unstaged and staged diffs without changing anything.
  2. Treat existing user changes as protected. Do not reset, checkout, clean,
     amend, or otherwise modify unrelated files. Do not include this prompt
     file in the review or the proposed staging set.
  3. Identify the files and behavior that would be in scope. If the intended
     scope is unclear, report that clarification is required and stop.
  4. Review the in-scope diff for correctness, regressions, security concerns,
     and missing focused tests. Keep unrelated changes untouched.
  5. Run the narrowest relevant tests, build, lint, or other validation that
     is safe in read-only dry-run mode. Do not claim validation that was not
     run. Report failures without making changes.
  6. Report exactly what would be staged, the commit message that would be
     used, the validation commands and results, and what would remain
     uncommitted. Do not stage, commit, amend, push, or modify files.

Follow this workflow:

1. Inspect `git status --short`, the current branch, and the complete unstaged and staged diffs before changing anything.
2. Treat existing user changes as protected. Do not reset, checkout, clean, amend, or otherwise modify unrelated files. Do not include this prompt file in the review or commit.
3. Identify the files and behavior in scope. If the intended scope is unclear, ask for clarification before staging or committing.
4. Review the in-scope diff for correctness, regressions, security concerns, and missing focused tests. Keep unrelated changes untouched.
5. Run the narrowest relevant tests, build, lint, or other validation for the approved changes. Do not claim validation that was not run. If validation fails, report the failure and do not commit unless the user explicitly approves proceeding.
6. Stage only the approved in-scope files by path. Recheck the staged diff and confirm that no unrelated files or the prompt itself are staged.
7. Create one concise commit with an appropriate conventional message. Do not amend an existing commit or push changes.
8. Report the commit hash and message, the files committed, the validation commands and results, and any worktree changes that remain uncommitted.
