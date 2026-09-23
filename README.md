# Oumezzine Academy

Oumezzine Academy is a bilingual French/English learning platform built with ASP.NET Core MVC, Entity Framework Core, SQL Server, ASP.NET Core Identity and a layered architecture.

🌐 **Live demo:** https://cours.oumezzine.com/fr/

📸 **Screenshots:** [Deployed site gallery](docs/screenshots/README.md)

📚 **Documentation:** [Documentation index](docs/README.md)

## Features

- Courses, categories, chapters, lessons and quizzes.
- Learning paths and prerequisites.
- Localized French and English content.
- Protected Admin area for content management.
- Media management and publication states.
- SEO metadata, sitemap, robots.txt, Open Graph and JSON-LD.
- Responsive Razor views and automated tests.

## Architecture

```text
Domain
  ↑
Application
  ↑
Infrastructure

Web ─────► Application
Web ─────► Infrastructure
Web ─────► Domain (where required)
```

- `src/OumezzineAcademy.Domain` contains framework-independent domain entities and concepts.
- `src/OumezzineAcademy.Application` contains DTOs, commands, queries and application ports.
- `src/OumezzineAcademy.Infrastructure` contains EF Core persistence, migrations, Identity persistence, media storage and technical adapters.
- `src/OumezzineAcademy.Web` contains MVC controllers, Razor views, ViewModels and host composition.

## Technology and prerequisites

- .NET 9 SDK
- SQL Server, LocalDB or another configured SQL Server instance
- Node.js and Chromium only for the optional browser tests

## Configuration and database

Configure `ConnectionStrings:DefaultConnection` and the optional Admin settings through user secrets or environment variables. Never commit real credentials. See `src/OumezzineAcademy.Web/appsettings.Example.json` for placeholders.

EF migrations are stored in Infrastructure. The initial database can be created with:

```sh
dotnet tool restore
dotnet ef database update --project src/OumezzineAcademy.Infrastructure/OumezzineAcademy.Infrastructure.csproj --startup-project src/OumezzineAcademy.Web/OumezzineAcademy.Web.csproj -- --environment DesignTime
```

## Build and test

```sh
dotnet restore OumezzineAcademy.sln
dotnet build OumezzineAcademy.sln -c Release
dotnet test OumezzineAcademy.sln -c Release
```

The solution contains these test projects:

- `tests/OumezzineAcademy.Testing`
- `tests/OumezzineAcademy.Domain.Tests`
- `tests/OumezzineAcademy.Application.Tests`
- `tests/OumezzineAcademy.Infrastructure.Tests`
- `tests/OumezzineAcademy.Web.Tests`
- `tests/OumezzineAcademy.IntegrationTests`

The Release build passes. The combined test run has previously remained active in the HTTP test host, so it should be rechecked before claiming a fully passing suite.

Optional browser tests:

```sh
cd tests/OumezzineAcademy.IntegrationTests/Browser
npm ci
npx playwright install chromium
npm test
```

## Deployment

The Web project can be published to IIS or another ASP.NET Core host. Provide secrets through the hosting environment and use durable storage for uploaded media.

## Licensing

The application source code is licensed under [GPL-2.0-or-later](LICENSE). Third-party components retain their own licenses; see [docs/THIRD_PARTY_NOTICES.md](docs/THIRD_PARTY_NOTICES.md).

Licensing for `AhmedOumezzine.EFCore.Repository` remains under review. See [docs/PUBLICATION_READINESS.md](docs/PUBLICATION_READINESS.md) for the publication checklist.
