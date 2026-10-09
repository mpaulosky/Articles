// Build one planned issue in a single sandbox: implement, merge main in, check,
// review, check again, and publish it as its own pull request. The branch is
// never merged into main locally and no issue is closed here: the PR says
// "Fixes #n", so the issue closes when the PR merges through the normal checks
// and review in docs/PROCESS.md.

import { existsSync } from "node:fs";
import { join } from "node:path";
import * as sandcastle from "@ai-hero/sandcastle";
import { commitsAhead } from "./branches.mts";
import { fenced, runCheck, tail } from "./check.mts";
import { BASE_BRANCH, CHECK_COMMENT_LINES, copyToWorktree, hooks, IMPLEMENTER_ITERATIONS, MODEL } from "./config.mts";
import { commentOnIssue, openPullRequest, type SandcastleIssue } from "./github.mts";
import { sandbox as dockerSandbox, repoGitDir, worktreeLinkProblems } from "./host-safety.mts";
import { issuePromptArgs } from "./prompts.mts";
import { prBody, prTitle } from "./publish.mts";
import { sh } from "./shell.mts";
import { parseVerdict } from "./verdict.mts";

// The parts of a sandbox buildIssue uses; tests pass a fake.
export type BuildSandbox = Pick<sandcastle.Sandbox, "run" | "exec" | "close" | "worktreePath">;

// What buildIssue needs from outside the pipeline; tests pass stubs.
export type BuildHost = {
  createSandbox(branch: string): Promise<BuildSandbox>;
  commitsAhead(branch: string): number;
  commentOnIssue(issueNumber: number, body: string): void;
  // Push the checked commit to the branch and open (or reuse) its pull request.
  publish(branch: string, commit: string, title: string, body: string): string;
  // What's wrong with how the worktree finds its repository (see
  // lib/host-safety.mts); empty when nothing is.
  worktreeProblems(worktreePath: string): string[];
  log(line: string): void;
};

// Push the commit the check passed on, from the main checkout with git hooks
// off. Pushing from the worktree would run its pre-push hook: the branch's own
// .github/hooks/pre-push, scripts/gate.sh and test code, files the agents
// wrote, on the host with its gh auth, Docker socket and home directory. And
// git in the worktree finds its repository through files agents can write. The
// sandbox already ran .sandcastle/check.sh on this commit, and CI runs the full
// suite on the PR. main.mts turns hooks off for every host git command too
// (lib/host-safety.mts); this repeats it where it matters most.
function publish(branch: string, commit: string, title: string, body: string): string {
  sh(
    process.cwd(),
    "git", "-c", "core.hooksPath=/dev/null", "push", `--force-with-lease=refs/heads/${branch}`,
    "origin", `${commit}:refs/heads/${branch}`,
  );
  return openPullRequest(process.cwd(), branch, title, body);
}

// Where Sandcastle puts a branch's worktree (its create() in worktree mode).
const worktreePathFor = (branch: string) => join(process.cwd(), ".sandcastle", "worktrees", branch.replaceAll("/", "-"));

const worktreeProblems = (worktreePath: string) => worktreeLinkProblems(worktreePath, repoGitDir());

const liveHost: BuildHost = {
  // Sandcastle reuses a branch's worktree that's still there, running git in
  // it first, so check that one before handing it over.
  createSandbox: (branch) => {
    const existing = worktreePathFor(branch);
    const problems = existsSync(existing) ? worktreeProblems(existing) : [];
    if (problems.length > 0) {
      return Promise.reject(new Error(`the worktree left at ${existing} was tampered with: ${problems.join("; ")}`));
    }
    return sandcastle.createSandbox({ branch, baseBranch: BASE_BRANCH, sandbox: dockerSandbox(), hooks, copyToWorktree });
  },
  commitsAhead,
  commentOnIssue,
  publish,
  worktreeProblems,
  log: console.log,
};

export type BuildOutcome =
  | "published"
  | "nothing-to-publish"
  | "implementer-unfinished"
  | "merge-conflict"
  | "check-failed"
  | "rejected"
  | "publish-failed";

