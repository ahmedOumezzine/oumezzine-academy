using Xunit;

namespace OumezzineAcademy.Tests;

public sealed class AdminNavigationTests
{
    [Fact]
    public void Shared_admin_layout_contains_the_centralized_section_matrix()
    {
        var path = TestProjectFiles.FindOumezzineAcademyFile("Areas/Admin/Views/Shared/_Layout.cshtml");
        var layout = File.ReadAllText(path);

        Assert.Contains("IsAdminSectionActive", layout);
        Assert.Contains("IsAdminSectionActive(\"Dashboard\")", layout);
        Assert.Contains("IsAdminSectionActive(\"Courses\", \"Chapters\", \"Lessons\", \"Quizzes\", \"Questions\", \"Answers\")", layout);
        Assert.Contains("IsAdminSectionActive(\"Categories\")", layout);
        Assert.Contains("IsAdminSectionActive(\"LearningPaths\", \"LearningPathCategories\", \"LearningPathCourses\")", layout);
        Assert.Contains("href=\"@publicHome\"", layout);
        Assert.Contains("aria-current=\"@(coursesActive ? \"page\" : null)\"", layout);
        Assert.DoesNotContain("Action ==", layout);
    }
}