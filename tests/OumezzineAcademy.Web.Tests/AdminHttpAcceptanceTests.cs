using System.Net;
using Xunit;

namespace OumezzineAcademy.Tests;

public sealed class AdminHttpAcceptanceTests
{
    [Theory]
    [InlineData("/admin")]
    [InlineData("/admin/courses")]
    [InlineData("/admin/categories")]
    [InlineData("/admin/learningpaths")]
    public async Task Anonymous_admin_pages_are_protected(string path)
    {
        using var factory = new AdminWebApplicationFactory(AdminTestProfile.Anonymous);
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        var response = await client.GetAsync(path);
        Assert.Contains(response.StatusCode, new[] { HttpStatusCode.Redirect, HttpStatusCode.Forbidden, HttpStatusCode.Unauthorized });
    }

    [Fact]
    public async Task Authenticated_non_admin_is_forbidden()
    {
        using var factory = new AdminWebApplicationFactory(AdminTestProfile.User);
        using var response = await factory.CreateClient(new() { AllowAutoRedirect = false }).GetAsync("/admin");
        Assert.True(response.StatusCode == HttpStatusCode.Forbidden ||
                    (response.StatusCode == HttpStatusCode.Redirect &&
                     response.Headers.Location?.ToString().Contains("access-denied", StringComparison.OrdinalIgnoreCase) == true));
    }

    [Theory]
    [InlineData("/admin")]
    [InlineData("/admin/courses")]
    [InlineData("/admin/categories")]
    [InlineData("/admin/learningpaths")]
    public async Task Admin_pages_return_success(string path)
    {
        using var factory = new AdminWebApplicationFactory(AdminTestProfile.Admin);
        using var response = await factory.CreateClient().GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Content-Security-Policy", out var values));
        var csp = string.Join(" ", values!);
        Assert.Contains("default-src 'self'", csp);
        Assert.Contains("object-src 'none'", csp);
        Assert.DoesNotContain("unsafe-inline", csp, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Mutation_without_antiforgery_token_is_rejected()
    {
        using var factory = new AdminWebApplicationFactory(AdminTestProfile.Admin);
        using var client = factory.CreateClient();
        using var response = await client.PostAsync("/admin/categories/delete?id=00000000-0000-0000-0000-000000000001", new FormUrlEncodedContent([]));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("chapters")]
    [InlineData("chapters/20000000-0000-0000-0000-000000000001/lessons")]
    [InlineData("chapters/20000000-0000-0000-0000-000000000001/quizzes")]
    [InlineData("chapters/20000000-0000-0000-0000-000000000001/quizzes/30000000-0000-0000-0000-000000000001/questions")]
    public async Task Admin_nested_content_pages_return_success(string suffix)
    {
        using var factory = new AdminWebApplicationFactory(AdminTestProfile.Admin);
        using var response = await factory.CreateClient().GetAsync($"/admin/courses/{AdminWebApplicationFactory.CourseId}/{suffix}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}