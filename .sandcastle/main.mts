// Parallel Planner with Review: plan → build → review → pull request loop
//
//   Phase 1 (Plan):   The host reads the open Sandcastle issues with its own gh
//                     auth, keeping only issues and comments from the owner,
//                     members and collaborators, and holds back every issue
//                     that already has an open PR. A planner agent picks the
//                     ones that can be built in parallel. The host names each
//                     issue's branch (lib/branches.mts).
//   Phase 2 (Build):  For each issue, in its own sandbox (lib/build.mts): the
//                     implementer works the issue, main is merged in, the
//                     host runs .sandcastle/check.sh, a reviewer refines the
//                     change and returns an approve/reject verdict, and the
//                     host checks again if the reviewer moved HEAD. An
//                     approved branch is pushed, with git hooks off, and gets
//                     its own draft PR that fixes the issue;
//                     anything else gets a comment on the issue and isn't
//                     pushed. All pipelines run concurrently.
//
// Nothing is merged into main locally and no issue is closed here: each change
// reaches main through its PR and the checks in docs/PROCESS.md.
//
// The sandbox gets no GitHub token. Agents read the issue from their prompt,
// and every GitHub write (comments, pushes, PRs) is made by the host, in code.
//
// The loop repeats up to MAX_ITERATIONS times so that newly unblocked issues
// are picked up, and stops early when a round opens no pull request.
//
// Usage:
//   pnpm run sandcastle

import { existsSync, readFileSync } from "node:fs";
import * as sandcastle from "@ai-hero/sandcastle";
import { z } from "zod";
import { fetchMain, prepareBranches, withoutOpenPullRequests } from "./lib/branches.mts";
import { buildIssue } from "./lib/build.mts";
import { MAX_ITERATIONS, MODEL } from "./lib/config.mts";
import { listSandcastleIssues, openPullRequestBranches } from "./lib/github.mts";
import { protectHostGit, sandbox } from "./lib/host-safety.mts";
import { pickedIssues } from "./lib/plan.mts";
import { plannerPromptArgs } from "./lib/prompts.mts";
import { githubTokensIn } from "./lib/sandbox-env.mts";

const envFile = ".sandcastle/.env";
const leakedTokens = existsSync(envFile) ? githubTokensIn(readFileSync(envFile, "utf8")) : [];
if (leakedTokens.length > 0) {
  throw new Error(
    `${envFile} sets ${leakedTokens.join(" and ")}, which Sandcastle would pass into the sandbox. ` +
      "Remove it: the host uses its own gh auth, and agents must not reach GitHub.",
  );
}

// Every git command this process starts, Sandcastle's included, runs with hooks
// off and its config pinned to this repository's .git: agents can write hooks
// and files that point git elsewhere. See lib/host-safety.mts, which also keeps
// .git/config and .git/hooks read-only in every sandbox.
protectHostGit();

// The planner emits its plan as JSON inside <plan> tags; Output.object extracts
// and validates it against this schema. There's no branch field: the host
// names branches, so a re-plan can't move an issue's work to a new branch.
const planSchema = z.object({
  issues: z.array(z.object({ id: z.string(), title: z.string() })),
});

for (let iteration = 1; iteration <= MAX_ITERATIONS; iteration++) {
  console.log(`\n=== Iteration ${iteration}/${MAX_ITERATIONS} ===\n`);

  // -------------------------------------------------------------------------
  // Phase 1: Plan
  // -------------------------------------------------------------------------
  const { ready, inReview } = withoutOpenPullRequests(listSandcastleIssues(), openPullRequestBranches());
  for (const issue of inReview) {
    console.log(`  ⏸ #${issue.number} is held back: its pull request is open.`);
  }
  if (ready.length === 0) {
    console.log("No open Sandcastle issues ready to build. Exiting.");
    break;
  }

  const plan = await sandcastle.run({
    sandbox: sandbox(),
    name: "planner",
    // Structured output requires maxIterations: 1.
    maxIterations: 1,
    agent: sandcastle.claudeCode(MODEL),
    promptFile: "./.sandcastle/plan-prompt.md",
    promptArgs: plannerPromptArgs(ready),
    // Throws StructuredOutputError if the tag is missing, the JSON is
    // malformed, or validation fails, which aborts the loop.
    output: sandcastle.Output.object({ tag: "plan", schema: planSchema }),
  });

  // Each ready issue the planner picked, once (lib/plan.mts).
  const picks = pickedIssues(
    plan.output.issues.map(({ id }) => id),
    ready,
  );

  if (picks.length === 0) {
    console.log("No unblocked issues to work on. Exiting.");
    break;
  }

  // -------------------------------------------------------------------------
  // Phase 2: Build, review and publish
  // -------------------------------------------------------------------------
  fetchMain();
  const work = prepareBranches(picks);

  console.log(`Planning complete. ${work.length} issue(s) to build in parallel:`);
  for (const { issue, branch } of work) {
    console.log(`  #${issue.number}: ${issue.title} → ${branch}`);
  }

  // Promise.allSettled means one failing pipeline doesn't cancel the others.
  const settled = await Promise.allSettled(work.map(({ issue, branch }) => buildIssue(issue, branch)));

  const published: string[] = [];
  for (const [i, outcome] of settled.entries()) {
    const { issue, branch } = work[i]!;
    if (outcome.status === "rejected") {
      console.error(`  ✗ #${issue.number} (${branch}) failed: ${outcome.reason}`);
    } else if (outcome.value.prUrl) {
      published.push(`  #${issue.number} (${branch}) → ${outcome.value.prUrl}`);
    }
  }

  console.log(`\nRound complete. ${published.length} pull request(s):`);
  for (const line of published) console.log(line);

  if (published.length === 0) {
    // Nothing reached a PR, so the next plan would pick the same issues and
    // repeat the same round. Stop and let a person look at the issue comments.
    console.log("No pull requests opened this round. Stopping.");
    break;
  }
}

console.log("\nAll done.");
