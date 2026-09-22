using Xunit;
using Microsoft.AspNetCore.Http;
using OumezzineAcademy.Web.Services;

namespace OumezzineAcademy.Tests;

public sealed class StudyLocalizationTests
{
    [Theory]
    [InlineData("/fr/cours/demo", "fr")]
    [InlineData("/EN/courses/demo", "en")]
    public async Task Route_provider_detects_supported_language(string path, string expected)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        var result = await new RouteLanguageRequestCultureProvider().DetermineProviderCultureResult(context);
        Assert.NotNull(result);
        Assert.Equal(expected, result!.Cultures[0].Value);
    }

    [Fact]
    public async Task Route_provider_returns_null_for_unknown_language()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/de/courses";
        Assert.Null(await new RouteLanguageRequestCultureProvider().DetermineProviderCultureResult(context));
    }
}
