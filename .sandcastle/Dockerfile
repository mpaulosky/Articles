FROM node:22-bookworm

# Install system dependencies. libicu is for .NET, python3 for the test
# discovery .sandcastle/check.sh shares with CI. There's no GitHub CLI: the
# sandbox never talks to GitHub (see main.mts).
RUN apt-get update && apt-get install -y \
  git \
  curl \
  jq \
  libicu72 \
  python3 \
  && rm -rf /var/lib/apt/lists/*

# Build-args for UID/GID alignment: sandcastle docker build-image
# defaults these to the host user's UID/GID so image-built files
# and bind-mounted files share an owner without runtime chown.
ARG AGENT_UID=1000
ARG AGENT_GID=1000

# Rename the base image's "node" user to "agent" and align UID/GID.
RUN groupmod -o -g $AGENT_GID node && usermod -o -u $AGENT_UID -g $AGENT_GID -d /home/agent -m -l agent node

# Install the .NET SDK that global.json pins. It pins 10.0.401 with
# rollForward latestMinor, so the latest 10.0 SDK satisfies it. Installed
# system-wide and linked into /usr/local/bin, so dotnet is on PATH in a login
# shell too, which resets PATH.
RUN curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh \
  && bash /tmp/dotnet-install.sh --channel 10.0 --install-dir /usr/share/dotnet \
  && rm /tmp/dotnet-install.sh \
  && ln -s /usr/share/dotnet/dotnet /usr/local/bin/dotnet

ENV DOTNET_ROOT="/usr/share/dotnet" \
  DOTNET_CLI_TELEMETRY_OPTOUT=1 \
  DOTNET_NOLOGO=1

# pnpm through Corepack, at the version package.json's "packageManager" pins
# (the root one for Sandcastle, src/Web's for the Tailwind build).
RUN corepack enable pnpm
ENV COREPACK_ENABLE_DOWNLOAD_PROMPT=0
USER ${AGENT_UID}:${AGENT_GID}

# Install Claude Code CLI
RUN curl -fsSL https://claude.ai/install.sh | bash

# Add Claude and .NET global tools to PATH
ENV PATH="/home/agent/.local/bin:/home/agent/.dotnet/tools:$PATH"

WORKDIR /home/agent

# In worktree sandbox mode, Sandcastle bind-mounts the git worktree at /home/agent/workspace
# and overrides the working directory to /home/agent/workspace at container start.
# Structure your Dockerfile so that /home/agent/workspace can serve as the project root.
ENTRYPOINT ["sleep", "infinity"]
