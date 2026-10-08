import assert from "node:assert/strict";
import { execFileSync } from "node:child_process";
import { chmodSync, existsSync, mkdirSync, mkdtempSync, rmSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { describe, it } from "node:test";
import { protectedGitMounts, withoutGitHooks } from "./host-safety.mts";

describe("withoutGitHooks", () => {
  it("forces core.hooksPath to /dev/null through the environment", () => {
    const env = withoutGitHooks({ PATH: "/bin" });
    assert.equal(env.PATH, "/bin");
    assert.equal(env.GIT_CONFIG_COUNT, "1");
    assert.equal(env.GIT_CONFIG_KEY_0, "core.hooksPath");
    assert.equal(env.GIT_CONFIG_VALUE_0, "/dev/null");
  });

  it("appends to GIT_CONFIG_* entries already set", () => {
    const env = withoutGitHooks({ GIT_CONFIG_COUNT: "1", GIT_CONFIG_KEY_0: "color.ui", GIT_CONFIG_VALUE_0: "never" });
    assert.equal(env.GIT_CONFIG_COUNT, "2");
    assert.equal(env.GIT_CONFIG_KEY_0, "color.ui");
    assert.equal(env.GIT_CONFIG_KEY_1, "core.hooksPath");
    assert.equal(env.GIT_CONFIG_VALUE_1, "/dev/null");
  });

  it("keeps a branch's hooks from running, though the repository's config points at them", () => {
    const repo = mkdtempSync(join(tmpdir(), "sandcastle-hooks-"));
    try {
      const git = (env: NodeJS.ProcessEnv, ...args: string[]) =>
        execFileSync("git", ["-c", "user.name=t", "-c", "user.email=t@t", ...args], { cwd: repo, env, stdio: "ignore" });
      git(process.env, "init", "--quiet");
      git(process.env, "config", "core.hooksPath", ".github/hooks");
      mkdirSync(join(repo, ".github/hooks"), { recursive: true });
      const ran = join(repo, "hook-ran");
      writeFileSync(join(repo, ".github/hooks/pre-commit"), `#!/bin/sh\ntouch "${ran}"\n`);
      chmodSync(join(repo, ".github/hooks/pre-commit"), 0o755);

      git(withoutGitHooks(process.env), "commit", "--quiet", "--allow-empty", "-m", "guarded");
      assert.equal(existsSync(ran), false, "the hook ran despite withoutGitHooks");

      // The control: without the override, git does run it.
      git(process.env, "commit", "--quiet", "--allow-empty", "-m", "unguarded");
      assert.equal(existsSync(ran), true);
    } finally {
      rmSync(repo, { recursive: true, force: true });
    }
  });
});

describe("protectedGitMounts", () => {
  it("mounts .git/config and .git/hooks read-only at their own paths", () => {
    assert.deepEqual(protectedGitMounts("/repo/.git", () => true), [
      { hostPath: "/repo/.git/config", sandboxPath: "/repo/.git/config", readonly: true },
      { hostPath: "/repo/.git/hooks", sandboxPath: "/repo/.git/hooks", readonly: true },
    ]);
  });

  it("leaves out a path that doesn't exist", () => {
    assert.deepEqual(
      protectedGitMounts("/repo/.git", (path) => path.endsWith("config")).map((mount) => mount.hostPath),
      ["/repo/.git/config"],
    );
  });
});
