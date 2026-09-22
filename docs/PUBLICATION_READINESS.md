# Publication readiness

## Completed

- Clean Architecture extraction for catalog, administration, media and seeding.
- No real application database or media persistence in Web services/controllers.
- Admin authorization, antiforgery, secure cookies, CSP, HSTS and upload validation reviewed.
- Shareable configuration files contain no real credentials.
- Release build: 0 errors and 0 warnings.
- 50 targeted validation tests passed.
- Generated files and runtime uploads are covered by `.gitignore`.

## Review required before public release

- Repository license: RESOLVED — GPL-2.0-or-later; see the repository-root `LICENSE`.
- CKEditor 5 48.5.0 licensing route: RESOLVED — locally bundled GPL route with `licenseKey: 'GPL'`; evidence is retained beside the assets.
- Brand/image provenance: PROVENANCE CONFIRMED — AI-GENERATED FOR PROJECT.
- AI PROVIDER OUTPUT TERMS: REVIEW REQUIRED; the provider/tool and its redistribution terms are not independently documented here.
- Run the NuGet vulnerability check from an environment with usable NuGet configuration.
- Run the EF pending-model check after restoring the pinned `dotnet-ef` tool.
- Optionally rerun the complete .NET suite in a stable environment; the combined run previously hung at the testhost level.
- Add only real, privacy-safe screenshots under `docs/screenshots` if desired.

The blocked checks are validation-environment limitations, not claims of application failure. No database update, migration generation, commit or push was performed during validation.

## Pre-publication checklist

- [x] Choose the application/repository license: GPL-2.0-or-later.
- [x] Resolve CKEditor licensing: local GPL route.
- [x] Resolve brand/image provenance: AI-generated for this project by the repository owner.
- [ ] Verify AI-provider output and redistribution terms.
- [ ] Run NuGet vulnerability validation.
- [ ] Run `dotnet ef migrations has-pending-model-changes`.
- [ ] Rerun the complete test suite.
- [ ] Review configuration and secret files.
- [ ] Capture actual screenshots, if useful.
- [ ] Inspect the final diff before committing or publishing.

## Portfolio description

Oumezzine Academy is a bilingual French/English ASP.NET Core MVC learning platform built as a portfolio project. It combines a localized course catalogue, chapters, lessons, quizzes and learning paths with a role-protected administration area for content, translations, publication states, prerequisites, ordering and media uploads. The solution uses EF Core, ASP.NET Core Identity and a layered Domain/Application/Infrastructure/Web structure with neutral ports for persistence, storage and seeding. Security-focused validation covers antiforgery, secure cookies, upload signatures, path traversal protection, HTML sanitization and response headers.

## Suggested GitHub description

`Bilingual ASP.NET Core MVC LMS with Clean Architecture, EF Core, Identity, localized content, admin workflows and secure media handling.`

## Suggested topics

`aspnet-core`, `dotnet`, `mvc`, `entity-framework-core`, `clean-architecture`, `sql-server`, `identity`, `lms`, `localization`
