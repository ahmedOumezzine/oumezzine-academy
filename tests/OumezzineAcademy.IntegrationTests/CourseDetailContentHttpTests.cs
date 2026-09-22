using System.Net;
using AngleSharp.Html.Parser;
using OumezzineAcademy.Infrastructure.Data;
using OumezzineAcademy.Domain.Catalog;
using OumezzineAcademy.Models.Catalog;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace OumezzineAcademy.Tests;

public sealed class CourseDetailContentHttpTests
{
    private const string ReactFrench = "Description réelle du cours React en français.";
    private const string ReactEnglish = "The real React course description in English.";
    private const string CourseAContent = "Unique detailed content belonging only to Course A.";
    private const string CourseBContent = "Unique detailed content belonging only to Course B.";

    [Fact]
    public async Task Course_details_render_their_published_language_content_once_without_cross_course_content()
    {
        using var factory = new AdminWebApplicationFactory(AdminTestProfile.Anonymous);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await SeedCoursesAsync(factory);

        var reactFrench = await GetPageAsync(client, "/fr/cours/react-introduction");
        var reactEnglish = await GetPageAsync(client, "/en/courses/react-introduction-en");
        var courseAFrench = await GetPageAsync(client, "/fr/cours/course-a-fr");
        var courseBEnglish = await GetPageAsync(client, "/en/courses/course-b-en");
        var emptyCourseFrench = await GetPageAsync(client, "/fr/cours/course-empty-fr");

        Assert.Contains(ReactFrench, reactFrench, StringComparison.Ordinal);
        Assert.DoesNotContain(ReactEnglish, reactFrench, StringComparison.Ordinal);
        Assert.Contains(ReactEnglish, reactEnglish, StringComparison.Ordinal);
        Assert.DoesNotContain(ReactFrench, reactEnglish, StringComparison.Ordinal);
        Assert.Contains(CourseAContent, courseAFrench, StringComparison.Ordinal);
        Assert.DoesNotContain(CourseBContent, courseAFrench, StringComparison.Ordinal);
        Assert.Contains(CourseBContent, courseBEnglish, StringComparison.Ordinal);
        Assert.DoesNotContain(CourseAContent, courseBEnglish, StringComparison.Ordinal);

        Assert.Single(AboutHeadings(reactFrench, "À propos de ce cours"));
        Assert.Single(AboutHeadings(reactEnglish, "About this course"));
        Assert.Empty(AboutHeadings(emptyCourseFrench, "À propos de ce cours"));
        Assert.DoesNotContain("APIs REST modernes avec ASP.NET Core", reactFrench, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("modern REST APIs with ASP.NET Core", reactEnglish, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<string> GetPageAsync(HttpClient client, string path)
    {
        using var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadAsStringAsync();
    }

    private static IEnumerable<string> AboutHeadings(string html, string title)
    {
        var document = new HtmlParser().ParseDocument(html);
        return document.QuerySelectorAll("h2").Where(h => h.TextContent.Trim() == title).Select(h => h.TextContent);
    }

    private static async Task SeedCoursesAsync(AdminWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var category = new CourseCategory
        {
            Id = Guid.NewGuid(), CreatedOnUtc = DateTime.UtcNow, Title = "Development", Slug = "development",
            Status = StudyStatus.Published
        };
        category.Translations.AddRange(
            new CourseCategoryTranslation
            {
                Id = Guid.NewGuid(), LanguageCode = "fr",
                PublicationStatus = StudyStatus.Published, Title = "Développement", Slug = "developpement"
            },
            new CourseCategoryTranslation
            {
                Id = Guid.NewGuid(), LanguageCode = "en",
                PublicationStatus = StudyStatus.Published, Title = "Development", Slug = "development"
            });

        db.AddRange(category,
            CreateCourse(category, "React Introduction", "react-introduction", "react-introduction-en",
                "Résumé court React FR.", "Short React summary EN.", ReactFrench, ReactEnglish),
            CreateCourse(category, "Course A", "course-a-fr", "course-a-en",
                "Résumé A.", "Summary A.", CourseAContent, "Course A English detail."),
            CreateCourse(category, "Course B", "course-b-fr", "course-b-en",
                "Résumé B.", "Summary B.", "Course B French detail.", CourseBContent),
            CreateCourse(category, "Course Empty", "course-empty-fr", "course-empty-en",
                "Résumé sans description.", "Summary without detail.", "", ""));
        await db.SaveChangesAsync();
    }

    private static Course CreateCourse(CourseCategory category, string title, string frenchSlug, string englishSlug,
        string frenchSummary, string englishSummary, string frenchOverview, string englishOverview)
    {
        var course = new Course
        {
            Id = Guid.NewGuid(), CreatedOnUtc = DateTime.UtcNow, Title = title, Slug = frenchSlug,
            CourseCategoryId = category.Id, CourseCategory = category, Level = StudyLevel.Beginner, Status = StudyStatus.Published
        };
        course.Translations.Add(new CourseTranslation
        {
            Id = Guid.NewGuid(), LanguageCode = "fr", PublicationStatus = StudyStatus.Published,
            Title = title, Slug = frenchSlug, Summary = frenchSummary, Overview = RichHtml(frenchOverview)
        });
        course.Translations.Add(new CourseTranslation
        {
            Id = Guid.NewGuid(), LanguageCode = "en", PublicationStatus = StudyStatus.Published,
            Title = $"{title} EN", Slug = englishSlug, Summary = englishSummary, Overview = RichHtml(englishOverview)
        });
        return course;
    }

    private static string? RichHtml(string content) => string.IsNullOrWhiteSpace(content) ? null : $"<p>{content}</p>";
}