export async function buildIssue(
  issue: SandcastleIssue,
  branch: string,
  host: BuildHost = liveHost,
): Promise<{ outcome: BuildOutcome; prUrl?: string }> {
  const log = (line: string) => host.log(`  #${issue.number} ${line}`);
  const stop = (outcome: BuildOutcome, comment: string) => {
    log(`stopped: ${outcome}`);
    host.commentOnIssue(issue.number, comment);
    return { outcome };
  };
  const notPushed = `\`${branch}\` wasn't pushed; the local branch keeps its commits.`;
  const promptArgs = issuePromptArgs(issue, branch);

  const sandbox = await host.createSandbox(branch);
  try {
    // Implement. A run that throws or uses up its iterations without
    // signalling completion stops the issue for this round.
    let finished: boolean;
    let failure = "it ran out of iterations unfinished";
    try {
      const implement = await sandbox.run({
        name: "implementer",
        agent: sandcastle.claudeCode(MODEL),
        maxIterations: IMPLEMENTER_ITERATIONS,
        promptFile: "./.sandcastle/implement-prompt.md",
        promptArgs,
      });
      finished = implement.completionSignal !== undefined;
    } catch (error) {
      finished = false;
      failure = `it failed: ${error}`;
    }
    if (!finished) return stop("implementer-unfinished", `Sandcastle stopped building this issue: the implementer ${failure}. ${notPushed}`);

    // Gate, review and publish whenever the branch holds work main doesn't,
    // not only when this run added commits: a re-run of a finished issue
    // makes none, and its earlier work still needs a PR.
    if (host.commitsAhead(branch) === 0) {
      log("nothing to publish");
      return { outcome: "nothing-to-publish" };
    }

    // Bring main in, so a branch that started on an older main (a reused
    // branch, or main moving during the build) is checked and reviewed as it
    // would merge, and its PR can merge. A conflict stops the issue: resolving
    // it is a person's call.
    const merge = await sandbox.exec(`git merge --no-edit -m "chore: Merge ${BASE_BRANCH}" -m "Brings the branch up to date with ${BASE_BRANCH} before it's checked." ${BASE_BRANCH} 2>&1`);
    if (merge.exitCode !== 0) {
      await sandbox.exec("git merge --abort");
      return stop(
        "merge-conflict",
        `Sandcastle stopped building this issue: merging \`${BASE_BRANCH}\` into \`${branch}\` failed. ${notPushed}\n\n` +
          `The last ${CHECK_COMMENT_LINES} lines of its output:\n\n${fenced(tail(merge.stdout, CHECK_COMMENT_LINES))}`,
      );
    }

    const checkPasses = async (when: string) => {
      const check = await runCheck(sandbox);
      log(`check ${when}: ${check.passed ? "passed" : "failed"}`);
      if (check.passed) return true;
      host.commentOnIssue(
        issue.number,
        `Sandcastle stopped building this issue: \`.sandcastle/check.sh\` failed ${when}. ${notPushed}\n\n` +
          `The last ${CHECK_COMMENT_LINES} lines of its output:\n\n${fenced(tail(check.output, CHECK_COMMENT_LINES))}`,
      );
      return false;
    };

    if (!(await checkPasses("after the implementer"))) return { outcome: "check-failed" };

    // Review. The reviewer may commit refinements, and must end with a
    // verdict; anything but an approval keeps the branch from being published.
    // The check runs again whenever HEAD moved, not only on new commits: a
    // reviewer could reset or rebase to a HEAD the check never saw.
    const headBefore = await head(sandbox);
    let checkedHead = headBefore;
    let verdict;
    try {
      const review = await sandbox.run({
        name: "reviewer",
        agent: sandcastle.claudeCode(MODEL),
        maxIterations: 1,
        promptFile: "./.sandcastle/review-prompt.md",
        promptArgs,
      });
      const headAfter = await head(sandbox);
      const moved = review.commits.length > 0 || headBefore === undefined || headAfter !== headBefore;
      if (moved) {
        if (!(await checkPasses("after the reviewer's changes"))) return { outcome: "check-failed" };
        checkedHead = headAfter;
      }
      verdict = parseVerdict(review.stdout);
    } catch (error) {
      verdict = { approved: false, summary: `The reviewer failed: ${error}` };
    }
    log(`reviewer ${verdict.approved ? "approved" : "rejected"}`);
    if (!verdict.approved) {
      return stop("rejected", `Sandcastle's reviewer rejected this issue's change, so ${notPushed}\n\n${verdict.summary}`);
    }

    // Publish the commit the check passed on.
    if (checkedHead === undefined) {
      return stop("publish-failed", `Sandcastle couldn't read the checked commit of \`${branch}\`, so ${notPushed}`);
    }
    try {
      const prUrl = host.publish(branch, checkedHead, prTitle(issue), prBody(issue, verdict.summary));
      log(`published ${prUrl}`);
      return { outcome: "published", prUrl };
    } catch (error) {
      return stop("publish-failed", `Sandcastle couldn't publish \`${branch}\`: ${error}`);
    }
  } finally {
    // Sandcastle's close() runs git in the worktree; leave one that no longer
    // points at this repository, with its sandbox, for a person to look at.
    const problems = host.worktreeProblems(sandbox.worktreePath);
    if (problems.length > 0) {
      log(`left ${sandbox.worktreePath} and its sandbox in place: ${problems.join("; ")}`);
      host.commentOnIssue(
        issue.number,
        `Sandcastle stopped: the worktree for \`${branch}\` no longer points at this repository, so it was left for a person to look at. ` +
          "Don't run git in it.",
      );
    } else {
      await sandbox.close();
    }
  }
}

// The sandbox's HEAD commit, or undefined when it can't be read; an unknown
// HEAD counts as moved, so the check runs again.
async function head(sandbox: Pick<BuildSandbox, "exec">): Promise<string | undefined> {
  const { stdout, exitCode } = await sandbox.exec("git rev-parse HEAD");
  const commit = stdout.trim();
  return exitCode === 0 && /^[0-9a-f]{40}(?:[0-9a-f]{24})?$/.test(commit) ? commit : undefined;
}
