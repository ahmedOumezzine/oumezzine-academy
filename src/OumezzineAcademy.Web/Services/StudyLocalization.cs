using Microsoft.AspNetCore.Localization;

namespace OumezzineAcademy.Web.Services;

public static class StudyLanguages
{
    public const string French = "fr";
    public const string English = "en";
    public static readonly string[] Supported = [French, English];
}

public sealed class RouteLanguageRequestCultureProvider : RequestCultureProvider
{
    public override Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
    {
        var segment = httpContext.Request.Path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return Task.FromResult<ProviderCultureResult?>(
            StudyLanguages.Supported.Contains(segment, StringComparer.OrdinalIgnoreCase)
                ? new ProviderCultureResult(segment!.ToLowerInvariant())
                : null);
    }
}