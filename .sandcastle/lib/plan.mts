// What the host takes from the planner's answer.

// The ready issues the planner picked, in its order, each once. An id that
// isn't a ready issue (hallucinated or stale) is skipped with a warning, so the
// planner can't start work on an issue that wasn't offered. A repeated id is
// dropped: two pipelines on one branch would race each other's sandboxes and
// pushes.
export function pickedIssues<T extends { number: number }>(
  ids: readonly string[],
  ready: readonly T[],
  warn: (line: string) => void = console.warn,
): T[] {
  const picked = new Set<number>();
  const picks: T[] = [];
  for (const id of ids) {
    const issue = ready.find((open) => String(open.number) === id);
    if (!issue) {
      warn(`  Skipping ${id}: it isn't one of the ready issues.`);
    } else if (!picked.has(issue.number)) {
      picked.add(issue.number);
      picks.push(issue);
    }
  }
  return picks;
}
