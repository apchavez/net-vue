#!/usr/bin/env bash
# Runs backend/frontend build/test inside official images (.NET 9 SDK for the api,
# node:22 for the web), matching CI exactly, so nothing beyond Docker needs to
# be installed locally (no .NET/Node needed on the host).
# Usage: ./docker/dev.sh <api-test|web-test|web-build|shell>
set -euo pipefail
export MSYS_NO_PATHCONV=1
cd "$(dirname "$0")/.."
ROOT="$(pwd -W 2>/dev/null || pwd)"

DOTNET_IMAGE="mcr.microsoft.com/dotnet/sdk:9.0"
NODE_IMAGE="node:22"
CMD="${1:-api-test}"
DOCKER_TTY=""
[[ "$CMD" == "shell" ]] && DOCKER_TTY="-it"

run_api() {
  # -v docker.sock: integration tests use Testcontainers (Postgres), which needs
  # to talk to the host's Docker daemon to spin up its own throwaway container.
  docker run --rm ${DOCKER_TTY:-} \
    -v "$ROOT/api":/workspace -w /workspace \
    -v net-vue-nuget-cache:/root/.nuget/packages \
    -v /var/run/docker.sock:/var/run/docker.sock \
    "$DOTNET_IMAGE" bash -c "$1"
}

run_web() {
  docker run --rm ${DOCKER_TTY:-} \
    -v "$ROOT/web":/workspace -w /workspace \
    -v net-vue-npm-cache:/root/.npm \
    "$NODE_IMAGE" bash -c "$1"
}

case "$CMD" in
  api-test)
    run_api "dotnet restore ProductApi.sln && dotnet build ProductApi.sln --no-restore -c Release && \
              dotnet test tests/ProductApi.UnitTests --no-build -c Release && \
              dotnet test tests/ProductApi.IntegrationTests --no-build -c Release"
    ;;
  web-test)  run_web "npm ci && npm run lint && npm run format:check && npx vue-tsc -b && npm run test" ;;
  web-build) run_web "npm ci && npm run build" ;;
  shell)     run_api "bash" ;;
  *) echo "Usage: $0 {api-test|web-test|web-build|shell}" >&2; exit 1 ;;
esac
