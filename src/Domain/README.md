# Domain

This project is the heart of the solution. It contains the domain model and has **no dependencies on infrastructure,
frameworks, or external concerns**.

## What belongs here

- **Entities** — objects with identity that persist over time (e.g., `User`, `Order`)
- **Value Objects** — immutable objects defined by their attributes (e.g., `Email`, `Money`)
- **Domain Interfaces** — repository and service contracts (`IUserRepository`, `IEmailService`)
- **Domain Events** — events raised when something meaningful happens in the domain
- **Enumerations** — strongly-typed enums modelled as classes when needed
- **Specifications** — encapsulated query logic reusable across the domain

## What does NOT belong here

- EF Core, database code, or any persistence concern
- HTTP clients or external API calls
- Web logic or presentation concerns
- Configuration or DI registration

## Conventions

- All public types should have XML doc comments (enforced via `GenerateDocumentationFile`)
- Entities extend a base `Entity` class or carry a typed `Id`
- Value Objects override `Equals` and `GetHashCode` and are sealed
- Interfaces are prefixed with `I`
