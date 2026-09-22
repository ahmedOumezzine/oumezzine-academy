using Xunit;

namespace OumezzineAcademy.Tests;

public sealed class AdminCategoryCardTests
{
    [Fact]
    public void Category_index_uses_explicit_bilingual_status_sections()
    {
        var view = File.ReadAllText(ProjectFile("Areas/Admin/Views/Categories/Index.cshtml"));

        Assert.Contains("Titre français", view);
        Assert.Contains("Titre anglais", view);
        Assert.Contains("_AdminStatusBadge", view);
        Assert.Contains("CourseCount", view);
        Assert.Contains("asp-action=\"Edit\"", view);
        Assert.Contains("asp-action=\"Delete\"", view);
        Assert.Contains("method=\"post\"", view);
        Assert.DoesNotContain("FR : OK", view);
        Assert.DoesNotContain("EN : Manquant", view);
        Assert.DoesNotContain("?? x.Title", view);
    }

    private static string ProjectFile(string relativePath) => TestProjectFiles.FindOumezzineAcademyFile(relativePath);
}