// The pull request the host opens for a finished issue. Its title and body
// follow docs/PROCESS.md, so the required PR title check passes and the
// description reads like any other.

import type { SandcastleIssue } from "./github.mts";

const conventionalTitle = /^(feat|fix|docs|style|refactor|perf|test|build|ci|chore|revert)(\([^)]+\))?!?: \S/;

// The issue title when it's already in commit format, otherwise the title
// behind `fix: ` for a bug or `feat: ` for anything else. The summary starts
// with a capital and has no closing period, as scripts/check-pr-title.sh wants.
export function prTitle(issue: Pick<SandcastleIssue, "title" | "labels">): string {
  const title = issue.title.trim().replace(/\s+/g, " ");
  const match = conventionalTitle.exec(title);
  const prefix = match ? title.slice(0, match[0].length - 1) : `${issue.labels.includes("bug") ? "fix" : "feat"}: `;
  const summary = (match ? title.slice(match[0].length - 1) : title).replace(/\.+$/, "") || "Resolve the issue";
  return `${prefix}${summary.charAt(0).toUpperCase()}${summary.slice(1)}`;
}

export function prBody(issue: Pick<SandcastleIssue, "number" | "title">, reviewSummary: string): string {
  return [
    "## Why",
    "",
    `Issue #${issue.number}: ${issue.title}`,
    "",
    "## What changed",
    "",
    "Sandcastle's implementer built the change in a sandbox, and its reviewer approved it:",
    "",
    ...reviewSummary.split("\n").map((line) => `> ${line}`),
    "",
    "## Verification",
    "",
    "- `.sandcastle/check.sh` (the solution build and the test projects that don't need Docker) passed in the sandbox.",
    "- The pre-push gate and CI run the full suite.",
    "",
    `Fixes #${issue.number}`,
  ].join("\n");
}
