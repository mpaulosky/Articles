import assert from "node:assert/strict";
import { existsSync, readFileSync } from "node:fs";
import { describe, it } from "node:test";
import { CHECK_PATHS, GATE_COMMAND, GATE_SCRIPT } from "./lib/repo-config.mts";

const root = new URL("../", import.meta.url);
const read = (path: string) => readFileSync(new URL(path, root), "utf8");

// The repo's Docker-free sandbox gate is .sandcastle/sandbox-gate.sh: the repo-ci-baseline Template owns
// .sandcastle/check.sh for a different job, and Apply keeps a check.sh that GATE_COMMAND or GATE_SCRIPT names.
describe("the sandbox gate", () => {
  it("GATE_SCRIPT and GATE_COMMAND name .sandcastle/sandbox-gate.sh", () => {
    // Arrange
    const expected = ".sandcastle/sandbox-gate.sh";

    // Act
    const names = [GATE_SCRIPT, GATE_COMMAND];

    // Assert
    assert.deepEqual(names, [expected, expected]);
  });

  it("the gate script is committed, and it isn't the Template's Owned check.sh", () => {
    // Arrange
    const gate = new URL(GATE_SCRIPT, root);

    // Act
    const committed = existsSync(gate);

    // Assert
    // The next re-Apply writes the Template's own .sandcastle/check.sh beside this gate, so only the name is pinned.
    assert.ok(committed, `${GATE_SCRIPT} is missing`);
    assert.notEqual(GATE_SCRIPT, ".sandcastle/check.sh");
  });

  // A guard for later edits to CHECK_PATHS: the rename itself doesn't change what it covers.
  it("CHECK_PATHS covers the gate script, so a PR that changes it is listed for the reviewer", () => {
    // Arrange
    const covers = (path: string) => GATE_SCRIPT === path || GATE_SCRIPT.startsWith(path.endsWith("/") ? path : `${path}/`);

    // Act
    const covered = CHECK_PATHS.some(covers);

    // Assert
    assert.ok(covered, `no CHECK_PATHS entry covers ${GATE_SCRIPT}`);
  });
});

// The Owned role prompts say to run scripts/gate.sh, which needs Docker; the sandbox has none, so this
// repo-owned file tells every role what that command means here, and survives the next re-Apply.
describe("the role rules name this repo's sandbox gate", () => {
  it("shared-rules.md says a role prompt's scripts/gate.sh means .sandcastle/sandbox-gate.sh in the sandbox", () => {
    // Arrange
    const rules = read(".sandcastle/roles/shared-rules.md");

    // Act
    const says = rules.includes("in the sandbox that means `.sandcastle/sandbox-gate.sh`");

    // Assert
    assert.ok(says);
  });
});
