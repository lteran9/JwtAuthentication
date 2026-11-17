# JWT Authentication in ASP.NET Core

A how-to sample that secures an ASP.NET Core Web API with JSON Web Tokens (JWT), backed by ASP.NET Core Identity, and shows an MVC UI that signs in and calls the protected API.

| Project | Role |
| --- | --- |
| `API/` | Web API. Registers users at `POST /auth/register` and issues tokens at `POST /auth/login`. Serves World Cup match results at `GET /world-cup/matches` (requires a token). |
| `UI/` | MVC app. Signs users in through the API and stores the resulting JWT in an encrypted, HttpOnly cookie. Shows World Cup results from the API. Its API client is generated from the committed OpenAPI document (`API/openapi.json`). |
| `API.Tests/` | Integration tests for registration and login that exercise the real pipeline with `WebApplicationFactory`. |

## How it works

```
Browser ──form──▶ UI /Account/Login ──POST /auth/login──▶ API (Identity checks password)
                       │                                      │
                       │◀──────── access token (JWT) ─────────┘
                       │
        stores JWT inside an encrypted, HttpOnly UI cookie
```

## Running the application with Docker Compose

Install Docker Desktop or Docker Engine with the Compose v2 plugin, then from the repository root create a local JWT signing key:

```bash
printf 'JWT_KEY=%s\n' "$(openssl rand -base64 48)" > .env
chmod 600 .env
```

`.env` is ignored by Git and Docker. Do not commit it or use this local setup as a production deployment.

Build and start the API and UI:

```bash
docker compose up --build
```

Open the UI at `http://localhost:8080`. The API is available at `http://localhost:8081`, including its OpenAPI document at `/openapi/v1.json`. Both ports are bound to loopback; use `localhost` for the UI so the authentication cookie works with the app's secure-cookie setting.

There is no pre-created account. Register one at `http://localhost:8080/Account/Register`, or with the API (replace both placeholders with your own credentials); passwords must meet the configured ASP.NET Core Identity policy:

```bash
curl -i http://localhost:8081/auth/register \
  -H 'Content-Type: application/json' \
  -d '{"userName":"<your-user-name>","password":"<your-strong-password>"}'
```

Stop the containers with `docker compose down`. The `api-data` named volume preserves the SQLite database across restarts and rebuilds. To remove the database and all other Compose-managed data, run `docker compose down --volumes`.

The Compose setup runs both apps in Development so the API applies pending EF Core migrations at startup. The UI reaches the API over the Compose network using the `api` service name; this is separate from the API's loopback-only host port. To onboard another developer, share these steps and have them generate their own `.env` signing key.

## World Cup results

`GET /world-cup/matches` returns FIFA World Cup matches from 1930 onward, newest first. Both query parameters are optional:

- `year`: a tournament year from 1930 to the current year. Other values return `400`.
- `team`: a team name, case-insensitive. It matches the team as either home or away.

The endpoint requires a token, so the UI forwards the access token from its cookie. The UI page is at `/WorldCup`.

