import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { describe, it } from "node:test";
import { issuePromptArgs, plannerPromptArgs } from "./prompts.mts";

const issue = { number: 5, title: "Add search", body: "", labels: ["Sandcastle"], comments: ["One.", "Two."] };

// The {{KEY}} placeholders a prompt file uses.
const placeholders = (file: string) =>
  new Set([...readFileSync(new URL(`../${file}`, import.meta.url), "utf8").matchAll(/\{\{\s*([A-Za-z_]\w*)\s*\}\}/g)].map((m) => m[1]!));

describe("issuePromptArgs", () => {
  it("fills in an empty body and joins the comments", () => {
    const args = issuePromptArgs(issue, "feature/5-add-search");
    assert.equal(args.ISSUE_BODY, "(no description)");
    assert.equal(args.ISSUE_COMMENTS, "One.\n\n---\n\nTwo.");
    assert.equal(args.BASE_BRANCH, "origin/main");
  });

  it("never sets a built-in Sandcastle argument, which Sandcastle refuses", () => {
    const args = issuePromptArgs(issue, "feature/5-add-search");
    assert.ok(!("TARGET_BRANCH" in args));
    assert.ok(!("SOURCE_BRANCH" in args));
  });

  // Sandcastle fails a run whose prompt names an argument it wasn't given.
  for (const file of ["implement-prompt.md", "review-prompt.md"]) {
    it(`supplies every placeholder ${file} uses`, () => {
      const args = issuePromptArgs(issue, "feature/5-add-search");
      for (const key of placeholders(file)) assert.ok(key in args, `${file} uses {{${key}}}`);
    });
  }

  it("supplies every placeholder plan-prompt.md uses", () => {
    const args = plannerPromptArgs([issue]);
    for (const key of placeholders("plan-prompt.md")) assert.ok(key in args, `plan-prompt.md uses {{${key}}}`);
  });
});

describe("prompts", () => {
  it("never ask an agent to use gh or npm", () => {
    for (const file of ["implement-prompt.md", "review-prompt.md", "plan-prompt.md"]) {
      const text = readFileSync(new URL(`../${file}`, import.meta.url), "utf8");
      assert.doesNotMatch(text, /`gh |!`gh|npm run/, file);
    }
  });
});

describe("implement-prompt.md", () => {
  it("asks for commits in the repository's format, not a RALPH: prefix", () => {
    const text = readFileSync(new URL("../implement-prompt.md", import.meta.url), "utf8");
    assert.doesNotMatch(text, /RALPH/);
    assert.match(text, /git-commit-instructions\.md/);
  });
});
