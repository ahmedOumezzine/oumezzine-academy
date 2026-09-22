using OumezzineAcademy.Application.Abstractions;

namespace OumezzineAcademy.Web.Services;

public sealed record LocalizedUrlSet(string Current, string? French, string? English);
public sealed record PublicNavigationUrlSet(string Home, string Courses, string Categories, string LearningPaths, string About);

public interface ILocalizedUrlService
{ Task<LocalizedUrlSet> ResolveAsync(HttpContext context); PublicNavigationUrlSet GetPublicNavigation(); }

public sealed class LocalizedUrlService : ILocalizedUrlService
{
    private readonly ILocalizedSlugQueries _queries; private readonly ICurrentLanguageService _language;

    [ActivatorUtilitiesConstructor]
    public LocalizedUrlService(ILocalizedSlugQueries queries, ICurrentLanguageService? language = null)
    {
        _queries = queries; _language = language ?? new CurrentLanguageService();
    }

    public PublicNavigationUrlSet GetPublicNavigation() => _language.LanguageCode == "en" ? new("/en/", "/en/courses", "/en/categories", "/en/learning-paths", "/en/about") : new("/fr/", "/fr/cours", "/fr/categories", "/fr/parcours", "/fr/a-propos");

    public async Task<LocalizedUrlSet> ResolveAsync(HttpContext context)
    { var path = context.Request.Path.Value?.TrimEnd('/') ?? ""; var lang = path.StartsWith("/en", StringComparison.OrdinalIgnoreCase) ? "en" : "fr"; var current = path.Length == 0 ? "/fr/" : path; var slug = context.Request.RouteValues["slug"]?.ToString(); if (string.IsNullOrWhiteSpace(slug)) return General(current, lang); var (type, fp, ep) = path switch { _ when path.Contains("/cours/", StringComparison.OrdinalIgnoreCase) || path.Contains("/courses/", StringComparison.OrdinalIgnoreCase) => ("course", "/fr/cours/", "/en/courses/"), _ when path.Contains("/categories/", StringComparison.OrdinalIgnoreCase) => ("category", "/fr/categories/", "/en/categories/"), _ when path.Contains("/parcours/", StringComparison.OrdinalIgnoreCase) || path.Contains("/learning-paths/", StringComparison.OrdinalIgnoreCase) => ("path", "/fr/parcours/", "/en/learning-paths/"), _ when path.Contains("/lecons/", StringComparison.OrdinalIgnoreCase) || path.Contains("/lessons/", StringComparison.OrdinalIgnoreCase) => ("lesson", "/fr/lecons/", "/en/lessons/"), _ when path.Contains("/quiz/", StringComparison.OrdinalIgnoreCase) => ("quiz", "/fr/quiz/", "/en/quiz/"), _ => (null, "", "") }; if (type is null) return General(current, lang); var result = await _queries.FindAsync(type, slug, lang, context.RequestAborted); if (result is null) return General(current, lang); result.Slugs.TryGetValue("fr", out var fr); result.Slugs.TryGetValue("en", out var en); var french = fr is null ? null : fp + fr; var english = en is null ? null : ep + en; return new(lang == "en" ? english ?? current : french ?? current, french, english); }

    private static LocalizedUrlSet General(string current, string lang)
    { var map = lang == "en" ? new Dictionary<string, string> { { "/en", "/fr" }, { "/en/courses", "/fr/cours" }, { "/en/categories", "/fr/categories" }, { "/en/learning-paths", "/fr/parcours" }, { "/en/about", "/fr/a-propos" } } : new Dictionary<string, string> { { "/fr", "/en" }, { "/fr/cours", "/en/courses" }, { "/fr/categories", "/en/categories" }, { "/fr/parcours", "/en/learning-paths" }, { "/fr/a-propos", "/en/about" } }; var alt = map.GetValueOrDefault(current); return lang == "en" ? new(current, alt, current) : new(current, current, alt); }
}