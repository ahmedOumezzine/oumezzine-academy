using AngleSharp.Html.Parser;
using Microsoft.Extensions.DependencyInjection;
using OumezzineAcademy.Infrastructure.Data;
using Xunit;

namespace OumezzineAcademy.Tests;

public sealed class PublicEnglishLinksHttpTests
{
    private const string FrenchCategorySlug = "developpement-web";
    private const string EnglishCategorySlug = "web-development";

    [Fact]
    public async Task Header_logo_and_about_cta_follow_the_current_language()
    {
        using var factory = new AdminWebApplicationFactory(AdminTestProfile.Anonymous);
        using var client = factory.CreateClient();
        await SeedPublicContentAsync(factory, includeEnglishCategory: true);

        var englishHome = await GetDocumentAsync(client, "/en/");
        var frenchHome = await GetDocumentAsync(client, "/fr/");
        var englishAbout = await GetDocumentAsync(client, "/en/about");
        var frenchAbout = await GetDocumentAsync(client, "/fr/a-propos");

        Assert.Equal("/en/", englishHome.QuerySelector(".site-header a.brand")?.GetAttribute("href"));
        Assert.Equal("/fr/", frenchHome.QuerySelector(".site-header a.brand")?.GetAttribute("href"));
        Assert.Equal("/en/courses", englishAbout.QuerySelector(".about-final-cta a.btn-main")?.GetAttribute("href"));
        Assert.Equal("/fr/cours", frenchAbout.QuerySelector(".about-final-cta a.btn-main")?.GetAttribute("href"));
    }

    [Fact]
    public async Task Course_detail_uses_the_current_languages_category_slug_and_hides_untranslated_category()
    {
        using var factory = new AdminWebApplicationFactory(AdminTestProfile.Anonymous);
        using var client = factory.CreateClient();
        await SeedPublicContentAsync(factory, includeEnglishCategory: true);

        var english = await GetDocumentAsync(client, "/en/courses/introduction-to-react");
        var french = await GetDocumentAsync(client, "/fr/cours/introduction-a-react");
        var englishCategoryLinks = CategoryLinks(english);
        var frenchCategoryLinks = CategoryLinks(french);

        Assert.NotEmpty(englishCategoryLinks);
        Assert.All(englishCategoryLinks, href => Assert.Equal($"/en/categories/{EnglishCategorySlug}", href));
        Assert.NotEmpty(frenchCategoryLinks);
        Assert.All(frenchCategoryLinks, href => Assert.Equal($"/fr/categories/{FrenchCategorySlug}", href));

        using var untranslatedFactory = new AdminWebApplicationFactory(AdminTestProfile.Anonymous);
        using var untranslatedClient = untranslatedFactory.CreateClient();
        await SeedPublicContentAsync(untranslatedFactory, includeEnglishCategory: false);
        var untranslatedEnglish = await GetDocumentAsync(untranslatedClient, "/en/courses/introduction-to-react");
        Assert.Empty(CategoryLinks(untranslatedEnglish));
    }

    [Fact]
    public async Task Main_english_pages_have_no_public_links_to_french_routes_except_the_language_switch()
    {
        using var factory = new AdminWebApplicationFactory(AdminTestProfile.Anonymous);
        using var client = factory.CreateClient();
        await SeedPublicContentAsync(factory, includeEnglishCategory: true);

        var pages = new[]
        {
            "/en/", "/en/courses", "/en/courses/introduction-to-react", "/en/learning-paths",
            "/en/learning-paths/path-en", "/en/lessons/react-lesson-en", "/en/about"
        };
        foreach (var path in pages)
        {
            var document = await GetDocumentAsync(client, path);
            var frenchLinks = document.QuerySelectorAll("a[href]")
                .Where(anchor => anchor.Closest(".language-switcher") is null)
                .Select(anchor => anchor.GetAttribute("href") ?? "")
                .Where(IsFrenchRoute)
                .ToArray();
            Assert.True(frenchLinks.Length == 0, $"{path} contains public FR links: {string.Join(", ", frenchLinks)}");
        }

        var home = await GetDocumentAsync(client, "/en/");
        Assert.Contains(home.QuerySelectorAll(".language-switcher a[href]").Select(a => a.GetAttribute("href")),
            href => href?.StartsWith("/fr", StringComparison.OrdinalIgnoreCase) == true);
    }

    private static async Task<AngleSharp.Html.Dom.IHtmlDocument> GetDocumentAsync(HttpClient client, string path)
        => new HtmlParser().ParseDocument(await client.GetStringAsync(path));

    private static string[] CategoryLinks(AngleSharp.Html.Dom.IHtmlDocument document)
        => document.QuerySelectorAll(".course-detail-breadcrumb a[href], .course-hero a[href], .course-about-card a[href]")
            .Select(anchor => anchor.GetAttribute("href") ?? "")
            .Where(href => href.Contains("/categories/", StringComparison.OrdinalIgnoreCase))
            .ToArray();

    private static bool IsFrenchRoute(string href)
        => href.StartsWith("/fr", StringComparison.OrdinalIgnoreCase)
            || href.StartsWith("https://cours.oumezzine.com/fr", StringComparison.OrdinalIgnoreCase);

