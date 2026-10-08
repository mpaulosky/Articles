import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { fenced, runCheck, tail } from "./check.mts";

// A sandbox whose exec answers each command from a table.
const sandboxWith = (results: Record<string, { stdout: string; exitCode: number }>) => ({
  exec: async (command: string) => {
    const result = results[command];
    if (!result) throw new Error(`unexpected command: ${command}`);
    return { ...result, stderr: "" };
  },
});

describe("runCheck", () => {
  it("passes a green check over a clean worktree, without colour codes", async () => {
    const result = await runCheck(
      sandboxWith({
        ".sandcastle/check.sh 2>&1": { stdout: "\u001b[32mok\u001b[0m\n", exitCode: 0 },
        "git status --porcelain 2>&1": { stdout: "", exitCode: 0 },
      }),
    );
    assert.deepEqual(result, { passed: true, output: "ok\n" });
  });

  it("fails a red check", async () => {
    const result = await runCheck(sandboxWith({ ".sandcastle/check.sh 2>&1": { stdout: "error CS1002", exitCode: 1 } }));
    assert.equal(result.passed, false);
    assert.equal(result.output, "error CS1002");
  });

  it("fails a green check over uncommitted changes", async () => {
    const result = await runCheck(
      sandboxWith({
        ".sandcastle/check.sh 2>&1": { stdout: "ok", exitCode: 0 },
        "git status --porcelain 2>&1": { stdout: " M src/Web/Program.cs\n", exitCode: 0 },
      }),
    );
    assert.equal(result.passed, false);
    assert.match(result.output, /uncommitted changes/);
  });
});

describe("tail", () => {
  it("keeps the last lines", () => {
    assert.equal(tail("a\nb\nc\n", 2), "b\nc");
  });
});

describe("fenced", () => {
  it("uses a fence longer than any backtick run inside", () => {
    assert.equal(fenced("x ```` y"), "`````text\nx ```` y\n`````");
    assert.equal(fenced("plain"), "```text\nplain\n```");
  });
});
