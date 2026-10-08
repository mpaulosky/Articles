// Build one planned issue in a single sandbox: implement, check, review, check
// again, and publish it as its own pull request. Nothing is merged locally and
// no issue is closed here: the PR says "Fixes #n", so the issue closes when the
// PR merges through the normal checks and review in docs/PROCESS.md.

import * as sandcastle from "@ai-hero/sandcastle";
import { docker } from "@ai-hero/sandcastle/sandboxes/docker";
import { commitsAhead } from "./branches.mts";
import { fenced, runCheck, tail } from "./check.mts";
import { BASE_BRANCH, CHECK_COMMENT_LINES, copyToWorktree, hooks, IMPLEMENTER_ITERATIONS, MODEL } from "./config.mts";
import { commentOnIssue, openPullRequest, type SandcastleIssue } from "./github.mts";
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

// Push from the worktree: the pre-push hook gates the checkout's HEAD, and runs
// the full gate (with the Docker test suites the sandbox can't run) on the host.
function publish(worktreePath: string, branch: string, title: string, body: string): string {
  sh(worktreePath, "git", "push", "--force-with-lease", "-u", "origin", branch);
  return openPullRequest(worktreePath, branch, title, body);
}

const liveHost: BuildHost = {
  createSandbox: (branch) =>
    sandcastle.createSandbox({ branch, baseBranch: BASE_BRANCH, sandbox: docker(), hooks, copyToWorktree }),
  commitsAhead,
  commentOnIssue,
  publish,
  log: console.log,
};

export type BuildOutcome =
  | "published"
  | "nothing-to-publish"
  | "implementer-unfinished"
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
    let verdict;
    try {
      const review = await sandbox.run({
        name: "reviewer",
        agent: sandcastle.claudeCode(MODEL),
        maxIterations: 1,
        promptFile: "./.sandcastle/review-prompt.md",
        promptArgs,
      });
      if (review.commits.length > 0 && !(await checkPasses("after the reviewer's commits"))) {
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
