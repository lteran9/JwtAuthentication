# GitHub Copilot instructions

These instructions apply to all changes in this repository. The solution contains an ASP.NET Core Web API (`API/`) and an ASP.NET Core MVC application (`UI/`), both targeting .NET 10.

## Engineering principles

- **KISS:** Prefer the simplest design that clearly solves the stated problem. Avoid unnecessary abstractions, dependencies, configuration, and indirection.
- **YAGNI:** Do not add speculative features or generality. Implement current requirements and extend later when there is a concrete need.
- **DRY:** Reuse meaningful business rules and shared behavior, but do not merge code that only looks similar when its responsibilities or change patterns differ.
- **SOLID:** Keep responsibilities cohesive and dependencies understandable. Apply these principles where they improve the design; do not introduce interfaces, layers, or design patterns without a practical benefit.
- **Maintainability:** Favor clear names, small focused methods, explicit behavior, and familiar ASP.NET Core patterns over clever or compressed code.
- **Correctness:** Preserve existing behavior unless the request calls for a change. Handle invalid input and failures explicitly; do not hide errors behind broad catches, silent defaults, or success-shaped responses.
- **Least privilege and secure defaults:** Validate inputs at system boundaries, avoid exposing sensitive details, and use the narrowest permissions and data access appropriate to the feature.

## Repository conventions

- Keep changes consistent with the existing project structure and coding style. The projects use nullable reference types and implicit usings; preserve nullable correctness and avoid suppressions unless justified.
- Keep API concerns in `API/` and MVC/UI concerns in `UI/`. Reuse or share code only when there is a clear need and an appropriate project boundary.
- Use the framework's established dependency injection, configuration, routing, validation, and error-handling facilities. Keep controllers focused on HTTP concerns and move substantial application logic into an appropriate service.
- For API changes, use correct HTTP methods and status codes, validate request data, and keep response contracts consistent. Do not expose implementation details in error responses.
- For MVC changes, follow existing controller, model, and Razor view patterns. Pass view data through appropriate models rather than embedding application logic in views.
- If adding or changing authentication or JWT handling, use ASP.NET Core authentication and token-validation facilities with secure validation settings. Never hard-code signing keys, credentials, or tokens; obtain secrets from an approved secret store or environment-specific configuration. Do not log tokens or other credentials.
- Do not add a package when the existing framework or project code can meet the requirement. If a dependency is necessary, keep it targeted and explain the need in the change summary.
- Update directly related documentation or configuration when behavior or setup changes. Do not reformat or modify unrelated files.

## Validation

- Add or update focused tests for behavior changed or introduced, especially validation, authorization, and error cases. Follow the repository's existing test conventions; do not add a testing framework solely for a small change without a clear reason.
- Run the narrowest relevant test command. For broader changes, build the solution with `dotnet build JwtAuthentication.slnx`; run applicable tests with `dotnet test`.
- Report checks that were run and any that could not be run. Do not claim a build or test passed unless it was actually executed successfully.
