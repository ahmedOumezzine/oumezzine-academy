using AhmedOumezzine.EFCore.Repository.Extensions;
using AhmedOumezzine.EFCore.Repository.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Infrastructure.Data;
using OumezzineAcademy.Infrastructure.Persistence;
using Xunit;

namespace OumezzineAcademy.Tests;

public sealed class QuizPersistenceTests
{
    [Fact]
    public async Task Saves_new_quiz_and_normalizes_order()
    {
        await using var db = CreateDb(out var courseId, out var chapterId);
        var persistence = new EfAdminQuizPersistence(CreateRepository(db));
        var result = await persistence.SaveAsync(Command(null, courseId, chapterId, 4));

        Assert.True(result.Success);
        var quiz = await db.CourseQuizzes.Include(x => x.Translations).SingleAsync();
        Assert.Equal(1, quiz.Order);
        Assert.Equal("quiz", quiz.Slug);
        Assert.Equal(2, quiz.Translations.Count);
    }

    [Fact]
    public async Task Deletes_empty_quiz_but_rejects_quiz_with_question()
    {
        await using var db = CreateDb(out var courseId, out var chapterId);
        var quiz = new CourseQuiz { Id = Guid.NewGuid(), CourseContentId = chapterId, Title = "Quiz", Slug = "quiz", Order = 1 };
        db.CourseQuizzes.Add(quiz);
        await db.SaveChangesAsync();
        var persistence = new EfAdminQuizPersistence(CreateRepository(db));

        var deleted = await persistence.DeleteAsync(courseId, chapterId, quiz.Id);
        Assert.True(deleted.Success);

        var blocked = new CourseQuiz { Id = Guid.NewGuid(), CourseContentId = chapterId, Title = "Blocked", Slug = "blocked", Order = 1 };
        blocked.QuizQuestions.Add(new QuizQuestion { Id = Guid.NewGuid(), CourseQuizId = blocked.Id, Text = "Question", Order = 1 });
        db.CourseQuizzes.Add(blocked);
        await db.SaveChangesAsync();
        var rejected = await persistence.DeleteAsync(courseId, chapterId, blocked.Id);
        Assert.False(rejected.Success);
        Assert.Contains("questions", rejected.Message);
    }

    private static IRepository CreateRepository(ApplicationDbContext db)
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => db);
        services.AddGenericRepository<ApplicationDbContext>();
        return services.BuildServiceProvider().GetRequiredService<IRepository>();
    }

    private static ApplicationDbContext CreateDb(out Guid courseId, out Guid chapterId)
    {
        courseId = Guid.NewGuid(); chapterId = Guid.NewGuid();
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var category = new CourseCategory { Id = Guid.NewGuid(), Title = "Category" };
        db.Courses.Add(new Course { Id = courseId, Title = "Course", CourseCategory = category });
        db.CourseContents.Add(new CourseContent { Id = chapterId, CourseId = courseId, Title = "Chapter", Order = 1 });
        db.SaveChanges();
        return db;
    }

    private static AdminQuizSaveCommand Command(Guid? id, Guid courseId, Guid chapterId, int order)
        => new(id, courseId, chapterId, order,
            new("Quiz", "quiz", "Résumé", StudyStatus.Published),
            new("Quiz", "quiz-en", "Summary", StudyStatus.Published));
}
