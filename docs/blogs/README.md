# Release Review Blog Index

This directory contains release review posts that document the evolution of the Articles project. Each post summarizes key changes, technical improvements, and the impact of each release.

## All Release Posts

| Date | Title | Tags |
|------|-------|------|
| 2026-08-19 | [Articles v0.1.1: MongoDB Data Layer Implementation](2026-08-19-v0.1.1-mongodb-data-layer.md) | mongodb, data-access, repository-pattern, entity-framework, tdd |
| 2026-08-19 | [Articles v0.1.0: Domain Refactoring Complete](2026-08-19-v0.1.0-domain-refactoring.md) | domain-driven-design, refactoring, vertical-slices, clean-architecture, testing |
| 2026-08-19 | [Articles v0.0.6: Domain Refactoring Preparation](2026-08-19-v0.0.6-domain-refactoring-prep.md) | architecture, domain-design, refactoring, redis, aspire |
| 2026-08-18 | [Articles v0.0.5: Auth0 Integration and Stability](2026-08-18-v0.0.5-auth0-integration.md) | auth0, authentication, security, testing, stability |
| 2026-08-17 | [Articles v0.0.4: Theme Management Testing](2026-08-17-v0.0.4-testing-expansion.md) | unit-tests, blazor, theme-management, web-testing |
| 2026-08-16 | [Articles v0.0.3: CI Coverage Report Discovery](2026-08-16-v0.0.3-ci-improvements.md) | ci-cd, code-coverage, codecov, testing |
| 2026-08-16 | [Articles v0.0.2: Enhanced Project Validation](2026-08-16-v0.0.2-project-validation.md) | validation, ci-cd, quality-gates |
| 2026-08-16 | [Articles v0.0.1: Initial Setup and Foundation](2026-08-16-v0.0.1-initial-setup.md) | initial-release, codeql, dependabot, project-setup |

## By Category

### Release & Automation
- [v0.1.1: MongoDB Data Layer Implementation](2026-08-19-v0.1.1-mongodb-data-layer.md)
- [v0.1.0: Domain Refactoring Complete](2026-08-19-v0.1.0-domain-refactoring.md)
- [v0.0.6: Domain Refactoring Preparation](2026-08-19-v0.0.6-domain-refactoring-prep.md)
- [v0.0.5: Auth0 Integration and Stability](2026-08-18-v0.0.5-auth0-integration.md)
- [v0.0.3: CI Coverage Report Discovery](2026-08-16-v0.0.3-ci-improvements.md)
- [v0.0.2: Enhanced Project Validation](2026-08-16-v0.0.2-project-validation.md)
- [v0.0.1: Initial Setup and Foundation](2026-08-16-v0.0.1-initial-setup.md)

### Testing
- [v0.1.1: MongoDB Data Layer Implementation](2026-08-19-v0.1.1-mongodb-data-layer.md)
- [v0.0.5: Auth0 Integration and Stability](2026-08-18-v0.0.5-auth0-integration.md)
- [v0.0.4: Theme Management Testing](2026-08-17-v0.0.4-testing-expansion.md)
- [v0.0.3: CI Coverage Report Discovery](2026-08-16-v0.0.3-ci-improvements.md)

### Infrastructure
- [v0.1.1: MongoDB Data Layer Implementation](2026-08-19-v0.1.1-mongodb-data-layer.md)
- [v0.0.6: Domain Refactoring Preparation](2026-08-19-v0.0.6-domain-refactoring-prep.md)
- [v0.0.5: Auth0 Integration and Stability](2026-08-18-v0.0.5-auth0-integration.md)

### Architecture
- [v0.1.0: Domain Refactoring Complete](2026-08-19-v0.1.0-domain-refactoring.md)

## Release Milestones

### v0.1.x Series - Data & Domain
The 0.1 series focused on establishing the domain architecture and data persistence layer:
- **v0.1.1**: MongoDB data layer with repositories and EF Core
- **v0.1.0**: Complete feature-based domain refactoring

### v0.0.x Series - Foundation
The initial series established project infrastructure and core capabilities:
- **v0.0.6**: Prepared for domain refactoring
- **v0.0.5**: Integrated Auth0 authentication
- **v0.0.4**: Expanded test coverage for theme management
- **v0.0.3**: Fixed CI coverage reporting
- **v0.0.2**: Validated project setup
- **v0.0.1**: Initial project setup and automation

## Contributing

When adding new release review posts:

1. Follow the naming convention: `YYYY-MM-DD-v{version}-{slug}.md`
2. Include complete frontmatter with all required fields
3. Add the post to this index in reverse chronological order
4. Update the main README.md with top 5 posts
5. Categorize appropriately for easy discovery

## Automation

The [Squad Release workflow](../../.github/workflows/squad-release.yml) automatically generates release review posts for merged PRs and updates this index. Manual posts can be added following the same format.
