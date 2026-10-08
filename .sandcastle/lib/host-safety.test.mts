import assert from "node:assert/strict";
import { execFileSync } from "node:child_process";
import { chmodSync, existsSync, mkdirSync, mkdtempSync, renameSync, rmSync, symlinkSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { describe, it } from "node:test";
import { protectedGitMounts, withoutGitHooks, worktreeLinkProblems } from "./host-safety.mts";

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

describe("worktreeLinkProblems", () => {
  const setup = () => {
    const root = mkdtempSync(join(tmpdir(), "sandcastle-link-"));
    const git = (cwd: string, ...args: string[]) =>
      execFileSync("git", ["-c", "user.name=t", "-c", "user.email=t@t", "-c", "core.hooksPath=/dev/null", ...args], { cwd, stdio: "ignore" });
    const repo = join(root, "repo");
    mkdirSync(repo);
    git(repo, "init", "--quiet");
    git(repo, "commit", "--quiet", "--allow-empty", "-m", "init");
    const worktree = join(repo, ".sandcastle", "worktrees", "feature-1-x");
    git(repo, "worktree", "add", "--quiet", "-b", "feature/1-x", worktree);
    return { root, repo, worktree, commonDir: join(repo, ".git") };
  };

  it("finds nothing wrong with a worktree git made", () => {
    const { root, worktree, commonDir } = setup();
    try {
      assert.deepEqual(worktreeLinkProblems(worktree, commonDir), []);
    } finally {
      rmSync(root, { recursive: true, force: true });
    }
  });

  it("catches a .git file pointed at a directory of the agent's making", () => {
    const { root, worktree, commonDir } = setup();
    try {
      mkdirSync(join(worktree, "evil"));
      writeFileSync(join(worktree, ".git"), `gitdir: ${join(worktree, "evil")}\n`);
      assert.match(worktreeLinkProblems(worktree, commonDir).join(), /doesn't point into/);
    } finally {
      rmSync(root, { recursive: true, force: true });
    }
  });

  it("catches a commondir that leads somewhere else", () => {
    const { root, worktree, commonDir } = setup();
    try {
      writeFileSync(join(commonDir, "worktrees", "feature-1-x", "commondir"), `${join(root, "evil")}\n`);
      assert.match(worktreeLinkProblems(worktree, commonDir).join(), /doesn't lead back/);
    } finally {
      rmSync(root, { recursive: true, force: true });
    }
  });

  it("catches a worktree directory swapped for a symlink", () => {
    const { root, worktree, commonDir } = setup();
    try {
      const admin = join(commonDir, "worktrees", "feature-1-x");
      renameSync(admin, `${admin}-moved`);
      symlinkSync(`${admin}-moved`, admin);
      assert.match(worktreeLinkProblems(worktree, commonDir).join(), /symlink/);
    } finally {
      rmSync(root, { recursive: true, force: true });
    }
  });
});
