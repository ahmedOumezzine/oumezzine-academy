# Oumezzine Academy

Oumezzine Academy is a portfolio project for a bilingual learning platform built with ASP.NET Core MVC. It brings together a public course catalogue, structured learning content, quizzes and an administration area for managing courses and learning paths.

The project demonstrates a localized FR/EN content model, role-protected administration, media handling and a layered application structure. It is presented as a source repository and portfolio project; no hosted production deployment is claimed here.

**Live demo:** Not configured for this standalone copy yet.

**Screenshots:** Add reviewed, privacy-safe images to [`docs/screenshots`](docs/screenshots).

## Features

- French and English routes and content localization.
- Courses, chapters, lessons, quizzes, categories and learning paths.
- Admin sign-in and role-protected content management.
- SEO metadata, canonical and hreflang links, Open Graph data, JSON-LD, `robots.txt` and `sitemap.xml`.
- HTML sanitization and validated media uploads.
- Responsive Razor views with locally hosted Bootstrap, jQuery and CKEditor assets.
- Automated .NET tests covering the public site, administration, localization, media and SEO.

## Architecture

The solution uses a ports-and-adapters approach around four projects:

```text
Web ───────────────► Application ─────► Domain
  │                       ▲
  └── composition ─► Infrastructure ──┘
```

- `OumezzineAcademy.Domain` contains framework-independent domain entities.
- `OumezzineAcademy.Application` contains neutral DTOs, commands, queries and ports.
- `OumezzineAcademy.Infrastructure` owns EF Core persistence, media storage and seed persistence.
- `OumezzineAcademy` contains MVC controllers, Razor views, ViewModels and host composition.
- `tests/OumezzineAcademy.Tests` contains integration and behavior tests.

EF Core is centralized in Infrastructure. `MediaUpload`, `IMediaStorage` and `IStudyLmsSeeder` keep application boundaries independent from HTTP and filesystem details.

## Technology

- .NET 9 / ASP.NET Core MVC and Razor
- Entity Framework Core with SQL Server
- ASP.NET Core Identity
- xUnit and `WebApplicationFactory`
- Optional Playwright browser tests in `tests/OumezzineAcademy.Tests/Browser`

The application code, data model, services, views, resources and EF migrations live under `src/OumezzineAcademy`. The automated .NET tests live under `tests/OumezzineAcademy.Tests`.

## Prerequisites

- .NET 9 SDK
- SQL Server for normal application use (local SQL Server, LocalDB, or a separately provisioned SQL Server instance)
- Node.js, npm and Chromium only if running the optional browser tests

## Configuration

Copy `src/OumezzineAcademy.Web/appsettings.Example.json` to `src/OumezzineAcademy.Web/appsettings.Development.json` for local development, or set environment variables. Replace every placeholder before use and never commit real credentials.

The application reads the database connection from `ConnectionStrings:DefaultConnection`, which maps to `ConnectionStrings__DefaultConnection` as an environment variable. The initial Admin account is optional and is read from `Admin:Email` and `Admin:Password` (environment variables `Admin__Email` and `Admin__Password`), with `LEARNWEBAPP_ADMIN_EMAIL` and `LEARNWEBAPP_ADMIN_PASSWORD` also supported.

Set the connection string before running EF commands. The application creates the Admin role at startup. If both Admin credentials are configured, it creates that account if needed and adds it to the Admin role. Do not use a real account password in local files or shell history.

For development, User Secrets are appropriate:

```sh
dotnet user-secrets init --project src/OumezzineAcademy.Web/OumezzineAcademy.Web.csproj
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\\MSSQLLocalDB;Database=OumezzineAcademyDev;Trusted_Connection=True;TrustServerCertificate=True" --project src/OumezzineAcademy.Web/OumezzineAcademy.Web.csproj
dotnet user-secrets set "Admin:Email" "admin@example.test" --project src/OumezzineAcademy.Web/OumezzineAcademy.Web.csproj
dotnet user-secrets set "Admin:Password" "Use-a-local-development-password-here" --project src/OumezzineAcademy.Web/OumezzineAcademy.Web.csproj
```

## Database setup

Create a new, empty SQL Server database and configure `ConnectionStrings__DefaultConnection`. The extracted copy includes an `InitialCreate` migration generated from the current `ApplicationDbContext` model. It creates the application catalogue, translations and Identity tables.

From the repository root, restore the pinned EF tool and initialize the new database:

```sh
dotnet tool restore
dotnet ef database update --project src/OumezzineAcademy.Infrastructure/OumezzineAcademy.Infrastructure.csproj --startup-project src/OumezzineAcademy.Web/OumezzineAcademy.Web.csproj -- --environment DesignTime
```

The `DesignTime` environment skips startup role/account seeding while EF applies the migration. Start the app afterward with the intended runtime environment and Admin configuration. This initial migration targets a new standalone database; do not apply it to an existing OumezzineAcademy or shared database. No demo content is seeded automatically.

The optional development Study LMS sample data uses the `IStudyLmsSeeder` contract and is separate from Admin Identity bootstrap. It is gated by the development startup path and is not a production data migration.

User-uploaded media is written under `src/OumezzineAcademy.Web/wwwroot/uploads`. The repository keeps only a placeholder there; configure persistent storage and backups for a real deployment.

## Build and test

```sh
dotnet restore OumezzineAcademy.sln
dotnet build OumezzineAcademy.sln -c Release
dotnet test tests/OumezzineAcademy.Tests/OumezzineAcademy.Tests.csproj -c Release
```

Final validation covered 50 targeted tests with no failures. The combined suite was inconclusive in the validation environment because the testhost stopped producing output; this is not reported as a passing full-suite result. Optional browser tests:

```sh
cd tests/OumezzineAcademy.Tests/Browser
npm ci
npx playwright install chromium
npm test
```

## Deployment

The app can be published as an ASP.NET Core application and hosted behind IIS or another supported ASP.NET Core host. Supply secrets through the hosting environment and use durable storage for uploads. Machine-specific publish profiles are intentionally excluded.

## Licensing

The application source code is licensed under the GNU General Public License
v2.0 or later (`GPL-2.0-or-later`); see [LICENSE](LICENSE). Third-party
components remain governed by their own licenses. The locally bundled CKEditor
5 distribution uses its documented open-source GPL route; see
[docs/THIRD_PARTY_NOTICES.md](docs/THIRD_PARTY_NOTICES.md).

The included Oumezzine Academy logos and illustrations were generated with an
AI image-generation tool at the direction of the repository owner specifically
for this project; they are not known to be copied third-party assets. This
documents provenance only, while AI-provider output terms remain subject to
review. Also obtain a license statement for the
`AhmedOumezzine.EFCore.Repository` package before public release.

See [`docs/PUBLICATION_READINESS.md`](docs/PUBLICATION_READINESS.md) and [`docs/THIRD_PARTY_NOTICES.md`](docs/THIRD_PARTY_NOTICES.md) for the pre-publication checklist and locally documented third-party asset evidence.
