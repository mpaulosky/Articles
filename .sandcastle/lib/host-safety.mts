// Keeping what agents write from running on the host.
//
// Agents can write two things the host later touches: the issue branch's files
// (its worktree), and the repository's shared .git directory, which Sandcastle
// mounts read-write into every sandbox so agents can commit. Git runs code from
// both: hooks from core.hooksPath (.github/hooks, which resolves inside the
// worktree a command runs in), and commands named in .git/config
// (core.fsmonitor, credential helpers, core.sshCommand, aliases and more).
//
// A worktree also finds its repository through files agents can write: its
// .git file, and commondir in its directory under .git/worktrees. Pointed at a
// directory of the agent's making, they'd make git read a config the agent
// wrote, so a git command run in the worktree isn't safe.
//
// So:
// - the host never runs a git hook (withoutGitHooks);
// - the sandbox can't change .git/config or .git/hooks (protectedGitMounts);
// - the host runs git and gh only in the main checkout, whose .git directory
//   is the repository itself, never in a worktree (lib/build.mts);
// - Sandcastle does run git in a worktree, when it reuses one and when it
//   removes one, so the host checks the worktree still points at this
//   repository before either (worktreeLinkProblems), and leaves a worktree
//   that doesn't for a person to look at.
//
// What's left: pipelines run concurrently, so another issue's agent could
// change a worktree's links between that check and Sandcastle's git command.
// Closing that needs Sandcastle to mount the .git directory read-only but for
// what a commit writes.

import { execFileSync } from "node:child_process";
import { existsSync, lstatSync, readFileSync } from "node:fs";
import { basename, join, resolve } from "node:path";
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

// What's wrong with how the worktree at `worktreePath` finds its repository,
// whose shared .git directory is `commonDir`; empty when nothing is. Its .git
// must be a file naming its directory under <commonDir>/worktrees, that
// directory must be a real one whose commondir leads back to `commonDir`, and
// none of them may be a symlink.
export function worktreeLinkProblems(worktreePath: string, commonDir: string): string[] {
  const kind = (path: string) => {
    try {
      const stat = lstatSync(path);
      return stat.isSymbolicLink() ? "symlink" : stat.isDirectory() ? "directory" : stat.isFile() ? "file" : "other";
    } catch {
      return "missing";
    }
  };
  const expectKind = (path: string, wanted: string) =>
    kind(path) === wanted ? [] : [`${path} is ${kind(path) === "missing" ? "missing" : `a ${kind(path)}`}, not a ${wanted}`];

  const gitFile = join(worktreePath, ".git");
  const problems = [...expectKind(worktreePath, "directory"), ...expectKind(gitFile, "file")];
  if (problems.length > 0) return problems;

  const gitdir = /^gitdir: (.+)$/.exec(readFileSync(gitFile, "utf8").trim())?.[1];
  const worktreesDir = join(commonDir, "worktrees");
  if (!gitdir || resolve(worktreePath, gitdir) !== join(worktreesDir, basename(resolve(worktreePath, gitdir)))) {
    return [`${gitFile} doesn't point into ${worktreesDir}`];
  }
  const adminDir = resolve(worktreePath, gitdir);
  const commondirFile = join(adminDir, "commondir");
  problems.push(...expectKind(worktreesDir, "directory"), ...expectKind(adminDir, "directory"), ...expectKind(commondirFile, "file"));
  if (problems.length > 0) return problems;

  if (resolve(adminDir, readFileSync(commondirFile, "utf8").trim()) !== resolve(commonDir)) {
    problems.push(`${commondirFile} doesn't lead back to ${commonDir}`);
  }
  return problems;
}

let commonDir: string | undefined;

// This repository's shared .git directory, read once.
export function repoGitDir(): string {
  commonDir ??= gitCommonDir();
  return commonDir;
}

// The Docker sandbox every agent runs in, with .git/config and .git/hooks
// read-only.
export function sandbox() {
  return docker({ mounts: protectedGitMounts(repoGitDir()) });
}
