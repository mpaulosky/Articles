#!/usr/bin/env bash
# Repo-specific CI setup, called by ci.yml after it restores and before it builds.
#
#   prepare.sh build              in the "Build Solution" job
#   prepare.sh test <test-name>   in each "Tests: <test-name>" matrix job
#
# ci.yml is Owned by the repo-ci-baseline Template and is overwritten on every
# Apply; this file is Seed, so it belongs to the repo. Put what only this repo
# needs here: tools the build runs (for example `corepack enable` for pnpm),
# images to pull or projects to publish before a test project runs. To pass
# environment variables to the later steps, append NAME=value lines to
# "$GITHUB_ENV".
#
# The test name comes from a file name in the PR, so treat it as data: quote
# it and never eval it.
set -euo pipefail

job="${1:?usage: prepare.sh build|test [test-name]}"
test_name="${2:-}"

# Building src/Web runs pnpm (Tailwind CSS), and every test project builds it
# too. Corepack provides the pnpm version pinned by "packageManager" in its
# package.json.
enable_pnpm() {
  export COREPACK_ENABLE_DOWNLOAD_PROMPT=0
  corepack enable
  (cd src/Web && pnpm --version)
}

# Sandcastle's orchestration code (.sandcastle/): type-check it and run its
# tests when the PR changes it or the root package files, as the local gate's
# .github/ci/gate-checks.sh does. Without an origin/main to compare with, run them.
sandcastle_tests() {
  local base
  if base="$(git merge-base HEAD origin/main 2>/dev/null)" \
    && git diff --quiet --no-renames "$base" HEAD -- .sandcastle package.json pnpm-lock.yaml pnpm-workspace.yaml; then
    echo "No Sandcastle or root package changes to test."
    return
  fi
  pnpm install --frozen-lockfile
  pnpm run test:sandcastle
}

case "$job" in
  build) enable_pnpm; sandcastle_tests ;;
  test) : "$test_name"; enable_pnpm ;;
  *) echo "prepare.sh: unknown job '$job'" >&2; exit 2 ;;
esac
