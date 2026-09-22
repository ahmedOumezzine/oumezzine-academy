using Microsoft.AspNetCore.Http;
using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Controllers;
using Xunit;

namespace OumezzineAcademy.Tests;

public sealed class SeoControllerTests
{
    [Fact]
    public void Robots_contains_host_and_sitemap()
    {
        var controller = new SeoController(new SitemapQueries());
        controller.ControllerContext.HttpContext = new DefaultHttpContext();
        controller.Request.Scheme = "https";
        controller.Request.Host = new HostString("academy.test");

        var result = Assert.IsType<Microsoft.AspNetCore.Mvc.ContentResult>(controller.Robots());
        Assert.Contains("https://academy.test/sitemap.xml", result.Content);
    }

    [Fact]
    public async Task Sitemap_contains_static_and_non_empty_dynamic_urls()
    {
        var controller = new SeoController(new SitemapQueries());
        controller.ControllerContext.HttpContext = new DefaultHttpContext();
        controller.Request.Host = new HostString("academy.test");

        var result = Assert.IsType<Microsoft.AspNetCore.Mvc.ContentResult>(await controller.Sitemap());
        Assert.Contains("https://academy.test/fr/cours/course", result.Content);
        Assert.DoesNotContain("/ignored", result.Content);
        Assert.Contains("application/xml", result.ContentType);
    }

    private sealed class SitemapQueries : ISitemapQueries
    {
        public Task<SitemapSlugs> GetSlugsAsync(string languageCode, CancellationToken cancellationToken = default)
            => Task.FromResult(new SitemapSlugs(["course", ""], ["category"], ["path"], ["lesson"]));
    }
}