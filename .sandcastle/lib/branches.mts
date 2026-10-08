// Branch naming and fetching. Branch names come from code, never from a model,
// so re-planning an issue always lands on the branch that holds its earlier
// work, and every name passes scripts/check-branch-name.sh.

import { sh } from "./shell.mts";

const maxSlugLength = 50;

// The issue-branch prefixes from docs/PROCESS.md. Sandcastle creates feature/
// and fix/ branches; a hotfix/ branch someone made by hand is still reused.
const issuePrefixes = ["feature", "fix", "hotfix"] as const;

// The parts of an issue its branch name depends on.
export type BranchIssue = { number: number; title: string; labels: string[] };

// The issue title as a branch slug: no conventional-commit prefix, lower case,
// apostrophes dropped so "haven't" stays one word, and runs of ASCII letters
// and digits joined with "-", cut to 50 characters at a "-" boundary.
export function slugFor(title: string): string {
  const words = title
    .replace(/^\s*[a-z]+(?:\([^)]*\))?!?:\s*/i, "")
    .toLowerCase()
    .replace(/['’]/g, "")
    .match(/[a-z0-9]+/g) ?? [];
  const slug = words.join("-");
  if (slug.length === 0) return "issue";
  if (slug.length <= maxSlugLength) return slug;

  const cut = slug.slice(0, maxSlugLength + 1);
  const boundary = cut.lastIndexOf("-");
  return boundary > 0 ? cut.slice(0, boundary) : slug.slice(0, maxSlugLength);
}

// Whether a branch is the issue's: feature/{n}-{slug}, fix/{n}-{slug} or
// hotfix/{n}-{slug}, with a slug scripts/check-branch-name.sh accepts. Only such
// a name is reused, because the branch name reaches a shell: review-prompt.md
// passes {{BRANCH}} to the `git diff` it runs in the sandbox.
export function isIssueBranch(branch: string, issueNumber: number): boolean {
  return new RegExp(`^(?:${issuePrefixes.join("|")})/${issueNumber}-[a-z0-9]+(?:-[a-z0-9]+)*$`).test(branch);
}

// The issue's branch: its existing feature/, fix/ or hotfix/{n}-* branch when
// there is one, even if the title or labels have changed since, so earlier
// work is built on rather than redone. Otherwise fix/{n}-{slug} for a bug and
// feature/{n}-{slug} for everything else.
export function branchFor(issue: BranchIssue, existingBranches: readonly string[]): string {
  const existing = existingBranches
    .filter((branch) => isIssueBranch(branch, issue.number))
    .sort()[0];
  if (existing) return existing;

  const prefix = issue.labels.includes("bug") ? "fix" : "feature";
  return `${prefix}/${issue.number}-${slugFor(issue.title)}`;
}

// Hold back every issue that already has an open pull request from one of its
// branches: its work is waiting for review, and building it again would only
// pile commits onto that PR.
export function withoutOpenPullRequests<T extends BranchIssue>(
  issues: readonly T[],
  openPrBranches: readonly string[],
): { ready: T[]; inReview: T[] } {
  const ready: T[] = [];
  const inReview: T[] = [];
  for (const issue of issues) {
    (openPrBranches.some((branch) => isIssueBranch(branch, issue.number)) ? inReview : ready).push(issue);
  }
  return { ready, inReview };
}

// Branch names from `git ls-remote --heads` output, without refs/heads/.
export function parseHeads(lsRemote: string): string[] {
  return lsRemote
    .split("\n")
    .filter(Boolean)
    .map((line) => line.split("\t")[1]!.replace(/^refs\/heads\//, ""));
}

// The remote git operations prepareBranches needs; tests pass a stub.
export type RemoteGit = {
  // The issue branches on origin.
  issueBranches(): string[];
  // Fetch origin's branch into its remote-tracking ref.
  fetch(branch: string): void;
};

const originGit: RemoteGit = {
  issueBranches: () =>
    parseHeads(
      sh(process.cwd(), "git", "ls-remote", "--heads", "origin", ...issuePrefixes.map((prefix) => `refs/heads/${prefix}/*`)),
    ),
  fetch: (branch) => {
    sh(process.cwd(), "git", "fetch", "--quiet", "origin", `+refs/heads/${branch}:refs/remotes/origin/${branch}`);
  },
};

// Refresh origin/main, the base of every new issue branch and of every diff
// the reviewer reads. Done once per round, before the pipelines start, because
// concurrent fetches would contend on the same ref lock.
export function fetchMain(): void {
  sh(process.cwd(), "git", "fetch", "--quiet", "origin", "main");
}

// Count the commits on the worktree's branch that origin/main doesn't have.
export function commitsAhead(worktreePath: string): number {
  return Number(sh(worktreePath, "git", "rev-list", "--count", "origin/main..HEAD"));
}

// Name each issue's branch, and fetch the ones that already exist on origin,
// so the sandbox starts from the work already pushed rather than from main.
export function prepareBranches<T extends BranchIssue>(issues: readonly T[], git: RemoteGit = originGit): { issue: T; branch: string }[] {
  const remoteBranches = git.issueBranches();
  return issues.map((issue) => {
    const branch = branchFor(issue, remoteBranches);
    if (remoteBranches.includes(branch)) git.fetch(branch);
    return { issue, branch };
  });
}
