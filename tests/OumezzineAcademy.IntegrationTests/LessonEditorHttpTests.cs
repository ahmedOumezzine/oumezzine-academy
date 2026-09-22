using AngleSharp.Html.Parser;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Infrastructure.Data;
using OumezzineAcademy.Infrastructure.Media;
using OumezzineAcademy.Infrastructure.Sanitization;
using OumezzineAcademy.Web.Services;
using System.Net;
using Xunit;

namespace OumezzineAcademy.Tests;

public sealed class LessonEditorHttpTests
{
    private static readonly string Lessons = $"/admin/courses/{AdminWebApplicationFactory.CourseId}/chapters/{AdminWebApplicationFactory.ChapterId}/lessons";

    [Fact]
    public async Task Create_edit_and_validation_preserve_separate_rich_html_through_real_posts()
    {
        using var factory = new AdminWebApplicationFactory(AdminTestProfile.Admin);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var createResponse = await client.GetAsync($"{Lessons}/create");
        createResponse.EnsureSuccessStatusCode();
        var create = await createResponse.Content.ReadAsStringAsync();
        var document = new HtmlParser().ParseDocument(create);
        Assert.Equal(2, document.QuerySelectorAll("textarea[data-lesson-editor]").Length);
        Assert.All(document.QuerySelectorAll("textarea[data-lesson-editor]"), field => Assert.Empty(field.TextContent));
        Assert.All(document.QuerySelectorAll("script[src],link[rel=stylesheet]"), asset =>
            Assert.StartsWith("/", asset.GetAttribute("src") ?? asset.GetAttribute("href")));
        var csp = createResponse.Headers.GetValues("Content-Security-Policy").Single();
        Assert.DoesNotContain("unsafe-inline", csp);
        Assert.DoesNotContain("unsafe-eval", csp);
        ExportFixture("create", create);

        const string french = "<h2>Leçon française</h2><p><strong>Bonjour</strong> <em>élève</em> <u>ici</u></p><ul><li>Premier</li><li>Deuxième</li></ul><blockquote><p>Citation</p></blockquote><pre><code>const x = 1;</code></pre><a href=\"https://example.com\" target=\"_blank\" rel=\"noopener noreferrer\">Source</a><figure><img src=\"/favicon.ico\" alt=\"Existant\"><figcaption>Légende</figcaption></figure><table><tbody><tr><td>Cellule</td></tr></tbody></table>";
        const string english = "<h3>English lesson</h3><p>Independent <strong>English</strong> text.</p><ol><li>One</li></ol>";
        var values = FormValues(create, french + "<script>alert(1)</script><p onclick=\"bad()\">Safe</p>", english);
        using var posted = await client.PostAsync($"{Lessons}/create", new FormUrlEncodedContent(values));
        Assert.Equal(HttpStatusCode.Redirect, posted.StatusCode);

        Guid id;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var lesson = await db.StudyCourseLessons.Include(x => x.Translations).SingleAsync(x => x.Slug == "editor-fr");
            id = lesson.Id;
            var fr = lesson.Translations.Single(x => x.LanguageCode == "fr").ContentHtml!;
            Assert.Contains("<strong>Bonjour</strong>", fr);
            Assert.Contains("<ul><li>Premier</li>", fr);
            Assert.DoesNotContain("script", fr, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("onclick", fr);
            Assert.Equal(english, lesson.Translations.Single(x => x.LanguageCode == "en").ContentHtml);
        }
        var edit = await client.GetStringAsync($"{Lessons}/{id}/edit");
        document = new HtmlParser().ParseDocument(edit);
        Assert.Contains("<h2>Leçon française</h2>", document.QuerySelector("#French-ContentHtml")!.TextContent);
        Assert.Equal(english, document.QuerySelector("#English-ContentHtml")!.TextContent);
        ExportFixture("edit", edit);

        const string changed = "<h4>Modifié</h4><p><strong>Nouveau texte</strong></p>";
        values = FormValues(edit, changed, english);
        using var saved = await client.PostAsync($"{Lessons}/{id}/edit", new FormUrlEncodedContent(values));
        Assert.Equal(HttpStatusCode.Redirect, saved.StatusCode);
        using (var scope = factory.Services.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<AdminLessonService>();
            var model = await service.GetFormAsync(AdminWebApplicationFactory.CourseId, AdminWebApplicationFactory.ChapterId, id, CancellationToken.None);
            Assert.Equal(changed, model!.French.ContentHtml);
            Assert.Equal(english, model.English.ContentHtml);
        }

        values["French.VideoUrl"] = "invalid";
        values["French.ContentHtml"] = "<p>Unsaved correction</p>";
        using var invalid = await client.PostAsync($"{Lessons}/{id}/edit", new FormUrlEncodedContent(values));
        Assert.Equal(HttpStatusCode.OK, invalid.StatusCode);
        var validation = await invalid.Content.ReadAsStringAsync();
        document = new HtmlParser().ParseDocument(validation);
        Assert.Equal(values["French.ContentHtml"], document.QuerySelector("#French-ContentHtml")!.TextContent);
        Assert.Equal(english, document.QuerySelector("#English-ContentHtml")!.TextContent);
        ExportFixture("validation", validation);
    }

