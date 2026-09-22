using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using OumezzineAcademy.Infrastructure.Data;
using OumezzineAcademy.Infrastructure.Persistence;
using OumezzineAcademy.Web.Services;
using System.Data.Common;
using System.Net;
using System.Text.RegularExpressions;
using Xunit;

namespace OumezzineAcademy.Tests;

public sealed class CourseVisibilityTests
{
    public static IEnumerable<object?[]> PublicationCases()
    {
        foreach (var language in new[] { "fr", "en" })
        {
            yield return [language, StudyStatus.Published, StudyStatus.Published, true];
            yield return [language, StudyStatus.Published, StudyStatus.Draft, false];
            yield return [language, StudyStatus.Published, StudyStatus.Archived, false];
            yield return [language, StudyStatus.Published, null, false];
            yield return [language, StudyStatus.Draft, StudyStatus.Published, false];
            yield return [language, StudyStatus.Archived, StudyStatus.Published, false];
            yield return [language, StudyStatus.Draft, StudyStatus.Draft, false];
            yield return [language, StudyStatus.Draft, null, false];
        }
    }

    [Theory]
    [MemberData(nameof(PublicationCases))]
    public async Task Admin_matches_public_catalogue_home_and_details_without_lessons(
        string language, StudyStatus courseStatus, StudyStatus? translationStatus, bool expectedVisible)
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateDatabase(connection);
        await db.Database.EnsureCreatedAsync();
        // An unpublished, untranslated category and zero lessons are deliberately valid here.
        var course = CreateCourse(courseStatus);
        if (translationStatus.HasValue) AddTranslation(course, language, translationStatus.Value);
        AddTranslation(course, language == "fr" ? "en" : "fr", StudyStatus.Published);
        db.Add(course);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var rows = await new EfAdminCourseQueries(db).ListAsync(null, null, null, null, null, null, 1, 10, default);
        var item = Assert.Single(rows.Items);
        var visibility = CourseVisibilityPolicy.Evaluate(item.CourseStatus, language == "fr" ? item.FrenchStatus : item.EnglishStatus, language);
        Assert.Equal(translationStatus, language == "fr" ? item.FrenchStatus : item.EnglishStatus);
        Assert.Equal(expectedVisible, visibility.IsVisible);
        Assert.Equal(expectedVisible, visibility.Reasons.Count == 0);

