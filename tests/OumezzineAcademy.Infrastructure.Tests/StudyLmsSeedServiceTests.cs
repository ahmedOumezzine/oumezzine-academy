using OumezzineAcademy.Infrastructure.Data;
using OumezzineAcademy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace OumezzineAcademy.Tests;

public sealed class StudyLmsSeedServiceTests
{
    [Fact]
    public async Task Demo_seed_is_idempotent_and_keeps_relations_consistent()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(nameof(Demo_seed_is_idempotent_and_keeps_relations_consistent))
            .Options;

        await using var db = new ApplicationDbContext(options);
        var seed = new StudyLmsSeedService(db, NullLogger<StudyLmsSeedService>.Instance);

        await seed.SeedAsync();
        var first = Counts(db);
        var firstCourseIds = await db.Courses.OrderBy(x => x.Slug).Select(x => x.Id).ToListAsync();

        await seed.SeedAsync();
        var second = Counts(db);
        var secondCourseIds = await db.Courses.OrderBy(x => x.Slug).Select(x => x.Id).ToListAsync();

        Assert.Equal(first, second);
        Assert.Equal(firstCourseIds, secondCourseIds);
        Assert.Equal(10, second.Courses);
        Assert.Equal(30, second.Chapters);
        Assert.Equal(60, second.Lessons);
        Assert.Equal(18, second.Quizzes);
        Assert.Equal(54, second.Questions);
        Assert.Equal(216, second.Answers);
        Assert.Equal(4, second.Paths);
        Assert.Equal(7, second.Prerequisites);
        Assert.Equal(0, await db.CourseTranslations.GroupBy(x => new { x.CourseId, x.LanguageCode }).Where(x => x.Count() > 1).CountAsync());
        Assert.Equal(0, await db.LearningPathCourses.GroupBy(x => new { x.LearningPathId, x.CourseId }).Where(x => x.Count() > 1).CountAsync());
        Assert.All(await db.CourseLessons.ToListAsync(), lesson => Assert.False(EfIsDeleted(db, lesson)));
    }

    private static bool EfIsDeleted(ApplicationDbContext db, object entity) => (bool)(db.Entry(entity).Property("IsDeleted").CurrentValue ?? true);

    private static SeedCounts Counts(ApplicationDbContext db) => new(
        db.CourseCategories.Count(), db.Courses.Count(), db.CourseContents.Count(), db.CourseLessons.Count(),
        db.CourseQuizzes.Count(), db.QuizQuestions.Count(), db.QuizAnswers.Count(), db.LearningPathCategories.Count(),
        db.LearningPaths.Count(), db.LearningPathCourses.Count(), db.CoursePrerequisites.Count());

    private sealed record SeedCounts(int Categories, int Courses, int Chapters, int Lessons, int Quizzes, int Questions, int Answers, int PathCategories, int Paths, int PathCourses, int Prerequisites);
}

