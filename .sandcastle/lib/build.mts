// Build one planned issue in a single sandbox: implement, merge main in, check,
// review, check again, and publish it as its own pull request. The branch is
// never merged into main locally and no issue is closed here: the PR says
// "Fixes #n", so the issue closes when the PR merges through the normal checks
// and review in docs/PROCESS.md.

import * as sandcastle from "@ai-hero/sandcastle";
import { commitsAhead } from "./branches.mts";
import { fenced, runCheck, tail } from "./check.mts";
import { BASE_BRANCH, CHECK_COMMENT_LINES, copyToWorktree, hooks, IMPLEMENTER_ITERATIONS, MODEL } from "./config.mts";
import { commentOnIssue, openPullRequest, type SandcastleIssue } from "./github.mts";
import { sandbox as dockerSandbox } from "./host-safety.mts";
import { issuePromptArgs } from "./prompts.mts";
import { prBody, prTitle } from "./publish.mts";
import { sh } from "./shell.mts";
import { parseVerdict } from "./verdict.mts";

// The parts of a sandbox buildIssue uses; tests pass a fake.
export type BuildSandbox = Pick<sandcastle.Sandbox, "run" | "exec" | "close" | "worktreePath">;

// What buildIssue needs from outside the pipeline; tests pass stubs.
export type BuildHost = {
  createSandbox(branch: string): Promise<BuildSandbox>;
  commitsAhead(worktreePath: string): number;
  commentOnIssue(issueNumber: number, body: string): void;
  // Push the branch from its worktree and open (or reuse) its pull request.
  publish(worktreePath: string, branch: string, title: string, body: string): string;
  log(line: string): void;
};

// Push from the worktree with git hooks off. The pre-push hook would run the
// branch's own .github/hooks/pre-push, scripts/gate.sh and test code: files the
// agents wrote, run on the host with its gh auth, Docker socket and home
// directory. The sandbox already ran .sandcastle/check.sh on this HEAD, and CI
// runs the full suite on the PR. main.mts turns hooks off for every host git
// command too (lib/host-safety.mts); this repeats it where it matters most.
function publish(worktreePath: string, branch: string, title: string, body: string): string {
  sh(worktreePath, "git", "-c", "core.hooksPath=/dev/null", "push", "--force-with-lease", "-u", "origin", branch);
  return openPullRequest(worktreePath, branch, title, body);
}

const liveHost: BuildHost = {
  createSandbox: (branch) =>
    sandcastle.createSandbox({ branch, baseBranch: BASE_BRANCH, sandbox: dockerSandbox(), hooks, copyToWorktree }),
  commitsAhead,
  commentOnIssue,
  publish,
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
    if (host.commitsAhead(sandbox.worktreePath) === 0) {
      log("nothing to publish");
      return { outcome: "nothing-to-publish" };
    }

    // Bring main in, so a branch that started on an older main (a reused
    // branch, or main moving during the build) is checked and reviewed as it
    // would merge, and its PR can merge. A conflict stops the issue: resolving
    // it is a person's call.
    const merge = await sandbox.exec(`git merge --no-edit ${BASE_BRANCH} 2>&1`);
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
      if (moved && !(await checkPasses("after the reviewer's changes"))) {
        return { outcome: "check-failed" };
      }
      verdict = parseVerdict(review.stdout);
    } catch (error) {
      verdict = { approved: false, summary: `The reviewer failed: ${error}` };
    }
    log(`reviewer ${verdict.approved ? "approved" : "rejected"}`);
    if (!verdict.approved) {
      return stop("rejected", `Sandcastle's reviewer rejected this issue's change, so ${notPushed}\n\n${verdict.summary}`);
    }

    // Publish while the worktree still exists; close() may remove it.
    try {
      const prUrl = host.publish(sandbox.worktreePath, branch, prTitle(issue), prBody(issue, verdict.summary));
      log(`published ${prUrl}`);
      return { outcome: "published", prUrl };
    } catch (error) {
      return stop("publish-failed", `Sandcastle couldn't publish \`${branch}\`: ${error}`);
    }
  } finally {
    await sandbox.close();
  }
}

// The sandbox's HEAD commit, or undefined when it can't be read; an unknown
// HEAD counts as moved, so the check runs again.
async function head(sandbox: Pick<BuildSandbox, "exec">): Promise<string | undefined> {
  const { stdout, exitCode } = await sandbox.exec("git rev-parse HEAD");
  return exitCode === 0 && stdout.trim() ? stdout.trim() : undefined;
}
