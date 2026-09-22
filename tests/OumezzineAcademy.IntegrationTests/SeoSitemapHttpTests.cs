using System.Net;
using System.Xml.Linq;
using OumezzineAcademy.Infrastructure.Data;
using OumezzineAcademy.Domain.Catalog;
using OumezzineAcademy.Models.Catalog;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace OumezzineAcademy.Tests;

public sealed class SeoSitemapHttpTests
{
    [Fact]
    public async Task Sitemap_returns_valid_https_xml_with_only_published_localized_slugs()
    {
        using var factory = new AdminWebApplicationFactory(AdminTestProfile.Anonymous);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://cours.oumezzine.com"),
            AllowAutoRedirect = false
        });
        await SeedTranslationsAsync(factory);

        using var response = await client.GetAsync("/sitemap.xml");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/xml", response.Content.Headers.ContentType?.MediaType);
        var document = XDocument.Parse(body);
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        var locations = document.Root!.Elements(ns + "url").Select(x => (string)x.Element(ns + "loc")!).ToArray();

        Assert.Contains("https://cours.oumezzine.com/fr/", locations);
        Assert.Contains("https://cours.oumezzine.com/en/", locations);
        Assert.Contains("https://cours.oumezzine.com/fr/cours/cours-fr&plus", locations);
        Assert.Contains("https://cours.oumezzine.com/en/courses/course-en", locations);
        Assert.Contains("https://cours.oumezzine.com/fr/cours/fallback-fr-only", locations);
        Assert.DoesNotContain("https://cours.oumezzine.com/en/courses/fallback-fr-only", locations);
        Assert.DoesNotContain(locations, x => x.Contains("draft-course", StringComparison.Ordinal));
        Assert.DoesNotContain(locations, x => x.Contains("empty-slug-course", StringComparison.Ordinal));
        Assert.All(locations, url => Assert.StartsWith("https://cours.oumezzine.com/", url, StringComparison.Ordinal));
        Assert.DoesNotContain(locations, url => url.Contains("localhost", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(locations, url => url.Contains("127.0.0.1", StringComparison.Ordinal));
        Assert.DoesNotContain(locations, url => url.Contains("/admin", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(locations, url => url.Contains("/identity", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(locations, url => url.Contains("/quiz/submit", StringComparison.OrdinalIgnoreCase));

        var robots = await client.GetStringAsync("/robots.txt");
        Assert.Contains("Sitemap: https://cours.oumezzine.com/sitemap.xml", robots, StringComparison.Ordinal);
    }

    private static async Task SeedTranslationsAsync(AdminWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.StudyCourseTranslations.AddRange(
            new CourseTranslation
            {
                Id = Guid.NewGuid(), CourseId = AdminWebApplicationFactory.CourseId, LanguageCode = "fr",
                PublicationStatus = StudyStatus.Published, Title = "Cours français", Slug = "cours-fr&plus"
            },
            new CourseTranslation
            {
                Id = Guid.NewGuid(), CourseId = AdminWebApplicationFactory.CourseId, LanguageCode = "en",
                PublicationStatus = StudyStatus.Published, Title = "English course", Slug = "course-en"
            });

        var categoryId = await db.StudyCourses.Where(x => x.Id == AdminWebApplicationFactory.CourseId)
            .Select(x => x.CourseCategoryId).SingleAsync();
        var frenchOnly = new Course
        {
            Id = Guid.NewGuid(), CreatedOnUtc = DateTime.UtcNow, Title = "French only", Slug = "fallback-fr-only",
            CourseCategoryId = categoryId, Status = StudyStatus.Published,
            Translations = [new CourseTranslation
            {
                Id = Guid.NewGuid(), LanguageCode = "fr", PublicationStatus = StudyStatus.Published,
                Title = "Seulement français", Slug = "fallback-fr-only"
            }]
        };
        var draft = new Course
        {
            Id = Guid.NewGuid(), CreatedOnUtc = DateTime.UtcNow, Title = "Draft", Slug = "draft-course",
            CourseCategoryId = categoryId, Status = StudyStatus.Draft,
            Translations = [new CourseTranslation
            {
                Id = Guid.NewGuid(), LanguageCode = "fr", PublicationStatus = StudyStatus.Published,
                Title = "Brouillon", Slug = "draft-course"
            }]
        };
        var emptySlug = new Course
        {
            Id = Guid.NewGuid(), CreatedOnUtc = DateTime.UtcNow, Title = "Empty slug", Slug = "empty-slug-course",
            CourseCategoryId = categoryId, Status = StudyStatus.Published,
            Translations = [new CourseTranslation
            {
                Id = Guid.NewGuid(), LanguageCode = "en", PublicationStatus = StudyStatus.Published,
                Title = "Empty slug", Slug = " "
            }]
        };
        db.StudyCourses.AddRange(frenchOnly, draft, emptySlug);
        await db.SaveChangesAsync();
    }
}

