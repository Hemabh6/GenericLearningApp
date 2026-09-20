# GenericLearningApp

A study workspace: notes, a watch-and-read list, a phased roadmap and practice tests,
one subject at a time. ASP.NET Core 10 + Blazor Server, EF Core on PostgreSQL.

## Run it locally

You need the .NET 10 SDK and Docker Desktop (for PostgreSQL).

```bash
# 1. Start PostgreSQL on localhost:5432
docker compose -f compose.dev.yaml up -d

# 2. Run. The app applies both migrations (Identity, and the learning aggregates) on start.
dotnet run --project src/GenericLearningApp.Web
```

To run the migrations yourself instead, set `Database:MigrateOnStartup` to `false` and use
`dotnet ef database update -c ApplicationDbContext -p src/GenericLearningApp.Web -s src/GenericLearningApp.Web`
and `dotnet ef database update -c LearningDbContext -p src/GenericLearningApp.Infrastructure -s src/GenericLearningApp.Web`.

Then open the URL it prints, register an account, and create a subject.
Confirmation emails are written to the console, not sent: the registration page
links straight to the confirmation.

`dotnet ef` missing? `dotnet tool install --global dotnet-ef`.

### Without Docker

Point `ConnectionStrings:DefaultConnection` in
`src/GenericLearningApp.Web/appsettings.Development.json` at any PostgreSQL you have,
then run steps 2 and 3.

### Admin area and the super admin

`/admin` manages the site: name, sign-in rules and top menus (published together with Publish / Discard),
people and their roles, per-person menu access, and roles with permissions. It opens only for someone
whose role holds an admin permission.

The **super admin** is set in configuration and can never be removed, demoted or deleted from the admin
area (the rules are enforced in the server code, not just by hiding buttons, and the "delete my data" page
refuses it too). Set it before first use:

```bash
dotnet user-secrets set "Admin:SuperAdminEmail" "you@example.com" --project src/GenericLearningApp.Web
dotnet user-secrets set "Admin:SuperAdminPassword" "a-strong-Password1!" --project src/GenericLearningApp.Web
```

On start, if that email has no account it is created with that password (so nobody can register it first);
if it already has one, that account is promoted. In the container set `SUPERADMIN_EMAIL` and
`SUPERADMIN_PASSWORD` in `.env`. To move the role to someone else you must edit the database
(`AspNetUsers.IsSuperAdmin`); nothing in the app can do it.

Invites can't be emailed yet (no email service is configured), so the People page shows a one-time link
for the admin to pass on.

### Google and Microsoft sign-in

Optional. The sign-in page shows a button only for a provider whose client id and secret are set
(in development, greyed-out buttons show where they will go).

```bash
dotnet user-secrets set "Authentication:Google:ClientId" "<id>" --project src/GenericLearningApp.Web
dotnet user-secrets set "Authentication:Google:ClientSecret" "<secret>" --project src/GenericLearningApp.Web
dotnet user-secrets set "Authentication:Microsoft:ClientId" "<id>" --project src/GenericLearningApp.Web
dotnet user-secrets set "Authentication:Microsoft:ClientSecret" "<secret>" --project src/GenericLearningApp.Web
```

Register the redirect URI `<your site>/signin-google` (Google Cloud Console, OAuth client) and
`<your site>/signin-microsoft` (Microsoft Entra, app registration). Locally that is
`http://localhost:5055/signin-google`. In the container, set `GOOGLE_CLIENT_ID` and friends in `.env`.

### Guest accounts

"Try it as a guest" creates a real account with no email or password and signs it in with a
long-lived cookie. Its data is stored like anyone else's, but the cookie is the only way back in:
clear it and that guest's data is unreachable. Guest rows (`UserName` starting `guest-`) are not
cleaned up automatically yet.

## Branches

| Branch | Purpose |
| --- | --- |
| `main` | Development. Feature branches are merged here by pull request. |
| `stg` | Staging. Promoted from `main` by pull request; deployed to the staging server. |
| `master` | Production. Promoted from `stg` by pull request; deployed to the production server. |

Changes move `feature/*` → `main` → `stg` → `master`, one pull request at a time, merged with a merge
commit (not squash). CI builds, tests and builds the Docker image on all three. Details, and how each
server follows its branch, are in [deploy/DEPLOY.md](deploy/DEPLOY.md#branches-and-environments).

## Tests

```bash
dotnet test
```

Domain tests are pure. Application tests run against a real SQLite database created
per test, because the model uses relational mapping the in-memory provider rejects.

## Deploy

Target: one always-on box (Oracle Cloud Always Free, Ampere A1, Mumbai or Hyderabad).
A Blazor Server circuit is a long-lived WebSocket, so the app cannot scale to zero.

```bash
cp .env.example .env    # set POSTGRES_PASSWORD, SITE_ADDRESS, SUPERADMIN_EMAIL, SUPERADMIN_PASSWORD
docker compose up -d --build
```

That brings up the app, PostgreSQL, Caddy (TLS) and a nightly `pg_dump` kept for 14 days.

**Full walkthrough, from creating the server to off-site backups and disaster recovery:
[deploy/DEPLOY.md](deploy/DEPLOY.md).**

## Layout

| Project | Holds |
| --- | --- |
| `Domain` | Entities and their invariants. No EF, no framework. |
| `Application` | Use-case interfaces and DTOs. |
| `Infrastructure` | EF Core, migrations, service implementations. |
| `Web` | Blazor Server UI, Identity, health endpoint. |
