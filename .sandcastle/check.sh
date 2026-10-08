#!/usr/bin/env bash
# The check Sandcastle's agents run before they finish, and the host runs in
# the sandbox before it publishes a branch: the exit code decides, never what
# an agent reports. It builds the solution and runs every test project but the
# ones that need Docker, which the sandbox doesn't have. The pre-push gate
# (scripts/gate.sh) runs those when the host pushes, and CI runs everything.
set -euo pipefail

cd "$(git rev-parse --show-toplevel)"

# TestContainers, the Aspire AppHost and Playwright all need Docker. A new
# test project that needs it fails here until it's added to this list.
needs_docker=(
  tests/AppHost.Tests/AppHost.Tests.csproj
  tests/Web.Integration.Tests/Web.Integration.Tests.csproj
  tests/Web.E2E.Tests/Web.E2E.Tests.csproj
)

echo "▶ Build"
mapfile -t solutions < <(find . -maxdepth 1 -name '*.slnx')
dotnet build "${solutions[@]}" --configuration Release -warnaserror

echo "▶ Tests (without Docker)"
mapfile -t projects < <(python3 .github/scripts/discover_tests.py --list | grep . || true)
for project in "${projects[@]}"; do
  skip=false
  for docker_project in "${needs_docker[@]}"; do
    [[ "$project" == "$docker_project" ]] && skip=true
  done
  if $skip; then
    echo "Skipping ${project}: it needs Docker."
    continue
  fi
  dotnet test "$project" --configuration Release
done

echo "▶ Sandcastle tests"
pnpm run test:sandcastle

echo "✅ Sandcastle check passed."