    [Theory]
    [InlineData("<p><strong>Bold</strong> <em>Italic</em> <u>Underline</u></p>")]
    [InlineData("<h2>Two</h2><h3>Three</h3><h4>Four</h4>")]
    [InlineData("<ul><li>Bullet</li></ul><ol><li>Number</li></ol>")]
    [InlineData("<blockquote><p>Quote</p></blockquote><pre><code>x &lt; y</code></pre>")]
    [InlineData("<a href=\"https://example.com\" target=\"_blank\" rel=\"noopener noreferrer\">Source</a>")]
    [InlineData("<figure class=\"table\"><table><tbody><tr><td>Cell</td></tr></tbody></table></figure>")]
    public void Sanitizer_keeps_editor_formatting(string html)
    {
        var sanitizer = new HtmlSanitizerService();
        Assert.Equal(html, sanitizer.Sanitize(html));
        var unsafeHtml = sanitizer.Sanitize(html + "<script>alert(1)</script><img src=\"data:image/png;base64,AAAA\" onerror=\"bad()\"><a href=\"javascript:bad()\">Bad</a>");
        Assert.DoesNotContain("<script", unsafeHtml);
        Assert.DoesNotContain("onerror", unsafeHtml);
        Assert.DoesNotContain("javascript:", unsafeHtml);
        Assert.DoesNotContain("data:image", unsafeHtml);
    }

    private static Dictionary<string, string> FormValues(string html, string french, string english)
    {
        var document = new HtmlParser().ParseDocument(html);
        return new()
        {
            ["__RequestVerificationToken"] = document.QuerySelector("input[name=__RequestVerificationToken]")!.GetAttribute("value")!,
            ["French.Title"] = "Éditeur FR",
            ["French.Slug"] = "editor-fr",
            ["French.ContentHtml"] = french,
            ["English.Title"] = "Editor EN",
            ["English.Slug"] = "editor-en",
            ["English.ContentHtml"] = english,
            ["Order"] = "2"
        };
    }

