using OumezzineAcademy.Infrastructure.Data;
using OumezzineAcademy.Domain.Catalog;
using OumezzineAcademy.Models.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace OumezzineAcademy.Tests;

public sealed class PublicLocalizedRoutingTests
{
    private static readonly Guid CategoryId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    [Fact]
    public async Task EnglishHomeAndCatalogRenderEnglishCourseUrls()
    {
        using var factory = new AdminWebApplicationFactory(AdminTestProfile.Anonymous);
        using var client = factory.CreateClient();
        await SeedPublicCourseAsync(factory);

        var home = await client.GetStringAsync("/en/");
        var catalog = await client.GetStringAsync("/en/courses");

        Assert.Contains("href=\"/en/courses\"", home);
        Assert.Contains("href=\"/en/courses/course-en\"", catalog);
        Assert.Contains("<title>Courses - Oumezzine Academy</title>", catalog);
        Assert.Contains("name=\"description\" content=\"Explore courses by topic, category, level, and recency", catalog);
        Assert.Contains("property=\"og:image\" content=\"https://localhost/images/brand/logo-primary.png\"", catalog);
        Assert.Contains("name=\"twitter:image\" content=\"https://localhost/images/brand/logo-primary.png\"", catalog);
        AssertNoFrenchContentRoutes(home);
        AssertNoFrenchContentRoutes(catalog);
    }

    [Fact]
    public async Task AnonymousEnglishLearningPathUsesPublicEnglishCourseLinks()
    {
        using var factory = new AdminWebApplicationFactory(AdminTestProfile.Anonymous);
        using var client = factory.CreateClient();
        await SeedPublicCourseAsync(factory);
        await SeedPublicPathAsync(factory);

        var html = await client.GetStringAsync("/en/learning-paths/path-en");

        Assert.True(html.Contains("href=\"/en/courses/course-en\"", StringComparison.Ordinal),
            string.Join("\n", System.Text.RegularExpressions.Regex.Matches(html, "href=\\\"[^\\\"]+\\\"").Select(match => match.Value)));
        Assert.DoesNotContain("/admin/", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/fr/cours/", html, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task SeedPublicCourseAsync(AdminWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.StudyCourseCategoryTranslations.Add(new CourseCategoryTranslation
        {
            Id = Guid.NewGuid(), CourseCategoryId = CategoryId, LanguageCode = "en",
            PublicationStatus = StudyStatus.Published, Title = "Frontend", Slug = "frontend"
        });
        db.StudyCourseTranslations.Add(new CourseTranslation
        {
            Id = Guid.NewGuid(), CourseId = AdminWebApplicationFactory.CourseId, LanguageCode = "en",
            PublicationStatus = StudyStatus.Published, Title = "Course EN", Slug = "course-en", Summary = "Summary"
        });
        await db.SaveChangesAsync();
    }

    private static void AssertNoFrenchContentRoutes(string html)
    {
        var hrefs = System.Text.RegularExpressions.Regex.Matches(html, "<a\\b[^>]*\\bhref=\"([^\"]+)\"")
            .Select(match => match.Groups[1].Value);
        Assert.DoesNotContain(hrefs, href => href.StartsWith("/fr/cours/", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(hrefs, href => href.StartsWith("/fr/parcours/", StringComparison.OrdinalIgnoreCase));
    }

    private static async Task SeedPublicPathAsync(AdminWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var category = new LearningPathCategory { Id = Guid.NewGuid(), Status = StudyStatus.Published };
        category.Translations.Add(new LearningPathCategoryTranslation
        {
            Id = Guid.NewGuid(), LearningPathCategoryId = category.Id, LanguageCode = "en",
            PublicationStatus = StudyStatus.Published, Title = "Development", Slug = "development"
        });
        var path = new LearningPath
        {
            Id = Guid.NewGuid(), LearningPathCategory = category, Status = StudyStatus.Published,
            Level = StudyLevel.Beginner
        };
        path.Translations.Add(new LearningPathTranslation
        {
            Id = Guid.NewGuid(), LearningPathId = path.Id, LanguageCode = "en",
            PublicationStatus = StudyStatus.Published, Title = "Path EN", Slug = "path-en", Summary = "Path summary"
        });
        path.LearningPathCourses.Add(new LearningPathCourse
        {
            Id = Guid.NewGuid(), LearningPath = path, CourseId = AdminWebApplicationFactory.CourseId, Order = 1
        });
        db.StudyLearningPaths.Add(path);
        await db.SaveChangesAsync();
    }
}

