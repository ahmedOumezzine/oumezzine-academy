using Microsoft.AspNetCore.Http;
using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Web.Services;
using Xunit;

namespace OumezzineAcademy.Tests;

public sealed class LocalizedUrlServiceTests
{
    [Fact]
    public void Public_navigation_uses_french_routes_by_default()
    {
        var service = new LocalizedUrlService(new SlugQueries(), new FixedLanguage("fr"));
        var navigation = service.GetPublicNavigation();
        Assert.Equal("/fr/", navigation.Home);
        Assert.Equal("/fr/cours", navigation.Courses);
        Assert.Equal("/fr/parcours", navigation.LearningPaths);
    }

    [Fact]
    public void Public_navigation_uses_english_routes_when_selected()
    {
        var navigation = new LocalizedUrlService(new SlugQueries(), new FixedLanguage("en")).GetPublicNavigation();
        Assert.Equal("/en/", navigation.Home);
        Assert.Equal("/en/courses", navigation.Courses);
        Assert.Equal("/en/learning-paths", navigation.LearningPaths);
    }

    [Theory]
    [InlineData("/fr/cours", "/en/courses")]
    [InlineData("/en/courses", "/fr/cours")]
    [InlineData("/fr/a-propos", "/en/about")]
    public async Task Resolves_general_language_alternates(string path, string alternate)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        var result = await new LocalizedUrlService(new SlugQueries()).ResolveAsync(context);
        Assert.Equal(alternate, path.StartsWith("/en", StringComparison.OrdinalIgnoreCase) ? result.French : result.English);
    }

    [Fact]
    public async Task Resolves_localized_course_slugs()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/en/courses/intro";
        context.Request.RouteValues["slug"] = "intro";
        var result = await new LocalizedUrlService(new SlugQueries(new("course", "intro", new Dictionary<string, string>
        {
            ["fr"] = "introduction",
            ["en"] = "intro"
        }))).ResolveAsync(context);

        Assert.Equal("/en/courses/intro", result.Current);
        Assert.Equal("/fr/cours/introduction", result.French);
    }

    private sealed class FixedLanguage(string code) : ICurrentLanguageService
    {
        public string LanguageCode => code;
    }

    private sealed class SlugQueries((string Type, string Slug, IReadOnlyDictionary<string, string> Slugs)? result = null) : ILocalizedSlugQueries
    {
        public Task<LocalizedSlugSet?> FindAsync(string entityType, string slug, string languageCode, CancellationToken cancellationToken = default)
            => Task.FromResult(result is null || result.Value.Type != entityType || result.Value.Slug != slug
                ? null
                : (LocalizedSlugSet?)new(Guid.NewGuid(), result.Value.Slugs));
    }
}