    [Fact]
    public async Task Lesson_photo_upload_is_protected_validated_and_persists_in_language_content()
    {
        var root = Path.Combine(Path.GetTempPath(), "LearnEditorPhotos", Guid.NewGuid().ToString("N"));
        try
        {
            using var factory = new AdminWebApplicationFactory(AdminTestProfile.Admin);
            using var configured = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IMediaStorage>();
                services.AddSingleton<IMediaStorage>(new FileSystemMediaStorage(root));
            }));
            using var client = configured.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            const string id = "50000000-0000-0000-0000-000000000001";
            var edit = await client.GetStringAsync($"{Lessons}/{id}/edit");
            var document = new HtmlParser().ParseDocument(edit);
            var uploadUrl = document.QuerySelector("form.lesson-concept-form")!.GetAttribute("data-photo-upload-url")!;
            Assert.Equal($"/image/{id}", uploadUrl);
            var token = FormValues(edit, "", "")["__RequestVerificationToken"];

            using var missingToken = await client.PostAsync(uploadUrl, PhotoForm(null));
            Assert.Equal(HttpStatusCode.BadRequest, missingToken.StatusCode);
            using var invalid = await client.PostAsync(uploadUrl, PhotoForm(token, [1, 2, 3]));
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
            using var missingLesson = await client.PostAsync($"/image/{Guid.NewGuid()}", PhotoForm(token));
            Assert.Equal(HttpStatusCode.NotFound, missingLesson.StatusCode);
            Assert.False(Directory.Exists(root));

            using var uploaded = await client.PostAsync(uploadUrl, PhotoForm(token));
            uploaded.EnsureSuccessStatusCode();
            using var json = System.Text.Json.JsonDocument.Parse(await uploaded.Content.ReadAsStringAsync());
            var url = json.RootElement.GetProperty("url").GetString()!;
            Assert.StartsWith($"/uploads/lessons/{id}/", url);
            Assert.True(File.Exists(Path.Combine(root, url.TrimStart('/'))));
            var values = FormValues(edit, $"<p>Photo française</p><figure class=\"image\"><img src=\"{url}\" alt=\"Diagramme\"><figcaption>Légende</figcaption></figure>", "<p>English remains separate</p>");
            values["French.Summary"] = "Leçon illustrée";
            values["French.PublicationStatus"] = "Published";
            using var saved = await client.PostAsync($"{Lessons}/{id}/edit", new FormUrlEncodedContent(values));
            Assert.Equal(HttpStatusCode.Redirect, saved.StatusCode);
            var reloaded = new HtmlParser().ParseDocument(await client.GetStringAsync($"{Lessons}/{id}/edit"));
            Assert.Contains(url, reloaded.QuerySelector("#French-ContentHtml")!.TextContent);
            Assert.Contains("<figcaption>Légende</figcaption>", reloaded.QuerySelector("#French-ContentHtml")!.TextContent);
            Assert.Equal("<p>English remains separate</p>", reloaded.QuerySelector("#English-ContentHtml")!.TextContent);
            using (var scope = configured.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.StudyCourseTranslations.Add(new OumezzineAcademy.Domain.Catalog.CourseTranslation
                {
                    Id = Guid.NewGuid(),
                    CourseId = AdminWebApplicationFactory.CourseId,
                    LanguageCode = "fr",
                    Title = "Cours illustré",
                    Slug = "cours-illustre",
                    PublicationStatus = OumezzineAcademy.Domain.Catalog.StudyStatus.Published
                });
                await db.SaveChangesAsync();
            }
            var detail = new HtmlParser().ParseDocument(await client.GetStringAsync("/fr/lecons/editor-fr"));
            var publicPhoto = detail.QuerySelector(".lesson-content-card img");
            Assert.NotNull(publicPhoto);
            Assert.Equal(url, publicPhoto.GetAttribute("src"));
            Assert.Equal("Diagramme", publicPhoto.GetAttribute("alt"));
            Assert.Equal("Légende", detail.QuerySelector(".lesson-content-card figcaption")!.TextContent);

            // The original LessonImage field now inserts its uploaded image and continues into Edit.
            var create = await client.GetStringAsync($"{Lessons}/create");
            using var createForm = PhotoForm(FormValues(create, "", "")["__RequestVerificationToken"], fieldName: "LessonImage");
            foreach (var pair in new Dictionary<string, string> { ["French.Title"] = "Photo", ["French.Slug"] = "photo-create", ["French.ContentHtml"] = "<p>Intro</p>", ["continueEditing"] = "true" })
                createForm.Add(new StringContent(pair.Value), pair.Key);
            using var created = await client.PostAsync($"{Lessons}/create", createForm);
            Assert.Equal(HttpStatusCode.Redirect, created.StatusCode);
            Assert.EndsWith("/edit", created.Headers.Location!.ToString());
            var newEdit = new HtmlParser().ParseDocument(await client.GetStringAsync(created.Headers.Location));
            Assert.Contains("/uploads/lessons/", newEdit.QuerySelector("#French-ContentHtml")!.TextContent);
            Assert.Contains("<p>Intro</p>", newEdit.QuerySelector("#French-ContentHtml")!.TextContent);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    private static MultipartFormDataContent PhotoForm(string? token, byte[]? bytes = null, string fieldName = "file")
    {
        var form = new MultipartFormDataContent();
        if (token is not null) form.Add(new StringContent(token), "__RequestVerificationToken");
        var image = new ByteArrayContent(bytes ?? Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+a9HkAAAAASUVORK5CYII="));
        image.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        form.Add(image, fieldName, "diagram.png");
        return form;
    }

    private static void ExportFixture(string name, string html)
    {
        // Opt-in browser fixtures use the actual Razor output with SQLite test data, never a live database.
        if (Environment.GetEnvironmentVariable("LEARN_EDITOR_BROWSER_FIXTURES") is not { Length: > 0 } path) return;
        Directory.CreateDirectory(path);
        File.WriteAllText(Path.Combine(path, $"{name}.html"), html);
    }
}