        var catalogue = new CourseCatalogService(new EfCourseCatalogQueries(db), new FixedLanguage(language));
        var search = await catalogue.SearchCoursesAsync(null, null, null, null, null, 1, 9);
        var detail = await catalogue.GetCourseAsync($"{language}-{course.Id:N}");
        var home = await catalogue.GetHomeAsync();
        Assert.Equal(expectedVisible, search.Courses.Any(c => c.Id == course.Id));
        Assert.Equal(expectedVisible, detail != null);
        Assert.Equal(expectedVisible ? 1 : 0, home.CourseCount);
        Assert.Equal(expectedVisible, home.LatestCourses.Any(c => c.Id == course.Id));
        if (detail != null) Assert.Equal(0, detail.LessonCount);
    }

    [Theory]
    [InlineData("fr", StudyStatus.Published, null, "Aucune traduction française.")]
    [InlineData("en", StudyStatus.Published, null, "Aucune traduction anglaise.")]
    [InlineData("fr", StudyStatus.Published, StudyStatus.Draft, "La traduction française est en brouillon.")]
    [InlineData("en", StudyStatus.Published, StudyStatus.Draft, "La traduction anglaise est en brouillon.")]
    [InlineData("en", StudyStatus.Published, StudyStatus.Archived, "La traduction anglaise est archivée.")]
    [InlineData("fr", StudyStatus.Draft, StudyStatus.Published, "Le statut global du cours est en brouillon.")]
    [InlineData("en", StudyStatus.Archived, StudyStatus.Published, "Le cours est archivé au niveau global.")]
    public void Hidden_reasons_describe_the_actual_publication_gate(
        string language, StudyStatus courseStatus, StudyStatus? translationStatus, string reason)
    {
        var result = CourseVisibilityPolicy.Evaluate(courseStatus, translationStatus, language);
        Assert.False(result.IsVisible);
        Assert.Equal(reason, Assert.Single(result.Reasons));
    }

    [Fact]
    public void All_blocking_reasons_are_reported()
    {
        var result = CourseVisibilityPolicy.Evaluate(StudyStatus.Draft, StudyStatus.Draft, "en");
        Assert.Equal(new[] { "Le statut global du cours est en brouillon.", "La traduction anglaise est en brouillon." }, result.Reasons);
    }

    [Fact]
    public async Task List_uses_two_queries_for_both_one_and_fifty_courses()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var counter = new QueryCounter();
        await using var db = CreateDatabase(connection, counter);
        await db.Database.EnsureCreatedAsync();
        for (var i = 0; i < 50; i++)
        {
            var course = CreateCourse(StudyStatus.Published);
            AddTranslation(course, "fr", StudyStatus.Published);
            AddTranslation(course, "en", StudyStatus.Draft);
            db.Add(course);
        }
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        foreach (var pageSize in new[] { 1, 50 })
        {
            counter.Count = 0;
            var result = await new EfAdminCourseQueries(db).ListAsync(null, null, null, null, null, null, 1, pageSize, default);
            Assert.Equal(pageSize, result.Items.Count);
            Assert.Equal(50, result.TotalCount);
            Assert.All(result.Items, item => { Assert.True(CourseVisibilityPolicy.Evaluate(item.CourseStatus, item.FrenchStatus, "fr").IsVisible); Assert.False(CourseVisibilityPolicy.Evaluate(item.CourseStatus, item.EnglishStatus, "en").IsVisible); });
            Assert.Equal(2, counter.Count);
        }
    }

    [Fact]
    public async Task Title_fallback_and_editorial_filters_are_preserved()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateDatabase(connection);
        await db.Database.EnsureCreatedAsync();
        var french = CreateCourse(StudyStatus.Published);
        AddTranslation(french, "fr", StudyStatus.Draft);
        AddTranslation(french, "en", StudyStatus.Published);
        var english = CreateCourse(StudyStatus.Published);
        AddTranslation(english, "en", StudyStatus.Published);
        var missing = CreateCourse(StudyStatus.Draft);
        db.AddRange(french, english, missing);
        await db.SaveChangesAsync();
        var queries = new EfAdminCourseQueries(db);
        var all = await queries.ListAsync(null, null, null, null, null, null, 1, 10, default);
        Assert.Equal("Titre français", all.Items.Single(x => x.Id == french.Id).DisplayTitle);
        Assert.Equal("English title", all.Items.Single(x => x.Id == english.Id).DisplayTitle);
        Assert.Equal("Cours sans traduction", all.Items.Single(x => x.Id == missing.Id).DisplayTitle);
        var drafts = await queries.ListAsync(null, null, null, "Draft", null, null, 1, 10, default);
        Assert.Equal(french.Id, Assert.Single(drafts.Items).Id);
        var missingEnglish = await queries.ListAsync(null, null, null, null, "Missing", null, 1, 10, default);
        Assert.Equal(missing.Id, Assert.Single(missingEnglish.Items).Id);
    }

    [Fact]
    public async Task Course_list_renders_unique_descriptions_and_visible_labels_for_both_layouts()
    {
        using var factory = new AdminWebApplicationFactory(AdminTestProfile.Admin);
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/admin/courses");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Contains("Non traduit", html);
        Assert.Contains("Non visible", html);
        Assert.DoesNotContain(">Missing<", html);
        var descriptions = Regex.Matches(html, "aria-describedby=\"([^\"]+)\"").Select(m => m.Groups[1].Value).ToArray();
        var tooltipDescriptions = descriptions.Where(id => Regex.IsMatch(html, $"id=\"{Regex.Escape(id)}\" class=\"course-visibility-tooltip\" role=\"tooltip\"")).ToArray();
        Assert.Equal(4, tooltipDescriptions.Length);
        Assert.Equal(4, tooltipDescriptions.Distinct().Count());
        foreach (var id in tooltipDescriptions) Assert.Contains($"id=\"{id}\" class=\"course-visibility-tooltip\" role=\"tooltip\"", html);
        Assert.Contains("Français : Non traduit, Non visible", html);
        Assert.Contains("Anglais : Non traduit, Non visible", html);
        Assert.Contains("/js/course-visibility.js", html);
    }

    private static ApplicationDbContext CreateDatabase(SqliteConnection connection, QueryCounter? counter = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection);
        if (counter != null) options.AddInterceptors(counter);
        return new(options.Options);
    }

    private static Course CreateCourse(StudyStatus status) => new()
    {
        Id = Guid.NewGuid(),
        Title = "Legacy title",
        Slug = Guid.NewGuid().ToString("N"),
        Status = status,
        CreatedOnUtc = DateTime.UtcNow,
        CourseCategory = new CourseCategory { Id = Guid.NewGuid(), Title = "Draft category", Slug = Guid.NewGuid().ToString("N"), Status = StudyStatus.Draft }
    };

    private static void AddTranslation(Course course, string language, StudyStatus status) => course.Translations.Add(new()
    {
        Id = Guid.NewGuid(),
        CourseId = course.Id,
        LanguageCode = language,
        PublicationStatus = status,
        Title = language == "fr" ? "Titre français" : "English title",
        Slug = $"{language}-{course.Id:N}"
    });

    private sealed class FixedLanguage(string code) : ICurrentLanguageService
    { public string LanguageCode => code; }

    private sealed class QueryCounter : DbCommandInterceptor
    {
        public int Count { get; set; }

        public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
        { Count++; return result; }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        { Count++; return ValueTask.FromResult(result); }
    }
}