The data is [API/Data/Seed/worldcup_matches.csv](/Users/lteran/Documents/Workspace/dotNet/PracticalScenarios/JwtAuthentication/API/Data/Seed/worldcup_matches.csv), a FIFA World Cup subset of [martj42/international_results](https://github.com/martj42/international_results) (CC0 1.0). It is embedded in the API and loaded into SQLite at startup in Development, only when the table is empty.

## Running tests

Run the integration tests from the repository root:

```bash
dotnet test JwtAuthentication.slnx
```

GitHub Actions runs the same tests ([tests.yml](/Users/lteran/Documents/Workspace/dotNet/PracticalScenarios/JwtAuthentication/.github/workflows/tests.yml)) on every push to `master`, including merged pull requests.

## Updating the API contract and the UI client

The UI's client is a committed file, [ApiClient.generated.cs](/Users/lteran/Documents/Workspace/dotNet/PracticalScenarios/JwtAuthentication/UI/Api/ApiClient.generated.cs), generated from [openapi.json](/Users/lteran/Documents/Workspace/dotNet/PracticalScenarios/JwtAuthentication/API/openapi.json). Nothing is generated during a build, so the UI and API builds stay independent. When you change the API's endpoints or DTOs, regenerate by hand:

1. Restore the local NSwag CLI (once per clone): `dotnet tool restore`
2. Start the API container, then export its contract:

   ```bash
   docker compose up --build --detach api
   curl -sS http://localhost:8081/openapi/v1.json | python3 -m json.tool --indent 2 > API/openapi.json
   ```

3. Regenerate the client:

   ```bash
   dotnet nswag openapi2csclient /input:API/openapi.json /output:UI/Api/ApiClient.generated.cs /namespace:UI.Api /className:ApiClient /jsonLibrary:SystemTextJson /dateType:System.DateOnly /useBaseUrl:false
   ```

4. Build and test: `dotnet build JwtAuthentication.slnx && dotnet test JwtAuthentication.slnx`

Commit `API/openapi.json` and `UI/Api/ApiClient.generated.cs` together.

## Decisions

| Topic | Decision | Why |
| --- | --- | --- |
| Identity source | ASP.NET Core Identity with EF Core and SQLite | Password hashing, lockout, and password policy come from the framework instead of hand-written code. |
| Signing algorithm | HS256 with a shared secret | Simplest option for a single API that both issues and validates tokens. Use RS256 when other services must validate tokens without being able to mint them. |
| Token lifetime | 15 minutes (`Jwt:AccessTokenMinutes`) | Limits the damage from a stolen token. There are no refresh tokens, so users sign in again when the token expires. |
| Validation | Issuer, audience, signing key, lifetime, and algorithm allowlist are all validated | Each check closes a known bypass. Skipping any of them is a common vulnerability. |
| Clock skew | 1 minute | The framework default is 5 minutes, which lets expired tokens work for longer than intended. |
| Login errors | Unknown user, locked-out user, and wrong password all return `401` | Callers can't tell which check failed, which makes account enumeration harder. |
| Where the UI keeps the JWT | Encrypted, HttpOnly cookie used as an auth token, with the cookie expiry set to the token expiry | Browser scripts can't read the token, and the UI never holds a cookie whose token has already expired. |
| Cookie flags | `HttpOnly`, `Secure`, `SameSite=Strict` | Mitigates XSS token theft and CSRF-style cross-site requests. |
| Open redirect | `ReturnUrl` is only followed when `Url.IsLocalUrl` returns true | Prevents attackers from using the login page to send users to another site. |
| UI API client | Generated with the NSwag CLI from the committed `API/openapi.json` into `UI/Api/ApiClient.generated.cs`, and committed | One source of truth for request and response shapes, with no hand-copied DTOs. Regenerating by hand (not at build time) keeps the UI and API builds loosely coupled. |
| Generated JSON and dates | System.Text.Json, and `DateOnly` for dates | Matches the framework and the API's own types, so no extra JSON library is needed. |

## Gotchas

- **Validation attributes on positional records must go on the parameter.** `[property: Required]` puts them on the property. MVC then throws `InvalidOperationException` and the request returns `500`. Write `[Required] string UserName` in the record's parameter list.
- **Order matters:** `app.UseAuthentication()` must come before `app.UseAuthorization()`. Without it, `HttpContext.User` is never populated and `[Authorize]` always fails.
- **Claim names change by default.** `JwtBearerOptions.MapInboundClaims` is `true` by default and renames `sub` to `ClaimTypes.NameIdentifier`. This sample turns it off so the claims match the JWT.
- **Never commit the signing key.** Compose reads `JWT_KEY` from `.env` and passes it as `Jwt__Key`; use an approved secret store in production. HS256 needs at least 32 bytes. `ValidateOnStart` checks the key when the API starts, so a missing or short key stops the app immediately.
- **Migrations run only in Development.** `Program.cs` migrates on startup in Development for convenience. Production should run migrations as a deployment step.
- **World Cup data loads only into an empty table.** Editing `worldcup_matches.csv` doesn't update an existing database. Run `docker compose down --volumes` to reload it.
- **Pin `Microsoft.OpenApi` to a patched version.** `API.csproj` pins 2.7.5. Older 2.x versions have a high-severity advisory (GHSA-v5pm-xwqc-g5wc), and the build reports it as NU1903 when the pin is removed.
- **Regenerate the client after contract changes, and don't hand-edit it.** `ApiClient.generated.cs` is generated code. Nothing regenerates it automatically, so an API change without regeneration silently leaves the UI on the old contract.
- **Non-success responses throw `ApiException`.** Check `StatusCode` to handle expected cases such as `401`, as `AccountController` does.
- **A JWT can't be revoked.** Logging out in the UI only deletes the UI's cookie, and a copy of the token stays valid until it expires. Keep lifetimes short. If you need immediate revocation, add a deny-list or move to reference tokens.
- **Don't put JWTs in `localStorage`.** Any script on the page can read them.
- **Registration is open in this sample.** Anyone can create an account. Restrict or remove `POST /auth/register` before going to production.
- **Duplicate-username errors reveal that an account exists.** Identity's error text leaks account existence. Return a generic message if that matters for your threat model.
- **Identity locks accounts after repeated failures** (5 failed attempts, 5 minute lockout by default). Tests use a fresh user for each case so lockout doesn't interfere.
- **Transitive dependency vulnerability.** The Identity/EF Core packages pulled in `SQLitePCLRaw.lib.e_sqlite3` 2.1.11, which bundles SQLite versions affected by CVE-2025-6965 (GHSA-2m69-gcr7-jv3q). `API/API.csproj` pins 2.1.14 to pick up the fix. Re-check with `dotnet list API package --vulnerable --include-transitive`.
- **Tests must not touch real secrets or data.** `ApiFactory` uses the `Development` environment so the startup migration runs, overrides the key with an in-memory test value (which takes precedence over user-secrets), and points at a temp SQLite file that is deleted afterwards. Releasing pooled connections first (`SqliteConnection.ClearAllPools`) lets the file delete cleanly.
- **`[Authorize]` returns `401` for missing or invalid tokens and `403` for authenticated users without permission.** This sample has no roles, so every failure is a `401`.

## Configuration reference

`API/appsettings.json`:

```json
"ConnectionStrings": { "Default": "Data Source=app.db" },
"Jwt": { "Issuer": "JwtAuthentication.Issuer", "Audience": "JwtAuthentication.Api", "AccessTokenMinutes": 15 }
```

Compose injects `Jwt__Key` from the local `JWT_KEY` value in `.env`; production deployments should use an approved secret store.

`UI/appsettings.json` contains the host-development API URL; Compose overrides it with `http://api:8080/` so the UI reaches the API through the Compose network. [Program.cs](/Users/lteran/Documents/Workspace/dotNet/PracticalScenarios/JwtAuthentication/UI/Program.cs) uses this setting as the base address of the typed `ApiClient`.
