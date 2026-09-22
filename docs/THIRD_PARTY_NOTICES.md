# Third-party asset inventory

This is an evidence inventory, not a project-wide license grant. Review the
included license files and confirm compatibility before public redistribution.

| Dependency | Version/evidence | Inclusion | Local evidence | Status |
| --- | --- | --- | --- | --- |
| Bootstrap | 5.1.0 banner in bundled JS | Local `wwwroot/lib/bootstrap` | `wwwroot/lib/bootstrap/LICENSE` | Confirmed MIT notice |
| jQuery | Local distribution; exact version should be confirmed from its banner | Local `wwwroot/lib/jquery` | `wwwroot/lib/jquery/LICENSE.txt` | Review metadata |
| jQuery Validation | Local distribution | Local `wwwroot/lib/jquery-validation` | `LICENSE.md` | Confirmed notice present |
| jQuery Validation Unobtrusive | Local distribution | Local `wwwroot/lib/jquery-validation-unobtrusive` | `LICENSE.txt` | Confirmed notice present |
| CKEditor 5 | 48.5.0 | Local `wwwroot/lib/ckeditor5` | `LICENSE.md`, `COPYING.GPL`, `README.md` | GPL open-source route selected |

## CKEditor 5

- Version: 48.5.0.
- Distribution: locally bundled and self-hosted; no cloud or premium service is configured.
- Application route: GPL open-source route, with `licenseKey: 'GPL'` configured for each editor initialization.
- Evidence retained locally: `LICENSE.md`, `COPYING.GPL`, and the bundled `README.md`.
- The commercial CKEditor route is not selected for this repository.

## Other assets

- Fonts: no committed font files were identified; external font providers retain
  their own terms.
- Project visual assets: the assets under `src/OumezzineAcademy.Web/wwwroot/images/brand/`
  and `src/OumezzineAcademy.Web/wwwroot/images/about/about-hero-laptop.svg` were
  generated using an AI image-generation tool at the direction of the repository
  owner specifically for this project. They are not known to be copied
  third-party assets. This documents provenance only and does not claim exclusive
  copyright. AI-provider output and redistribution terms remain a separate
  review item.
- `docs/screenshots` currently contains no project screenshots.
- The application source license is GPL-2.0-or-later; see the repository-root `LICENSE`.
