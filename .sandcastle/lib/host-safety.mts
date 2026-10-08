// Keeping what agents write from running on the host.
//
// Agents can write two things the host later touches: the issue branch's files
// (its worktree), and the repository's shared .git directory, which Sandcastle
// mounts read-write into every sandbox so agents can commit. Git runs code from
// both: hooks from core.hooksPath (.github/hooks, which resolves inside the
// worktree a command runs in), and commands named in .git/config
// (core.fsmonitor, credential helpers, core.sshCommand, aliases and more).
//
// So the host never runs a git hook, and the sandbox can't change .git/config
// or .git/hooks. The host still runs git in the worktree (push, rev-list) and
// in the main checkout (fetch), and Sandcastle runs git there too (worktree
// add and remove, a fast-forward of a reused worktree).

import { execFileSync } from "node:child_process";
import { existsSync } from "node:fs";
import { join } from "node:path";
import type { MountConfig } from "@ai-hero/sandcastle";
import { docker } from "@ai-hero/sandcastle/sandboxes/docker";

// The environment with core.hooksPath forced to /dev/null for every git command
// started from it, Sandcastle's own included, appended to any GIT_CONFIG_*
// entries already there. Environment config outranks the repository's, so
// neither a branch's .github/hooks nor a changed .git/config can turn hooks on.
export function withoutGitHooks(env: NodeJS.ProcessEnv): NodeJS.ProcessEnv {
  const count = Number(env.GIT_CONFIG_COUNT ?? "0");
  const index = Number.isInteger(count) && count > 0 ? count : 0;
  return {
    ...env,
    GIT_CONFIG_COUNT: String(index + 1),
    [`GIT_CONFIG_KEY_${index}`]: "core.hooksPath",
    [`GIT_CONFIG_VALUE_${index}`]: "/dev/null",
  };
}

// Turn git hooks off for this process and everything it starts. Call before
// the first git command.
export function disableHostGitHooks(): void {
  Object.assign(process.env, withoutGitHooks(process.env));
}

// Read-only mounts over the parts of the shared .git directory that make git
// run commands, laid over Sandcastle's read-write mount of the directory at the
// same path. Agents only need the objects, refs and index to commit. A path
// that doesn't exist is left out: Docker can't mount it.
export function protectedGitMounts(commonDir: string, exists: (path: string) => boolean = existsSync): MountConfig[] {
  return ["config", "hooks"]
    .map((name) => join(commonDir, name))
    .filter((path) => exists(path))
    .map((path) => ({ hostPath: path, sandboxPath: path, readonly: true }));
}

// The repository's shared .git directory, as an absolute path.
export function gitCommonDir(cwd: string = process.cwd()): string {
  return execFileSync("git", ["rev-parse", "--path-format=absolute", "--git-common-dir"], {
    cwd,
    encoding: "utf8",
    stdio: ["ignore", "pipe", "inherit"],
  }).trim();
}

let mounts: MountConfig[] | undefined;

// The Docker sandbox every agent runs in, with .git/config and .git/hooks
// read-only.
export function sandbox() {
  mounts ??= protectedGitMounts(gitCommonDir());
  return docker({ mounts });
}
