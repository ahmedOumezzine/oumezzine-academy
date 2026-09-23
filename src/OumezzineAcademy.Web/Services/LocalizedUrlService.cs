using OumezzineAcademy.Application.Abstractions;

namespace OumezzineAcademy.Web.Services;

public sealed record LocalizedUrlSet(
    string Current,
    string? French,
    string? English);

public sealed record PublicNavigationUrlSet(
    string Home,
    string Courses,
    string Categories,
    string LearningPaths,
    string About);

public interface ILocalizedUrlService
{
    Task<LocalizedUrlSet> ResolveAsync(HttpContext context);

    PublicNavigationUrlSet GetPublicNavigation();
}

public sealed class LocalizedUrlService : ILocalizedUrlService
{
    private readonly ILocalizedSlugQueries _queries;
    private readonly ICurrentLanguageService _language;

    [ActivatorUtilitiesConstructor]
    public LocalizedUrlService(
        ILocalizedSlugQueries queries,
        ICurrentLanguageService? language = null)
    {
        _queries = queries;
        _language = language ?? new CurrentLanguageService();
    }

    public PublicNavigationUrlSet GetPublicNavigation()
        => _language.LanguageCode == "en"
            ? new(
                "/en/",
                "/en/courses",
                "/en/categories",
                "/en/learning-paths",
                "/en/about")
            : new(
                "/fr/",
                "/fr/cours",
                "/fr/categories",
                "/fr/parcours",
                "/fr/a-propos");

    public async Task<LocalizedUrlSet> ResolveAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.TrimEnd('/') ?? "";
        var languageCode = path.StartsWith(
            "/en",
            StringComparison.OrdinalIgnoreCase)
            ? "en"
            : "fr";
        var current = path.Length == 0 ? "/fr/" : path;
        var slug = context.Request.RouteValues["slug"]?.ToString();

        if (string.IsNullOrWhiteSpace(slug))
        {
            return General(current, languageCode);
        }

        var (entityType, frenchPrefix, englishPrefix) = path switch
        {
            _ when path.Contains(
                "/cours/",
                StringComparison.OrdinalIgnoreCase)
                || path.Contains(
                    "/courses/",
                    StringComparison.OrdinalIgnoreCase)
                => ("course", "/fr/cours/", "/en/courses/"),
            _ when path.Contains(
                "/categories/",
                StringComparison.OrdinalIgnoreCase)
                => ("category", "/fr/categories/", "/en/categories/"),
            _ when path.Contains(
                "/parcours/",
                StringComparison.OrdinalIgnoreCase)
                || path.Contains(
                    "/learning-paths/",
                    StringComparison.OrdinalIgnoreCase)
                => ("path", "/fr/parcours/", "/en/learning-paths/"),
            _ when path.Contains(
                "/lecons/",
                StringComparison.OrdinalIgnoreCase)
                || path.Contains(
                    "/lessons/",
                    StringComparison.OrdinalIgnoreCase)
                => ("lesson", "/fr/lecons/", "/en/lessons/"),
            _ when path.Contains(
                "/quiz/",
                StringComparison.OrdinalIgnoreCase)
                => ("quiz", "/fr/quiz/", "/en/quiz/"),
            _ => (null, "", "")
        };

        if (entityType is null)
        {
            return General(current, languageCode);
        }

        var result = await _queries.FindAsync(
            entityType,
            slug,
            languageCode,
            context.RequestAborted);

        if (result is null)
        {
            return General(current, languageCode);
        }

        result.Slugs.TryGetValue("fr", out var frenchSlug);
        result.Slugs.TryGetValue("en", out var englishSlug);

        var french = frenchSlug is null
            ? null
            : frenchPrefix + frenchSlug;
        var english = englishSlug is null
            ? null
            : englishPrefix + englishSlug;

        return new(
            languageCode == "en"
                ? english ?? current
                : french ?? current,
            french,
            english);
    }

    private static LocalizedUrlSet General(
        string current,
        string languageCode)
    {
        var map = languageCode == "en"
            ? new Dictionary<string, string>
            {
                ["/en"] = "/fr",
                ["/en/courses"] = "/fr/cours",
                ["/en/categories"] = "/fr/categories",
                ["/en/learning-paths"] = "/fr/parcours",
                ["/en/about"] = "/fr/a-propos"
            }
            : new Dictionary<string, string>
            {
                ["/fr"] = "/en",
                ["/fr/cours"] = "/en/courses",
                ["/fr/categories"] = "/en/categories",
                ["/fr/parcours"] = "/en/learning-paths",
                ["/fr/a-propos"] = "/en/about"
            };

        var alternate = map.GetValueOrDefault(current);

        return languageCode == "en"
            ? new(current, alternate, current)
            : new(current, current, alternate);
    }
}
