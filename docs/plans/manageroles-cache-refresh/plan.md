# ManageRoles Cache + Background-Refresh Plan

Charted via wayfinder map [#148 — ManageRoles: longer cache + background-refresh design](https://github.com/mpaulosky/Articles/issues/148).

## Goal

Extend the existing `UserManagementCacheService` shared cache to a ~5 minute TTL, and add page-driven polling to `ManageRoles.razor` (~30s per open page) that silently refreshes `_users`/`_availableRoles` and calls `StateHasChanged` when Auth0-sourced data changes elsewhere (e.g. edited directly in the Auth0 dashboard). Scoped to the ManageRoles feature only — not a shared caching framework.

## Background

- Data source: Auth0 Management API (`Auth0.ManagementApi` SDK) via `UserManagementHandler.cs` — no local DB mirror.
- Cache: `Web.Infrastructure.Caching.UserManagementCacheService` — L1 `IMemoryCache` (currently 30s) / L2 Redis `IDistributedCache` (currently 2min), keyed via `UserManagementCacheKeys.AllUsers` (`"usermgmt:users"`) / `AllRoles` (`"usermgmt:roles"`). `AssignRoleCommand`/`RemoveRoleCommand` already call `InvalidateUsersAsync` on mutation.
- `Auth0ManagementApiClientFactory` does a fresh OAuth token exchange on every Auth0 call (no token caching) — the reason the thundering-herd guard matters.
- App topology: single Web instance today — Aspire's `AppHost.cs` registers `Web` with no `.WithReplicas(...)`, and no deployment manifests exist in the repo.
- Models (`src/Web/Components/Features/UserManagement/Models/`): `internal sealed record UserWithRolesDto(string UserId, string Email, string Name, IReadOnlyList<string> Roles)`, `internal sealed record RoleDto(string Id, string Name)`.
- Ordering facts: users list and each user's `Roles` come straight from the Auth0 SDK pager in `UserManagementHandler.cs` with no sort applied — order not guaranteed stable. The available-roles list is the exception: `ManageRoles.razor` sorts it alphabetically (`OrderBy(role.Name, StringComparer.OrdinalIgnoreCase)`) before display.

## Decisions

1. **Cache scope** — stays shared across all admin sessions; only the TTL changes.
2. **Cache TTL** — ~5 minutes, hardcoded constant (matches the existing pattern; no new config surface).
3. **Refresh mechanism** — page-driven polling: each open `ManageRoles` page runs its own timer (~30s); no app-wide background service.
4. **Update behavior** — silently replace `_users`/`_availableRoles` and call `StateHasChanged`; no confirmation banner.
5. **Thundering-herd guard** ([#149](https://github.com/mpaulosky/Articles/issues/149)) — per-cache-key, in-process single-flight lock inside `UserManagementCacheService`'s fetch methods, with double-checked locking. In-process (not Redis-distributed) because the app runs as a single instance today.
6. **Equality/diff strategy** ([#150](https://github.com/mpaulosky/Articles/issues/150)) — custom `Equals`/`GetHashCode` on `UserWithRolesDto` treating `Roles` as an unordered set; `RoleDto` needs no change. A new static comparison helper diffs the users list as an unordered set keyed by `UserId`, and the roles list with ordered `SequenceEqual` after applying the existing display sort to both sides.

## Implementation

### 1. Cache TTL

In `UserManagementCacheService.cs`, change:

```csharp
private static readonly MemoryCacheEntryOptions LocalOpts =
    new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(30));

private static readonly DistributedCacheEntryOptions RedisOpts =
    new DistributedCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(2));
```

to a shared ~5 minute constant for both tiers (keep L1/L2 as separate fields for clarity, both set to `TimeSpan.FromMinutes(5)`).

### 2. Single-flight guard

Add a `ConcurrentDictionary<string, SemaphoreSlim>` field to `UserManagementCacheService`, keyed by cache key (`AllUsers` / `AllRoles` share the same map but different keys, so they never block each other). In `GetOrFetchUsersAsync`/`GetOrFetchRolesAsync`, on an L1+L2 miss:

1. Get-or-add the `SemaphoreSlim` for that key.
2. `await semaphore.WaitAsync(ct)`.
3. Re-check L1 then L2 inside the lock (double-checked locking) — if another caller already populated the cache while this one waited, return that instead of fetching again.
4. If still a miss, call `fetch()`, populate L1 + L2, return the result.
5. `finally { semaphore.Release(); }`.

### 3. Equality / diff strategy

- Add custom `Equals`/`GetHashCode` overrides to `UserWithRolesDto` (`src/Web/Components/Features/UserManagement/Models/UserWithRolesDto.cs`) that compare `Roles` as an unordered set (e.g. sort-then-`SequenceEqual`, or `HashSet<string>.SetEquals`) with an order-independent hash (e.g. XOR/aggregate over each role's hash, not a positional combine). `RoleDto` is unchanged.
- Add a small static comparison helper in the `ManageRoles` feature folder (e.g. `ManageRolesComparer.cs`) with two methods:
  - `bool UsersChanged(IReadOnlyList<UserWithRolesDto> current, IReadOnlyList<UserWithRolesDto> fetched)` — builds a `Dictionary<string, UserWithRolesDto>` keyed by `UserId` for each side; returns `true` if the key sets differ or any shared key's values differ (using the new `UserWithRolesDto` equality).
  - `bool RolesChanged(IReadOnlyList<RoleDto> current, IReadOnlyList<RoleDto> fetched)` — sorts both sides with the same `OrderBy(role.Name, StringComparer.OrdinalIgnoreCase)` used for display, then compares with `SequenceEqual`.

### 4. Page-driven polling

In `ManageRoles.razor`'s `@code` block:

- Add a `PeriodicTimer` field, started in `OnInitializedAsync` (or `OnAfterRenderAsync(firstRender: true)`) with a ~30s period, and disposed in `IDisposable.Dispose()` (component must implement `IDisposable`).
- Run the timer loop as a background `Task` (`_ = PollAsync()`), guarded by a `CancellationTokenSource` that's cancelled on dispose — this also covers circuit disconnects, since Blazor Server disposes components when a circuit ends.
- Each tick: fetch users/roles the same way `RefreshAsync` does today (via `Sender.Send`, which goes through the now-5-minute cache), then use `ManageRolesComparer.UsersChanged`/`RolesChanged` against the currently-held `_users`/`_availableRoles`. Only on a real difference: replace the fields (applying the existing alphabetical sort to roles) and call `StateHasChanged` (via `InvokeAsync(StateHasChanged)` since the timer callback runs off the Blazor renderer's sync context).
- `AssignRole`/`RemoveRole`'s existing `RefreshAsync` path is unaffected — it already invalidates the cache on mutation and re-fetches immediately.

## Workstream

1. Extend `UserManagementCacheService`'s L1/L2 TTLs to ~5 minutes.
2. Add the per-key single-flight lock to `GetOrFetchUsersAsync`/`GetOrFetchRolesAsync`.
3. Add `Equals`/`GetHashCode` overrides to `UserWithRolesDto`.
4. Add the `ManageRolesComparer` static helper with `UsersChanged`/`RolesChanged`.
5. Add `PeriodicTimer`-driven polling to `ManageRoles.razor`, wired to the comparer and `StateHasChanged`.
6. Validate the app builds and the targeted tests pass.

## TDD checkpoints

- A cache-service test asserting concurrent misses on the same key result in exactly one `fetch()` invocation (single-flight).
- A `UserWithRolesDto` equality test asserting two instances with the same roles in different order are equal, and that `GetHashCode` matches for both.
- `ManageRolesComparer` tests: unchanged data (including reordered users/roles) reports no change; an added/removed/edited user or role reports a change.
- A `ManageRoles.razor` component test (in `tests/Web.UI.Tests/Features/UserManagement/ManageRoles/ManageRolesTests.cs`) asserting a poll tick that returns unchanged data does not trigger a re-render, and one that returns changed data updates `_users`/`_availableRoles` and re-renders.
- These tests are required to pass before the implementation is considered complete.

## Not yet specified

- Exact `PeriodicTimer` vs `System.Threading.Timer` choice for the poll loop and its disposal wiring across circuit disconnects is captured directly in [Implementation §4](#4-page-driven-polling) above rather than as its own decision — low ambiguity, `PeriodicTimer` is the modern idiomatic choice and disposal follows the component's existing lifecycle.

## Out of scope

- The roles-cache invalidation gap: `InvalidateRolesAsync` exists on `UserManagementCacheService` but is never called by any handler. Pre-existing bug, unrelated to this effort.
- The N+1 Auth0 call per user in `GetUsersWithRolesQuery` (one `Roles.ListAsync` call per user in `UserManagementHandler.cs`). Pre-existing perf issue, unrelated to this effort.
