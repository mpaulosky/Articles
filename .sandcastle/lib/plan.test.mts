import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { pickedIssues } from "./plan.mts";

const ready = [
  { number: 3, title: "Three" },
  { number: 5, title: "Five" },
];

describe("pickedIssues", () => {
  it("keeps the planner's order", () => {
    assert.deepEqual(pickedIssues(["5", "3"], ready, () => {}).map((issue) => issue.number), [5, 3]);
  });

  it("picks an issue the planner lists twice only once", () => {
    assert.deepEqual(pickedIssues(["3", "5", "3"], ready, () => {}).map((issue) => issue.number), [3, 5]);
  });

  it("skips an id that isn't a ready issue, and says so", () => {
    const warnings: string[] = [];
    assert.deepEqual(pickedIssues(["9", "5"], ready, (line) => warnings.push(line)).map((issue) => issue.number), [5]);
    assert.equal(warnings.length, 1);
    assert.match(warnings[0]!, /Skipping 9/);
  });
});
