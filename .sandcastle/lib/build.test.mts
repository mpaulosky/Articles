import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { buildIssue, type BuildHost, type BuildSandbox } from "./build.mts";

const issue = { number: 7, title: "Add search", body: "Search articles.", labels: ["Sandcastle"], comments: [] };
const branch = "feature/7-add-search";
const commitA = "a".repeat(40);
const commitB = "b".repeat(40);

type RunResult = { completionSignal?: string; commits?: { sha: string }[]; stdout?: string } | Error;

// A pipeline with scripted agent runs and check results, recording what the
// host was asked to do.
function pipeline(options: {
  implementer?: RunResult;
  reviewer?: RunResult;
  checks?: boolean[];
  ahead?: number;
  publishError?: Error;
  // What the host finds wrong with the worktree's links (nothing by default).
  worktreeProblems?: string[];
  // Exit code of `git merge` (0 by default).
  mergeExit?: number;
  // What `git rev-parse HEAD` prints, in order (the same commit by default).
  heads?: string[];
}) {
  const checks = [...(options.checks ?? [true, true])];
  const heads = [...(options.heads ?? [])];
  const calls = {
    runs: [] as string[],
    comments: [] as string[],
    published: [] as { branch: string; commit: string; title: string; body: string }[],
    execs: [] as string[],
    closed: false,
  };
  const results: Record<string, RunResult> = {
    implementer: options.implementer ?? { completionSignal: "<promise>COMPLETE</promise>", commits: [{ sha: "a" }] },
    reviewer: options.reviewer ?? { stdout: '<verdict>{"approved": true, "summary": "Clean."}</verdict>', commits: [] },
  };

  const sandbox = {
    worktreePath: "/tmp/worktree",
    run: async (opts: { name?: string }) => {
      calls.runs.push(opts.name!);
      const result = results[opts.name!]!;
      if (result instanceof Error) throw result;
      return { iterations: [], stdout: "", commits: [], ...result };
    },
    exec: async (command: string) => {
      calls.execs.push(command);
      if (command.startsWith("git merge --no-edit")) {
        const exitCode = options.mergeExit ?? 0;
        return { stdout: exitCode === 0 ? "Already up to date." : "CONFLICT (content): Merge conflict in src/A.cs", stderr: "", exitCode };
      }
      if (command === "git rev-parse HEAD") {
        return { stdout: `${heads.length > 0 ? heads.shift() : commitA}\n`, stderr: "", exitCode: 0 };
      }
      if (command.startsWith(".sandcastle/check.sh")) {
        const passed = checks.shift();
        if (passed === undefined) throw new Error("check ran more often than scripted");
        return { stdout: passed ? "ok" : "error CS1002", stderr: "", exitCode: passed ? 0 : 1 };
      }
      return { stdout: "", stderr: "", exitCode: 0 };
    },
    close: async () => {
      calls.closed = true;
      return {};
    },
  } as unknown as BuildSandbox;

  const host: BuildHost = {
    createSandbox: async () => sandbox,
    commitsAhead: () => options.ahead ?? 1,
    commentOnIssue: (_, body) => calls.comments.push(body),
    publish: (branch, commit, title, body) => {
      if (options.publishError) throw options.publishError;
      calls.published.push({ branch, commit, title, body });
      return "https://github.com/o/r/pull/1";
    },
    worktreeProblems: () => options.worktreeProblems ?? [],
    log: () => {},
  };

  return { run: () => buildIssue(issue, branch, host), calls, checksLeft: checks };
}

