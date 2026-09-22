Lesson rich text editor browser regression tests
==============================================

From this directory:

    npm ci
    npx playwright install chromium
    npm test

An existing Chromium installation can be selected with the environment variable
PLAYWRIGHT_CHROMIUM_EXECUTABLE (absolute executable path).

The runner first executes LessonEditorHttpTests to export real Create, Edit and
validation-error Razor pages into obj/fixtures. Those tests use the existing
AdminWebApplicationFactory with its isolated in-memory SQLite database and test
authentication. No real database, account, credentials, or lesson records are used.

Playwright serves these pages and the application's actual wwwroot assets on
127.0.0.1:4197 under a CSP allowing only same-origin scripts, styles and images.
It tests CKEditor in Chromium, including language switching, native form data,
preview, word count, formatting, links, paste filtering, failure fallback, compact
height and overflow at widths 375, 768 and 1440. HTTP persistence and antiforgery
are exercised separately by LessonEditorHttpTests, not by the static browser host.
Photo tests intercept the existing upload endpoint to verify multipart submission,
pending state, multiple images, language separation and failure recovery. The HTTP
tests exercise the real endpoint, file validation and public lesson detail rendering.

Outputs and screenshots are kept in the ignored obj directory.
