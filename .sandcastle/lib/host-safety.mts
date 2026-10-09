// Keeping what agents write from running on the host.
//
// Agents can write two things the host later touches: the issue branch's files
// (its worktree), and the repository's shared .git directory, which Sandcastle
// mounts read-write into every sandbox so agents can commit. Git runs code from
// both: hooks from core.hooksPath (.github/hooks, which resolves inside the
// worktree a command runs in), and commands named in .git/config
// (core.fsmonitor, credential helpers, core.sshCommand, aliases and more).
//
// Git also finds the config it reads through files agents can write: a
// commondir file, in .git itself or in a worktree's directory under
// .git/worktrees, and a worktree's .git file. Pointed at a directory of the
// agent's making, they'd make git read a config the agent wrote.
//
// So, from protectHostGit() on:
// - the host never runs a git hook (withoutGitHooks);
// - every host git command, Sandcastle's included, takes its config, refs and
//   objects from this repository's .git, whatever a commondir file says
//   (GIT_COMMON_DIR, set by hostGitEnv);
// - the sandbox can't change .git/config or .git/hooks (protectedGitMounts);
// - the host runs its own git and gh in the main checkout, never in a worktree
//   (lib/build.mts);
// - Sandcastle runs git in a worktree when it reuses one and when it removes
//   one, so the host first checks the worktree still points at this repository
//   (worktreeLinkProblems), and leaves one that doesn't for a person.
//
// What's left: a worktree's own files (HEAD, index) can still be redirected,
// which changes what git sees there but runs nothing, unless someone turned on
// extensions.worktreeConfig, which reads a config.worktree from there.

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

// The environment every host git command runs in: hooks off, and the shared
// .git directory pinned to `commonDir`. GIT_COMMON_DIR outranks any commondir
// file, so a planted one can't point git at a config an agent wrote.
export function hostGitEnv(env: NodeJS.ProcessEnv, commonDir: string): NodeJS.ProcessEnv {
  return { ...withoutGitHooks(env), GIT_COMMON_DIR: commonDir };
}

// The main checkout's .git directory, which is this repository's shared one.
// Found on disk rather than by asking git, since git would follow a planted
// commondir. Throws when it isn't a real directory, or has a commondir file:
// a main checkout never has one, so an agent planted it.
export function mainGitDir(checkout: string): string {
  const gitDir = resolve(checkout, ".git");
  const stat = lstatSync(gitDir, { throwIfNoEntry: false });
  if (!stat?.isDirectory()) {
    throw new Error(`${gitDir} isn't a directory. Run Sandcastle from the root of the main checkout.`);
  }
  if (existsSync(join(gitDir, "commondir"))) {
    throw new Error(`${gitDir}/commondir exists, so git would read another directory's config. Inspect it and remove it.`);
  }
  return gitDir;
}

let commonDir: string | undefined;

// This repository's shared .git directory, read once.
export function repoGitDir(): string {
  commonDir ??= mainGitDir(process.cwd());
  return commonDir;
}

// Protect this process and everything it starts (see hostGitEnv). Call before
// the first git command and before any agent runs.
export function protectHostGit(): void {
  Object.assign(process.env, hostGitEnv(process.env, repoGitDir()));
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

// The Docker sandbox every agent runs in, with .git/config and .git/hooks
// read-only.
export function sandbox() {
  return docker({ mounts: protectedGitMounts(repoGitDir()) });
}
