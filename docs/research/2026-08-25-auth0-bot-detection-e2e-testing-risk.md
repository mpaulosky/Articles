---
title: "Auth0 bot-detection risk for scripted E2E logins"
date: 2026-08-25
ticket: "https://github.com/mpaulosky/Articles/issues/157"
---

## Question

Does Auth0's hosted Universal Login page have bot-detection, anomaly-detection, CAPTCHA, or
rate-limiting mechanisms that could block or flake a Playwright-driven scripted login for the
project's 3 E2E test users (Admin/Author/User), both locally and from GitHub Actions runners?

## Findings

### Auth0's own stance on browser-automation testing

Auth0's support documentation on automated testing against Universal Login states plainly that
Auth0 does **not** support direct script-based/programmatic access to the internal login routes
(`/u/login/identifier`, `/u/login/password`) — those are considered private implementation detail
of the Universal Login state engine. The **recommended** approach is exactly what this project
already decided on: drive the real page with a browser automation tool (Cypress, Playwright, or
Selenium), entering credentials and clicking through like a real user, rather than trying to script
the underlying HTTP calls.
(Source: [Auth0 Support Center — Automated Testing with Identifier First Universal Login](https://support.auth0.com/center/s/article/automated-testing-with-identifier-first-universal-login))

### Bot Detection

Bot Detection is a real, tenant-wide feature (not connection-scoped) that watches for **burst
patterns** of login/signup/password-reset attempts from an IP address that statistically resemble
credential-stuffing or list-validation attacks. On trigger, it inserts a verification step before
the login can complete — by default an "Auth Challenge" (a CAPTCHA-free JS verification), with
optional fallback to a traditional CAPTCHA. Three mitigations exist:

- Set the CAPTCHA/challenge response to **"Never"**, disabling the extra step outright.
- Add up to 100 IP addresses/CIDR ranges to a tenant-wide **IP Allowlist** that bypasses detection.
- Enable **"Fail Open"**, so auth isn't blocked if the bot-detection service itself is unreachable.

(Source: [Auth0 Docs — Bot Detection](https://auth0.com/docs/secure/attack-protection/bot-detection))

A small, fixed set of 3 known-valid test accounts logging in a handful of times per test run is a
low, steady volume — nothing like a credential-stuffing burst — so Bot Detection is unlikely to
trigger from normal use. The practical risk is concentrated in CI:

### Why GitHub Actions runners are the real risk, not local dev

- **Local dev**: a developer's machine has a stable, low-volume IP. Bot Detection and the related
  Suspicious IP Throttling / Brute-force Protection features are very unlikely to interfere.
- **GitHub Actions (hosted runners)**: runner IPs are drawn from large, shared, rotating pools used
  by an enormous number of unrelated workflows and organizations simultaneously. Auth0's default
  Suspicious IP Throttling threshold is **100 login attempts per day per IP**
  ([Auth0 Support — Default Values for Suspicious IP Throttling](https://support.auth0.com/center/s/article/Default-values-for-Suspicious-IP-Throttling)).
  Because that IP is shared across unrelated Auth0 customers' CI traffic too, this project's own
  3-logins-per-build volume could tip an already-warm shared IP over the threshold — a risk this
  project can't fully control by keeping its own volume low. IP-allowlisting GitHub's runner ranges
  isn't practical either: GitHub publishes very large, frequently-changing CIDR blocks, incompatible
  with the 100-entry Auth0 allowlist cap.
- Auth0 explicitly recommends **against** disabling Suspicious IP Throttling or Brute-force
  Protection outright, since both are real account-security controls.

## Recommendation for the fixture-design ticket (#158)

1. **Keep the real-UI-login approach** — it matches Auth0's own recommended testing pattern, so
   nothing changes there.
2. **Locally, do nothing special** — default Attack Protection settings are very unlikely to
   interfere with a developer's own machine.
3. **For CI, set Bot Detection's challenge response to "Never" on the Auth0 application/connection
   used for tests**, rather than trying to allowlist GitHub's runner IP ranges (impractical) or
   disabling Suspicious IP Throttling/Brute-force Protection (Auth0 advises against it, and it's
   not what's actually causing Bot Detection to fire). Confirm whether this app uses a single Auth0
   tenant for both prod and test, or a separate one — if separate, scope this setting change to the
   test-only tenant/connection so production login security is untouched.
4. **The storage-state-reuse design already limits login volume** to once per role per CI run
   (not once per test), which keeps the project's own contribution to any shared-IP throttling
   count as small as realistically possible — this is a good reason not to relax that design choice.
5. Treat occasional CI login flakiness from shared-IP throttling as a known residual risk even
   after mitigation #3 — worth a retry-once pattern in the fixture rather than assuming it's fully
   eliminated.

## Sources

- [Auth0 Support Center — Automated Testing with Identifier First Universal Login](https://support.auth0.com/center/s/article/automated-testing-with-identifier-first-universal-login)
- [Auth0 Docs — Bot Detection](https://auth0.com/docs/secure/attack-protection/bot-detection)
- [Auth0 Docs — Suspicious IP Throttling](https://auth0.com/docs/attack-protection/suspicious-ip-throttling)
- [Auth0 Support Center — Default Values for Suspicious IP Throttling](https://support.auth0.com/center/s/article/Default-values-for-Suspicious-IP-Throttling)
- [Auth0 Docs — Brute-Force Protection](https://auth0.com/docs/secure/attack-protection/brute-force-protection)
