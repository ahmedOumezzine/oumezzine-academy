using System.Net;
using System.Text.RegularExpressions;
using OumezzineAcademy.Infrastructure.Data;
using OumezzineAcademy.Domain.Catalog;
using OumezzineAcademy.Web.Services;
using OumezzineAcademy.Application.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace OumezzineAcademy.Tests;

public sealed class CourseDeleteTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task Desktop_and_mobile_forms_delete_with_their_actual_route_and_antiforgery_token(int layout)
    {
        using var fixture = new Fixture();
        var id = await fixture.AddCourseAsync();
        var forms = DeleteForms(await fixture.Client.GetStringAsync("/admin/courses"), id);
        Assert.Equal(2, forms.Length);
        var form = forms[layout];
        Assert.Contains("method=\"post\"", form);
        Assert.Contains("type=\"submit\"", form);
        var action = Attribute(form, "action");
        Assert.Contains(id.ToString(), action);
        Assert.DoesNotContain("courseId", action, StringComparison.OrdinalIgnoreCase);
        var token = Token(form);
        Assert.NotEmpty(token);
        using var response = await fixture.Client.PostAsync(action, new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = token }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var index = WebUtility.HtmlDecode(await fixture.Client.GetStringAsync(response.Headers.Location));
        Assert.Contains("Le cours a été supprimé.", index);
        Assert.Empty(DeleteForms(index, id));
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.False(await db.StudyCourses.AnyAsync(x => x.Id == id));
        Assert.False(await db.StudyCourseTranslations.AnyAsync(x => x.CourseId == id));
        Assert.Equal(1, fixture.Cache.Invalidations);
        Assert.Single(fixture.Media.Deleted);
        Assert.False(fixture.Media.CourseExistedAtDeletion);
    }

    [Fact]
    public async Task Delete_via_get_does_not_mutate_data()
    {
        using var fixture = new Fixture();
        var id = await fixture.AddCourseAsync();
        var action = Attribute(DeleteForms(await fixture.Client.GetStringAsync("/admin/courses"), id)[0], "action");
        using var response = await fixture.Client.GetAsync(action);
        Assert.Contains(response.StatusCode, new[] { HttpStatusCode.MethodNotAllowed, HttpStatusCode.NotFound });
        await fixture.AssertRetainedAsync(id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("invalid-token")]
    public async Task Missing_or_invalid_antiforgery_is_rejected(string? token)
    {
        using var fixture = new Fixture();
        var id = await fixture.AddCourseAsync();
        var action = Attribute(DeleteForms(await fixture.Client.GetStringAsync("/admin/courses"), id)[0], "action");
        var fields = new Dictionary<string, string>();
        if (token != null) fields["__RequestVerificationToken"] = token;
        using var response = await fixture.Client.PostAsync(action, new FormUrlEncodedContent(fields));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await fixture.AssertRetainedAsync(id);
    }

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    [InlineData("99999999-9999-9999-9999-999999999999")]
    [InlineData("not-a-guid")]
    public async Task Invalid_or_missing_course_is_not_found(string id)
    {
        using var fixture = new Fixture();
        var index = await fixture.Client.GetStringAsync("/admin/courses");
        var token = Token(DeleteForms(index, AdminWebApplicationFactory.CourseId)[0]);
        using var response = await fixture.Client.PostAsync($"/admin/courses/delete/{id}", new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = token }));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(0, fixture.Cache.Invalidations);
        Assert.Empty(fixture.Media.Deleted);
    }

    [Theory]
    [InlineData("chapters", "contient encore des chapitres")]
    [InlineData("path", "utilisé dans un parcours")]
    [InlineData("required-by", "utilisé comme prérequis")]
    [InlineData("prerequisites", "possède encore des prérequis")]
    public async Task Dependencies_block_deletion_and_show_the_real_reason(string dependency, string expectedReason)
    {
        using var fixture = new Fixture();
        var id = dependency == "chapters" ? AdminWebApplicationFactory.CourseId : await fixture.AddCourseAsync();
        using (var scope = fixture.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            if (dependency == "path")
            {
                db.StudyLearningPathCourses.Add(new LearningPathCourse
                {
                    Id = Guid.NewGuid(), CourseId = id,
                    LearningPath = new LearningPath { Id = Guid.NewGuid(), Title = "Test path", Slug = "test-path", LearningPathCategory = new LearningPathCategory { Id = Guid.NewGuid(), Title = "Test", Slug = "test" } }
                });
            }
            if (dependency == "required-by") db.StudyCoursePrerequisites.Add(new() { CourseId = AdminWebApplicationFactory.CourseId, PrerequisiteCourseId = id });
            if (dependency == "prerequisites") db.StudyCoursePrerequisites.Add(new() { CourseId = id, PrerequisiteCourseId = AdminWebApplicationFactory.CourseId });
            await db.SaveChangesAsync();
        }
        var form = DeleteForms(await fixture.Client.GetStringAsync("/admin/courses"), id)[0];
        using var response = await fixture.Client.PostAsync(Attribute(form, "action"), new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = Token(form) }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var index = WebUtility.HtmlDecode(await fixture.Client.GetStringAsync(response.Headers.Location));
        Assert.Contains("role=\"alert\"", index);
        Assert.Contains(expectedReason, index);
        await fixture.AssertRetainedAsync(id);
        using var verifyScope = fixture.Factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (dependency == "path") Assert.True(await verifyDb.StudyLearningPathCourses.AnyAsync(x => x.CourseId == id));
        if (dependency == "required-by") Assert.True(await verifyDb.StudyCoursePrerequisites.AnyAsync(x => x.PrerequisiteCourseId == id));
        if (dependency == "prerequisites") Assert.True(await verifyDb.StudyCoursePrerequisites.AnyAsync(x => x.CourseId == id));
        if (dependency == "chapters")
        {
            Assert.True(await verifyDb.StudyCourseContents.AnyAsync(x => x.CourseId == id));
            Assert.True(await verifyDb.StudyCourseLessons.AnyAsync());
            Assert.True(await verifyDb.StudyCourseQuizzes.AnyAsync());
        }
    }

    [Fact]
    public async Task Shared_thumbnail_is_kept_after_successful_deletion()
    {
        using var fixture = new Fixture();
        var id = await fixture.AddCourseAsync();
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var existing = await db.StudyCourses.SingleAsync(x => x.Id == AdminWebApplicationFactory.CourseId);
        existing.Thumbnail = Fixture.Thumbnail(id);
        await db.SaveChangesAsync();
        var result = await scope.ServiceProvider.GetRequiredService<AdminCourseService>().DeleteAsync(id, default);
        Assert.True(result.Success);
        Assert.Empty(fixture.Media.Deleted);
        Assert.Equal(1, fixture.Cache.Invalidations);
    }

    [Fact]
    public async Task Media_cleanup_failure_does_not_report_a_committed_delete_as_refused()
    {
        using var fixture = new Fixture();
        var id = await fixture.AddCourseAsync();
        fixture.Media.FailCleanup = true;
        using var scope = fixture.Factory.Services.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<AdminCourseService>().DeleteAsync(id, default);
        Assert.True(result.Success);
        Assert.Equal(1, fixture.Cache.Invalidations);
        Assert.False(await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().StudyCourses.AnyAsync(x => x.Id == id));
    }

    [Fact]
    public async Task Filter_and_individual_delete_forms_are_not_nested()
    {
        using var fixture = new Fixture();
        var html = await fixture.Client.GetStringAsync("/admin/courses");
        var depth = 0;
        foreach (Match tag in Regex.Matches(html, @"</?form\b[^>]*>", RegexOptions.IgnoreCase))
        {
            if (tag.Value.StartsWith("</", StringComparison.Ordinal)) depth--;
            else { Assert.Equal(0, depth); depth++; }
            Assert.InRange(depth, 0, 1);
        }
        Assert.Equal(0, depth);
        Assert.Contains("/js/admin-delete-confirm.js", html);
        Assert.Contains("data-confirm-delete", html);
    }

    private static string[] DeleteForms(string html, Guid id) => Regex.Matches(html, @"<form\b[^>]*class=""course-delete-form""[^>]*>.*?</form>", RegexOptions.Singleline)
        .Select(m => m.Value).Where(form => Attribute(form, "action").Contains(id.ToString(), StringComparison.OrdinalIgnoreCase)).ToArray();
    private static string Attribute(string html, string name) => WebUtility.HtmlDecode(Regex.Match(html, $"\\b{name}=\"([^\"]*)\"").Groups[1].Value);
    private static string Token(string form) => Attribute(Regex.Match(form, @"<input\b[^>]*name=""__RequestVerificationToken""[^>]*>").Value, "value");

    private sealed class Fixture : IDisposable
    {
        private readonly AdminWebApplicationFactory _base = new(AdminTestProfile.Admin);
        public Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> Factory { get; }
        public HttpClient Client { get; }
        public FakeCache Cache { get; } = new();
        public FakeMedia Media { get; } = new();
        public Fixture()
        {
            Factory = _base.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            {
                services.RemoveAll<IStudyLmsCacheInvalidator>(); services.AddSingleton<IStudyLmsCacheInvalidator>(Cache);
                services.RemoveAll<IMediaStorage>(); services.AddSingleton<IMediaStorage>(Media);
            }));
            Client = Factory.CreateClient(new() { AllowAutoRedirect = false, BaseAddress = new Uri("https://localhost") });
            Media.CourseExists = id => { using var scope = Factory.Services.CreateScope(); return scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().StudyCourses.Any(x => x.Id == id); };
        }
        public static string Thumbnail(Guid id) => $"/uploads/courses/{id:D}/test.png";
        public async Task<Guid> AddCourseAsync()
        {
            using var scope = Factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var id = Guid.NewGuid();
            var course = new Course { Id = id, CourseCategoryId = await db.StudyCourseCategories.Select(x => x.Id).FirstAsync(), Title = "Deletable", Slug = id.ToString("N"), Thumbnail = Thumbnail(id), CreatedOnUtc = DateTime.UtcNow };
            course.Translations.Add(new() { Id = Guid.NewGuid(), LanguageCode = "fr", Title = "Cours à supprimer", Slug = "fr-" + id.ToString("N") });
            db.Add(course); await db.SaveChangesAsync(); return id;
        }
        public async Task AssertRetainedAsync(Guid id)
        {
            using var scope = Factory.Services.CreateScope();
            Assert.True(await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().StudyCourses.AnyAsync(x => x.Id == id));
            Assert.Equal(0, Cache.Invalidations); Assert.Empty(Media.Deleted);
        }
        public void Dispose() { Client.Dispose(); Factory.Dispose(); _base.Dispose(); }
    }
    private sealed class FakeCache : IStudyLmsCacheInvalidator
    {
        public int Invalidations;
        public Task InvalidatePublicAsync(CancellationToken token = default) { Invalidations++; return Task.CompletedTask; }
        public Task InvalidateCatalogAsync(CancellationToken token = default) => Task.CompletedTask;
    }
    private sealed class FakeMedia : IMediaStorage
    {
        public List<string?> Deleted { get; } = [];
        public Func<Guid, bool>? CourseExists;
        public bool CourseExistedAtDeletion;
        public bool FailCleanup;
        public Task<string> SaveImageAsync(MediaUpload upload, string area, Guid entityId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public bool IsSafeImagePath(string? relativePath, string area, Guid entityId) => true;
        public void DeleteIfSafe(string? relativePath, string area, Guid entityId)
        {
            CourseExistedAtDeletion = CourseExists!(entityId);
            if (FailCleanup) throw new IOException("Simulated cleanup failure");
            Assert.Equal("courses", area); Deleted.Add(relativePath);
        }
    }
}

