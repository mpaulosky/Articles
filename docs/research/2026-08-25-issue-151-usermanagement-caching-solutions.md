# UserManagement Caching: Roles-Cache Invalidation Gap and N+1 Auth0 Role Calls

Research for issue #151.

## Summary and recommendation (TL;DR)

**Problem 1 (invalidation) is not the bug the issue title implies once you look at what actually changes the roles catalog.** This codebase has **no code path anywhere** that calls Auth0's role-catalog-mutating endpoints (`POST /roles`, `PATCH /roles/{id}`, `DELETE /roles/{id}` — exposed by the SDK as `client.Roles.CreateAsync/UpdateAsync/DeleteAsync`). The only Auth0 "Roles" calls in the repo are `client.Roles.ListAsync` (read the catalog) and `client.Users.Roles.AssignAsync/DeleteAsync` (assign/remove a role **to/from a user**, which changes a user's role membership, not the roles catalog itself). Per Auth0's own docs, assigning/removing a user's role uses separate "Associate/Remove roles" endpoints that don't touch role name/description/id (["Assign roles to a user"](https://auth0.com/docs/api/management/v2/users/post-user-roles), ["Update a role"](https://auth0.com/docs/api/management/v2/roles/patch-roles-by-id)). So **`AssignRoleCommand`/`RemoveRoleCommand` correctly do *not* call `InvalidateRolesAsync`** — doing so would be a no-op fix for a bug that isn't there. The real gap is that `InvalidateRolesAsync` is dead code because there is no role-CRUD feature in this app at all; the roles cache is effectively static data whose staleness is bounded only by TTL. **Recommendation:** either (a) leave `InvalidateRolesAsync` unused but call it out as intentionally-reserved-for-a-future-role-CRUD-feature, or (b) if role CRUD is added later, call `InvalidateRolesAsync` from those new handlers — do not wire it into the existing assign/remove-role-to-user handlers.

**Problem 2 (N+1) is real and Auth0 has no first-class fix for the exact shape of this query** (paged users + regular Users API), confirmed by both Auth0's own community engineers and the API reference: `GET /api/v2/users` has no roles-include parameter (["Get Users"](https://auth0.com/docs/api/management/v2/users/get-users)), and there is no bulk "roles-by-user" endpoint for regular (non-Organization) users — Auth0 staff have stated this directly in community threads. The one genuine bulk mechanism Auth0 ships is scoped to **Organizations**: `GET /api/v2/organizations/{id}/members?fields=roles` returns each member's roles in the same call, but only for users who are members of an Auth0 Organization — a data-modeling change this app doesn't currently use. **Recommendation: don't restructure around Organizations for this.** Instead: (1) bound the blast radius with concurrency-limited parallel `Users.Roles.ListAsync` calls (e.g. `Parallel.ForEachAsync` / `SemaphoreSlim`-gated) instead of the current fully sequential `await foreach`, since Auth0's Management API rate limits are tight enough (as low as **2 req/s sustained, burst 10, on Free/non-production tenants**; **16 req/s sustained, burst 50, on Enterprise production tenants**) that unbounded fan-out risks 429s at even modest user counts; (2) separately fix the **token-caching problem** by migrating `Auth0ManagementApiClientFactory` off its per-call manual OAuth POST onto the SDK's own `ClientCredentialsTokenProvider` (available via the newer `ManagementClient` wrapper in `Auth0.ManagementApi`), which acquires and refreshes tokens automatically instead of once per Management API call; (3) lean on the existing/soon-5-minute `GetOrFetchUsersAsync` cache (see `docs/plans/manageroles-cache-refresh/plan.md`) as the primary mitigation for read frequency, since Auth0 has no cheaper native bulk-roles-per-user endpoint to replace the N+1 shape with.

---

## 1. Roles-cache invalidation: what actually changes the roles catalog, and does this codebase touch it

Auth0's Management API v2 models **roles** (the catalog: id/name/description) and **user-role assignments** (which roles a given user holds) as separate resource groups with separate endpoints:

- **Roles catalog** — `POST /api/v2/roles` (create), `GET /api/v2/roles` / `GET /api/v2/roles/{id}` (read), `PATCH /api/v2/roles/{id}` (update name/description), `DELETE /api/v2/roles/{id}` (delete). The Update Role endpoint confirms it "modifies the details of a specific... role specified by ID," changing only `name`/`description`. ([Update a role](https://auth0.com/docs/api/management/v2/roles/patch-roles-by-id), [Create a role](https://github.com/auth0/docs/blob/master/articles/api/management/guides/roles/create-roles.md))
- **User-role assignment** — `POST /api/v2/users/{id}/roles` (assign), `DELETE /api/v2/users/{id}/roles` (remove), `GET /api/v2/users/{id}/roles` (list a user's roles). These endpoints only change which roles a specific user has; they never touch a role's `id`/`name`/`description`. ([Assign roles to a user](https://auth0.com/docs/api/management/v2/users/post-user-roles), [Remove roles from a user](https://github.com/auth0/docs/blob/master/articles/api/management/guides/users/remove-user-roles.md))

**Repo search result:** grepping the whole `src/` tree for `client.Roles.` (the SDK surface for the catalog operations) turns up exactly one call site, and it's a read:

```
src/Web/Components/Features/UserManagement/ManageRoles/UserManagementHandler.cs:152:
    var rolesPager = await client.Roles.ListAsync(new ListRolesRequestParameters(), ...)
```

There is **no** `client.Roles.CreateAsync`, `client.Roles.UpdateAsync`, or `client.Roles.DeleteAsync` anywhere in the codebase. The only two mutation call sites involving "Roles" at all are:

```
UserManagementHandler.cs:86-89   client.Users.Roles.AssignAsync(request.UserId, ...)   // AssignRoleCommand
UserManagementHandler.cs:118-121 client.Users.Roles.DeleteAsync(request.UserId, ...)    // RemoveRoleCommand
```

Both call `client.Users.Roles.*` (user-role assignment), not `client.Roles.*` (roles catalog), and both correctly call `cache.InvalidateUsersAsync(...)` afterward — the users-with-roles cache **is** invalidated correctly on the data it actually changes. Neither handler calls `InvalidateRolesAsync`, and per the API semantics above, they shouldn't: assigning a role to a user doesn't change the roles catalog, so the `AllRoles` cache entry isn't made stale by that operation.

**Conclusion:** the roles catalog in this app is read-only from the app's perspective — there is no in-app feature (UI, command, or handler) that creates, renames, or deletes a role. `IUserManagementCacheService.InvalidateRolesAsync` is therefore genuinely dead code today, not a wiring bug. The existing `docs/plans/manageroles-cache-refresh/plan.md` (written for the separate #148/#149/#150 TTL/polling/diff work) independently reaches the same conclusion, listing "the roles-cache invalidation gap... is never called by any handler" as an explicit **out-of-scope pre-existing item**, without characterizing it as a wrong-handler bug. Any fix for issue #151's problem 1 should therefore be scoped to *either* leaving it as documented dead code (with a comment explaining why), *or* adding it only if/when a role-CRUD admin feature is built, calling it from that new Create/Update/Delete-role handler — not from `AssignRoleCommand`/`RemoveRoleCommand`.

## 2. N+1 Auth0 Management API calls in `GetUsersWithRolesQuery`

### 2a. No bulk "users + roles" endpoint for regular users

`GET /api/v2/users` (List or Search Users) supports `page`, `per_page`, `include_totals`, `sort`, `connection`, `fields`/`include_fields`, `q` (Lucene search), `search_engine`, and `primary_order` — there is no parameter to expand/include each user's role assignments in the same response. ([Get Users](https://auth0.com/docs/api/management/v2/users/get-users))

Community threads confirm this is a recognized, unresolved gap for exactly this use case (listing many users with their roles). A thread asking how to retrieve roles for 2,000–5,000 users across 700 roles efficiently got no endpoint-level solution from Auth0 staff — only an Organizations-scoped workaround (`GET /api/v2/organizations/{id}/members`, which requires the users to belong to an Organization) and an acknowledgment that mirroring roles into `app_metadata` risks staleness. ([community.auth0.com — 700 roles, 2000-5000 users](https://community.auth0.com/t/how-to-retrieve-roles-and-user-list-with-700-roles-and-2000-5000-users-management-api/138153)) A separate thread specifically about getting organization members with roles got the same answer from an Auth0 engineer at the time it was asked: **"currently, there isn't a dedicated endpoint to do this"** for combining member listing with role listing in one call. ([community.auth0.com — org members and roles](https://community.auth0.com/t/get-organization-members-and-their-roles-in-management-api/108793))

### 2b. The one real bulk mechanism Auth0 does ship — Organizations only

Auth0's Organizations feature *does* support returning roles inline for org members: `GET /api/v2/organizations/{id}/members` accepts `fields=roles` (with the `read:organization_member_roles` scope) to return each listed member's roles in the same response, avoiding a per-user round trip. This is documented on the "Get members who belong to an organization" reference page and is reflected in the Auth0 .NET SDK's own `OrganizationMember.Roles` property, which the SDK's maintainers note is only populated "when `OrganizationGetAllMembersRequest.Fields` includes `roles`" ([Auth0 docs — Get organization members](https://translations.mintlify.app/docs/api/management/v2/organizations/get-organization-members); [auth0/auth0.net PR #895 — make `OrganizationMember.Roles` nullable](https://github.com/auth0/auth0.net/pull/895)). This is Auth0's real, documented bulk answer to "users + roles in one call" — but it only exists for the Organizations resource model, which this app does not currently use for its user base (this app manages individual Auth0 users/roles directly, not via Organizations). Restructuring the whole user base into an Organization purely to get this one query optimization is a significant architectural change, disproportionate to fixing an N+1 query.

### 2c. Auth0 Actions / custom claims — not applicable here

Auth0 Actions (e.g. a post-login Action writing roles into ID token custom claims) is Auth0's documented mechanism for surfacing a role list without an extra Management API call — but it only surfaces the **currently authenticating user's own roles** into their token, not a bulk admin-facing list of *all* users' roles. It solves a different problem (avoiding a Management API call at login time for the logged-in user) than `GetUsersWithRolesQuery`'s admin-panel "list every user with their roles" requirement, so it isn't a fit for this handler.

### 2d. Rate limits — why unbounded N+1 fan-out is risky

Auth0 documents separate rate-limit policies for the Authentication API vs. the Management API, varying by tenant plan and by Production vs. Development/Staging tenant type. ([Rate Limit Policy overview](https://auth0.com/docs/troubleshoot/customer-support/operational-policies/rate-limit-policy)) Confirmed figures from the tier-specific configuration pages:

- **Free / non-production tenants:** Management API burst limit **2**, sustained **2 requests/second** (the page lists specific higher limits only for a short allowlist of endpoints — dynamic client registration, custom domain verification, signing-key rotation, email template/provider config — none of which apply here, so `Users`/`Roles` calls fall under the 2 req/s default). ([Free/Public tier rate limits](https://auth0.com/docs/troubleshoot/customer-support/operational-policies/rate-limit-policy/rate-limit-configurations/free-public))
- **Essentials/Professional B2B tenants:** endpoint-specific limits, e.g. "Read Users" burst **40**, sustained **500/minute** (~8.3/s); "Write Users" burst **20**, sustained **200/minute**; all other endpoints combined burst **10**, sustained **150/minute**. ([Essentials/Professional B2B rate limits](https://auth0.com/docs/troubleshoot/customer-support/operational-policies/rate-limit-policy/rate-limit-configurations/essentials-professional-b2b))
- **Enterprise production tenants:** global Management API burst **50**, sustained **16 requests/second**; non-production Enterprise tenants get burst **10**, sustained **2/second**. ([Enterprise/Public tier rate limits](https://auth0.com/docs/troubleshoot/customer-support/operational-policies/rate-limit-policy/rate-limit-configurations/enterprise-public))

At any of these tiers, a fully sequential 1-call-per-user loop (as the current code does) is *safe* from rate-limit errors but slow (latency scales linearly with user count — e.g. hundreds of users means hundreds of sequential round trips). The risk direction issue #151 should guard against is the opposite failure mode: a naive "parallelize everything" fix without a concurrency cap could burst past even Enterprise's 50-request burst allowance and start receiving `429`s, especially on Free/non-production tenants where the sustained limit is only 2 req/s. Auth0's own support guidance for bulk role retrieval explicitly recommends "a solution that sequentially manages API calls and enforces a safe rate limit," inspecting the `x-ratelimit-remaining`/`x-ratelimit-reset`/`Retry-After` response headers and pausing once remaining calls drop below a small threshold (the article's own example: pause when `remaining < 5`), and recommends using an official SDK specifically because official SDKs "include built-in rate limit and retry logic." ([Auth0 Support Center — Retrieving Auth0 User Roles in Bulk While Respecting Rate Limits](https://support.auth0.com/center/s/article/Retrieving-Auth0-User-Roles-in-Bulk-While-Respecting-Rate-Limits))

### 2e. Secondary issue: no Management API token caching

`Auth0ManagementApiClientFactory.CreateAsync` (`src/Web/Components/Features/UserManagement/Auth0/Auth0ManagementApiClientFactory.cs`) does a manual `HttpClient.PostAsJsonAsync` OAuth `client_credentials` token exchange on **every** call, then constructs a fresh `ManagementApiClient` with that token — this happens once per `GetUsersWithRolesQuery`/`AssignRoleCommand`/etc. invocation, independent of and in addition to the per-user N+1 role calls, multiplying total Management API traffic. The Auth0.NET SDK ships a purpose-built alternative: the `ManagementClient` wrapper accepts a `TokenProvider`, and its built-in `ClientCredentialsTokenProvider` acquires and refreshes tokens automatically ("tokens are acquired and refreshed automatically") rather than on every call — Auth0 Management API access tokens are valid for 24 hours, so caching one token across requests (until near expiry) eliminates the redundant OAuth round trips entirely. ([auth0/auth0.net README — token providers](https://github.com/auth0/auth0.net))

## Recommended implementation approach

1. **Problem 1 — do not add `InvalidateRolesAsync` calls to `AssignRoleCommand`/`RemoveRoleCommand`.** Those handlers correctly invalidate only the users cache, since user-role assignment doesn't touch the roles catalog. If issue #151 wants the dead-code gap closed, either remove `InvalidateRolesAsync` and its cache key entirely (no code path can ever call it today) or leave it in place with a short comment noting it's reserved for a not-yet-built role-CRUD feature (create/rename/delete role), and wire it into that feature's handler(s) if/when it's built.
2. **Problem 2 — cap concurrency on the per-user roles fetch instead of leaving it fully sequential or naively parallelizing.** Replace the sequential `await foreach` + per-user `Users.Roles.ListAsync` in `GetUsersWithRolesQuery`'s handler with a bounded-concurrency fan-out (e.g. `Parallel.ForEachAsync` with `MaxDegreeOfParallelism` set conservatively, or a `SemaphoreSlim` gate), sized to stay well under the tenant's documented sustained rate limit (as low as 2 req/s on Free/non-production tenants) — this reduces wall-clock latency without risking `429`s, matching Auth0's own guidance to "sequentially manage API calls and enforce a safe rate limit" rather than removing pacing altogether.
3. **Don't restructure onto Auth0 Organizations** to chase the `fields=roles` bulk endpoint — it's real, but scoped to Organization members, and adopting Organizations purely for this query is a disproportionate architectural change for an N+1 fix.
4. **Fix Management API token reuse independently**: migrate `Auth0ManagementApiClientFactory` from its manual per-call OAuth POST to the SDK's `ManagementClient` + `ClientCredentialsTokenProvider`, which caches and auto-refreshes the token instead of exchanging a new one on every Management API call — this compounds with fix #2 by cutting overall Management API request volume regardless of how the N+1 role-fetch loop is restructured.

## References

- https://auth0.com/docs/api/management/v2/users/get-users
- https://auth0.com/docs/api/management/v2/roles/patch-roles-by-id
- https://github.com/auth0/docs/blob/master/articles/api/management/guides/roles/create-roles.md
- https://auth0.com/docs/api/management/v2/users/post-user-roles
- https://github.com/auth0/docs/blob/master/articles/api/management/guides/users/remove-user-roles.md
- https://community.auth0.com/t/how-to-retrieve-roles-and-user-list-with-700-roles-and-2000-5000-users-management-api/138153
- https://community.auth0.com/t/get-organization-members-and-their-roles-in-management-api/108793
- https://translations.mintlify.app/docs/api/management/v2/organizations/get-organization-members
- https://github.com/auth0/auth0.net/pull/895
- https://auth0.com/docs/troubleshoot/customer-support/operational-policies/rate-limit-policy
- https://auth0.com/docs/troubleshoot/customer-support/operational-policies/rate-limit-policy/rate-limit-configurations/free-public
- https://auth0.com/docs/troubleshoot/customer-support/operational-policies/rate-limit-policy/rate-limit-configurations/essentials-professional-b2b
- https://auth0.com/docs/troubleshoot/customer-support/operational-policies/rate-limit-policy/rate-limit-configurations/enterprise-public
- https://support.auth0.com/center/s/article/Retrieving-Auth0-User-Roles-in-Bulk-While-Respecting-Rate-Limits
- https://github.com/auth0/auth0.net
- docs/plans/manageroles-cache-refresh/plan.md (repo-internal, confirms out-of-scope status of both gaps from prior work)
