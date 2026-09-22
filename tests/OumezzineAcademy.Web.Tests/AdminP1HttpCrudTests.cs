using System.Net;
using Xunit;

namespace OumezzineAcademy.Tests;

public sealed class AdminP1HttpCrudTests
{
    [Theory]
    [InlineData("/admin/courses/10000000-0000-0000-0000-000000000001/chapters/20000000-0000-0000-0000-000000000001/quizzes")]
    [InlineData("/admin/courses/10000000-0000-0000-0000-000000000001/chapters/20000000-0000-0000-0000-000000000001/quizzes/30000000-0000-0000-0000-000000000001/questions")]
    [InlineData("/admin/learningpaths")]
    [InlineData("/admin/learningpathcategories")]
    public async Task Admin_crud_entry_pages_are_reachable(string path)
    {
        using var factory = new AdminWebApplicationFactory(AdminTestProfile.Admin);
        using var response = await factory.CreateClient().GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("/admin/courses/10000000-0000-0000-0000-000000000001/chapters/20000000-0000-0000-0000-000000000001/quizzes/create")]
    [InlineData("/admin/courses/10000000-0000-0000-0000-000000000001/chapters/20000000-0000-0000-0000-000000000001/quizzes/30000000-0000-0000-0000-000000000001/questions/create")]
    public async Task Quiz_and_question_mutations_require_antiforgery(string path)
    {
        using var factory = new AdminWebApplicationFactory(AdminTestProfile.Admin);
        using var client = factory.CreateClient();
        using var response = await client.PostAsync(path, new FormUrlEncodedContent([]));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("/admin/courses/10000000-0000-0000-0000-000000000099/chapters/20000000-0000-0000-0000-000000000001/quizzes")]
    [InlineData("/admin/courses/10000000-0000-0000-0000-000000000001/chapters/20000000-0000-0000-0000-000000000099/quizzes")]
    [InlineData("/admin/courses/10000000-0000-0000-0000-000000000001/chapters/20000000-0000-0000-0000-000000000001/quizzes/30000000-0000-0000-0000-000000000099/questions")]
    public async Task Invalid_nested_parents_are_rejected(string path)
    {
        using var factory = new AdminWebApplicationFactory(AdminTestProfile.Admin);
        using var response = await factory.CreateClient().GetAsync(path);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Learning_path_course_mutations_require_antiforgery()
    {
        using var factory = new AdminWebApplicationFactory(AdminTestProfile.Admin);
        using var client = factory.CreateClient();
        using var response = await client.PostAsync("/admin/learningpaths/10000000-0000-0000-0000-000000000099/courses/add", new FormUrlEncodedContent([]));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