describe("buildIssue", () => {
  it("publishes an approved, checked branch as a PR that fixes the issue", async () => {
    const { run, calls } = pipeline({});
    const result = await run();
    assert.deepEqual(result, { outcome: "published", prUrl: "https://github.com/o/r/pull/1" });
    assert.deepEqual(calls.runs, ["implementer", "reviewer"]);
    assert.equal(calls.published[0]!.title, "feat: Add search");
    assert.match(calls.published[0]!.body, /Fixes #7/);
    assert.deepEqual(calls.comments, []);
    assert.ok(calls.closed);
  });

  it("doesn't publish when the reviewer rejects, and says why on the issue", async () => {
    const { run, calls } = pipeline({
      reviewer: { stdout: '<verdict>{"approved": false, "summary": "No test for an empty query."}</verdict>' },
    });
    assert.equal((await run()).outcome, "rejected");
    assert.deepEqual(calls.published, []);
    assert.match(calls.comments[0]!, /No test for an empty query/);
  });

  it("treats a review without a verdict as a rejection", async () => {
    const { run, calls } = pipeline({ reviewer: { stdout: "<promise>COMPLETE</promise>" } });
    assert.equal((await run()).outcome, "rejected");
    assert.deepEqual(calls.published, []);
  });

  it("treats a reviewer that throws as a rejection", async () => {
    const { run, calls } = pipeline({ reviewer: new Error("sandbox crashed") });
    assert.equal((await run()).outcome, "rejected");
    assert.match(calls.comments[0]!, /sandbox crashed/);
  });

  it("stops when the implementer doesn't finish", async () => {
    const { run, calls } = pipeline({ implementer: { commits: [{ sha: "a" }] } });
    assert.equal((await run()).outcome, "implementer-unfinished");
    assert.deepEqual(calls.runs, ["implementer"]);
    assert.match(calls.comments[0]!, /ran out of iterations/);
  });

  it("stops when the check fails after the implementer", async () => {
    const { run, calls } = pipeline({ checks: [false] });
    assert.equal((await run()).outcome, "check-failed");
    assert.deepEqual(calls.runs, ["implementer"]);
    assert.match(calls.comments[0]!, /error CS1002/);
  });

  it("checks again after the reviewer commits, and stops when that fails", async () => {
    const { run, calls } = pipeline({
      reviewer: { stdout: '<verdict>{"approved": true, "summary": "Tidied."}</verdict>', commits: [{ sha: "b" }] },
      checks: [true, false],
    });
    assert.equal((await run()).outcome, "check-failed");
    assert.deepEqual(calls.published, []);
  });

  it("checks again when the reviewer moves HEAD without committing", async () => {
    const { run, calls, checksLeft } = pipeline({ heads: [commitA, commitB], checks: [true, false] });
    assert.equal((await run()).outcome, "check-failed");
    assert.equal(checksLeft.length, 0);
    assert.deepEqual(calls.published, []);
  });

  it("merges main in before the first check", async () => {
    const { run, calls } = pipeline({});
    assert.equal((await run()).outcome, "published");
    const merge = calls.execs.findIndex((command) => command.startsWith("git merge --no-edit"));
    const check = calls.execs.findIndex((command) => command.startsWith(".sandcastle/check.sh"));
    assert.ok(merge !== -1 && merge < check, calls.execs.join("\n"));
    // In the repository's commit format, so the reviewer doesn't hold it against the change.
    assert.match(calls.execs[merge]!, /-m "chore: Merge origin\/main into feature\/7-add-search" origin\/main/);
  });

  it("stops on a conflict merging main, aborts the merge and says why", async () => {
    const { run, calls, checksLeft } = pipeline({ mergeExit: 1 });
    assert.equal((await run()).outcome, "merge-conflict");
    assert.ok(calls.execs.includes("git merge --abort"));
    assert.match(calls.comments[0]!, /Merge conflict in src\/A\.cs/);
    assert.equal(checksLeft.length, 2);
    assert.deepEqual(calls.published, []);
  });

  it("publishes the commit the check passed on", async () => {
    const { run, calls } = pipeline({
      reviewer: { stdout: '<verdict>{"approved": true, "summary": "Tidied."}</verdict>', commits: [{ sha: "b" }] },
      heads: [commitA, commitB],
      checks: [true, true],
    });
    assert.equal((await run()).outcome, "published");
    assert.equal(calls.published[0]!.commit, commitB);
    assert.equal(calls.published[0]!.branch, branch);
  });

  it("doesn't publish when the checked commit can't be read", async () => {
    const { run, calls } = pipeline({ heads: ["not a commit", "not a commit"], checks: [true, true] });
    assert.equal((await run()).outcome, "publish-failed");
    assert.deepEqual(calls.published, []);
  });

  it("leaves a worktree that no longer points at the repository unclosed, and says so", async () => {
    const { run, calls } = pipeline({ worktreeProblems: ["/w/.git doesn't point into /r/.git/worktrees"] });
    await run();
    assert.equal(calls.closed, false);
    assert.match(calls.comments.at(-1)!, /no longer points at this repository/);
  });

  it("doesn't check again when the reviewer leaves HEAD where it was", async () => {
    const { run, checksLeft } = pipeline({ checks: [true, true] });
    assert.equal((await run()).outcome, "published");
    assert.equal(checksLeft.length, 1);
  });

  it("publishes nothing when the branch has no work main lacks", async () => {
    const { run, calls } = pipeline({ ahead: 0 });
    assert.equal((await run()).outcome, "nothing-to-publish");
    assert.deepEqual(calls.runs, ["implementer"]);
    assert.deepEqual(calls.comments, []);
  });

  it("reports a failed push on the issue", async () => {
    const { run, calls } = pipeline({ publishError: new Error("push rejected") });
    assert.equal((await run()).outcome, "publish-failed");
    assert.match(calls.comments[0]!, /push rejected/);
    assert.ok(calls.closed);
  });
});
