import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { buildIssue, type BuildHost, type BuildSandbox } from "./build.mts";

const issue = { number: 7, title: "Add search", body: "Search articles.", labels: ["Sandcastle"], comments: [] };
const branch = "feature/7-add-search";

type RunResult = { completionSignal?: string; commits?: { sha: string }[]; stdout?: string } | Error;

// A pipeline with scripted agent runs and check results, recording what the
// host was asked to do.
function pipeline(options: {
  implementer?: RunResult;
  reviewer?: RunResult;
  checks?: boolean[];
  ahead?: number;
  publishError?: Error;
}) {
  const checks = [...(options.checks ?? [true, true])];
  const calls = { runs: [] as string[], comments: [] as string[], published: [] as { title: string; body: string }[], closed: false };
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
    publish: (_, __, title, body) => {
      if (options.publishError) throw options.publishError;
      calls.published.push({ title, body });
      return "https://github.com/o/r/pull/1";
    },
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

  it("doesn't check again when the reviewer commits nothing", async () => {
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
    const { run, calls } = pipeline({ publishError: new Error("pre-push gate failed") });
    assert.equal((await run()).outcome, "publish-failed");
    assert.match(calls.comments[0]!, /pre-push gate failed/);
    assert.ok(calls.closed);
  });
});
