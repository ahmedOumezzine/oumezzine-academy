CKEditor 5, version 48.5.0. These unmodified browser assets are served locally.

Source: https://registry.npmjs.org/ckeditor5/-/ckeditor5-48.5.0.tgz
Upstream source: https://github.com/ckeditor/ckeditor5/tree/v48.5.0

Reproduce from the repository with:

    pwsh -File src/OumezzineAcademy.Web/Tools/Sync-LessonEditor.ps1

The maintenance script requires Node/npm and tar and checks the pinned archive
SHA-512 before copying assets. Source maps
are intentionally omitted; they are not needed at runtime. No npm install or
network connection is needed to build, publish, or use the application.

Licensing: see LICENSE.md and COPYING.GPL. The lesson editor uses licenseKey: 'GPL'.
Use this mode only under the applicable GPL terms. A proprietary deployment
that cannot comply requires a CKSource commercial license and corresponding
self-hosted license configuration. No commercial entitlement is bundled.

The lesson photo button uses FileDialogButtonView and the application's existing
authenticated, antiforgery-protected lesson upload endpoint. Images are inserted
only after upload using their local URL, without a Base64 adapter or blob preview.
No cloud services, media embeds, inline-style formatting, or premium features are
loaded. The unchanged server sanitizer remains authoritative.
