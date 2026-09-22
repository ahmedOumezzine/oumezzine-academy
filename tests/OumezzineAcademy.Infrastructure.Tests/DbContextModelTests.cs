using Microsoft.EntityFrameworkCore;
using OumezzineAcademy.Infrastructure.Data;
using Xunit;

namespace OumezzineAcademy.Tests;

public sealed class DbContextModelTests
{
    [Fact]
    public void ApplicationContextContainsIdentityAndLms()
    {
        using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(nameof(ApplicationContextContainsIdentityAndLms))
            .Options);

        var entityNames = db.Model.GetEntityTypes().Select(x => x.ClrType.Name).ToHashSet();

        Assert.DoesNotContain("Article", entityNames);
        Assert.DoesNotContain("Project", entityNames);
        Assert.DoesNotContain("Exam", entityNames);
        Assert.DoesNotContain("BabyName", entityNames);
        Assert.Contains("CourseCategory", entityNames);
        Assert.Contains("Course", entityNames);
        Assert.Contains("CourseTranslation", entityNames);
        Assert.Contains("CoursePrerequisite", entityNames);
        Assert.Contains("CourseContent", entityNames);
        Assert.Contains("CourseLesson", entityNames);
        Assert.Contains("CourseQuiz", entityNames);
        Assert.Contains("QuizQuestion", entityNames);
        Assert.Contains("QuizAnswer", entityNames);
        Assert.Contains("LearningPath", entityNames);
        Assert.Contains("LearningPathCourse", entityNames);
    }
}