    private static async Task SeedPublicContentAsync(AdminWebApplicationFactory factory, bool includeEnglishCategory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var category = new CourseCategory
        {
            Id = Guid.NewGuid(),
            CreatedOnUtc = DateTime.UtcNow,
            Title = "Développement",
            Slug = FrenchCategorySlug,
            Status = StudyStatus.Published
        };
        category.Translations.Add(new CourseCategoryTranslation
        {
            Id = Guid.NewGuid(),
            LanguageCode = "fr",
            PublicationStatus = StudyStatus.Published,
            Title = "Développement web",
            Slug = FrenchCategorySlug
        });
        if (includeEnglishCategory)
            category.Translations.Add(new CourseCategoryTranslation
            {
                Id = Guid.NewGuid(),
                LanguageCode = "en",
                PublicationStatus = StudyStatus.Published,
                Title = "Web development",
                Slug = EnglishCategorySlug
            });

        var course = new Course
        {
            Id = Guid.NewGuid(),
            CreatedOnUtc = DateTime.UtcNow,
            Title = "Introduction React",
            Slug = "introduction-a-react",
            CourseCategory = category,
            CourseCategoryId = category.Id,
            Level = StudyLevel.Beginner,
            Status = StudyStatus.Published
        };
        course.Translations.Add(new CourseTranslation
        {
            Id = Guid.NewGuid(),
            LanguageCode = "fr",
            PublicationStatus = StudyStatus.Published,
            Title = "Introduction à React",
            Slug = "introduction-a-react",
            Summary = "Résumé React FR",
            Overview = "<p>React FR</p>"
        });
        course.Translations.Add(new CourseTranslation
        {
            Id = Guid.NewGuid(),
            LanguageCode = "en",
            PublicationStatus = StudyStatus.Published,
            Title = "Introduction to React",
            Slug = "introduction-to-react",
            Summary = "React summary EN",
            Overview = "<p>React EN</p>"
        });

        var chapter = new CourseContent
        {
            Id = Guid.NewGuid(),
            CreatedOnUtc = DateTime.UtcNow,
            Title = "React chapter",
            Slug = "react-chapter",
            Course = course,
            CourseId = course.Id,
            Order = 1,
            Status = StudyStatus.Published
        };
        chapter.Translations.Add(new CourseContentTranslation
        {
            Id = Guid.NewGuid(),
            LanguageCode = "fr",
            PublicationStatus = StudyStatus.Published,
            Title = "Chapitre React"
        });
        chapter.Translations.Add(new CourseContentTranslation
        {
            Id = Guid.NewGuid(),
            LanguageCode = "en",
            PublicationStatus = StudyStatus.Published,
            Title = "React chapter"
        });
        var lesson = new CourseLesson
        {
            Id = Guid.NewGuid(),
            CreatedOnUtc = DateTime.UtcNow,
            Title = "React lesson",
            Slug = "react-lesson-fr",
            CourseContent = chapter,
            CourseContentId = chapter.Id,
            Order = 1,
            Status = StudyStatus.Published
        };
        lesson.Translations.Add(new CourseLessonTranslation
        {
            Id = Guid.NewGuid(),
            LanguageCode = "fr",
            PublicationStatus = StudyStatus.Published,
            Title = "Leçon React",
            Slug = "react-lesson-fr",
            Summary = "Résumé de la leçon"
        });
        lesson.Translations.Add(new CourseLessonTranslation
        {
            Id = Guid.NewGuid(),
            LanguageCode = "en",
            PublicationStatus = StudyStatus.Published,
            Title = "React lesson",
            Slug = "react-lesson-en",
            Summary = "React lesson summary"
        });

        var pathCategory = new LearningPathCategory
        {
            Id = Guid.NewGuid(),
            CreatedOnUtc = DateTime.UtcNow,
            Title = "Development",
            Slug = "development",
            Status = StudyStatus.Published
        };
        pathCategory.Translations.Add(new LearningPathCategoryTranslation
        {
            Id = Guid.NewGuid(),
            LanguageCode = "fr",
            PublicationStatus = StudyStatus.Published,
            Title = "Développement",
            Slug = "developpement"
        });
        pathCategory.Translations.Add(new LearningPathCategoryTranslation
        {
            Id = Guid.NewGuid(),
            LanguageCode = "en",
            PublicationStatus = StudyStatus.Published,
            Title = "Development",
            Slug = "development"
        });
        var path = new LearningPath
        {
            Id = Guid.NewGuid(),
            LearningPathCategory = pathCategory,
            LearningPathCategoryId = pathCategory.Id,
            Status = StudyStatus.Published,
            Level = StudyLevel.Beginner
        };
        path.Translations.Add(new LearningPathTranslation
        {
            Id = Guid.NewGuid(),
            LanguageCode = "fr",
            PublicationStatus = StudyStatus.Published,
            Title = "Parcours React",
            Slug = "parcours-react",
            Summary = "Parcours React"
        });
        path.Translations.Add(new LearningPathTranslation
        {
            Id = Guid.NewGuid(),
            LanguageCode = "en",
            PublicationStatus = StudyStatus.Published,
            Title = "React path",
            Slug = "path-en",
            Summary = "React path"
        });
        path.LearningPathCourses.Add(new LearningPathCourse { Id = Guid.NewGuid(), LearningPath = path, Course = course, Order = 1 });
        db.AddRange(category, course, chapter, lesson, pathCategory, path);
        await db.SaveChangesAsync();
    }
}