using Microsoft.EntityFrameworkCore;
using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Infrastructure.Data;
using OumezzineAcademy.Infrastructure.Persistence;
using Xunit;

namespace OumezzineAcademy.Tests;

public sealed class QuestionPersistenceTests
{
    [Fact]
    public async Task Saves_question_with_two_answers_and_one_correct_answer()
    {
        await using var db = CreateDb(out var courseId, out var chapterId, out var quizId);
        var command = new EfAdminQuestionCommands(db);
        var result = await command.SaveAsync(Save(null, courseId, chapterId, quizId, Answers()));
        var saved = await db.QuizQuestions.Include(x => x.Answers).SingleAsync();

        Assert.NotEqual(Guid.Empty, result);
        Assert.Equal(2, saved.Answers.Count);
        Assert.Single(saved.Answers, x => x.IsCorrect);
    }

    [Fact]
    public async Task Rejects_missing_quiz_and_invalid_answer_sets()
    {
        await using var db = CreateDb(out var courseId, out var chapterId, out var quizId);
        var command = new EfAdminQuestionCommands(db);
        await Assert.ThrowsAsync<InvalidOperationException>(() => command.SaveAsync(Save(null, courseId, chapterId, Guid.NewGuid(), Answers())));
        await Assert.ThrowsAsync<InvalidOperationException>(() => command.SaveAsync(Save(null, courseId, chapterId, quizId, [new(Guid.Empty, "Only", null, true)])));
        await Assert.ThrowsAsync<InvalidOperationException>(() => command.SaveAsync(Save(null, courseId, chapterId, quizId, [new(Guid.Empty, "A", null, true), new(Guid.Empty, "B", null, true)])));
    }

    [Fact]
    public async Task Deletes_question_and_returns_not_found_for_unknown_id()
    {
        await using var db = CreateDb(out var courseId, out var chapterId, out var quizId);
        var question = new QuizQuestion { Id = Guid.NewGuid(), CourseQuizId = quizId, Text = "Question", Order = 1 };
        db.QuizQuestions.Add(question);
        await db.SaveChangesAsync();
        var command = new EfAdminQuestionCommands(db);
        Assert.Equal(AdminQuestionDeleteStatus.Deleted, await command.DeleteAsync(courseId, chapterId, quizId, question.Id));
        Assert.Equal(AdminQuestionDeleteStatus.NotFound, await command.DeleteAsync(courseId, chapterId, quizId, Guid.NewGuid()));
    }

    private static AdminQuestionSaveCommand Save(Guid? id, Guid courseId, Guid chapterId, Guid quizId, IReadOnlyList<AdminAnswerDto> answers)
        => new(id, courseId, chapterId, quizId, 1, "Question", StudyStatus.Published, "Question", StudyStatus.Published, answers);

    private static IReadOnlyList<AdminAnswerDto> Answers()
        => [new(Guid.Empty, "Oui", "Yes", true), new(Guid.Empty, "Non", "No", false)];

    private static ApplicationDbContext CreateDb(out Guid courseId, out Guid chapterId, out Guid quizId)
    {
        courseId = Guid.NewGuid(); chapterId = Guid.NewGuid(); quizId = Guid.NewGuid();
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var category = new CourseCategory { Id = Guid.NewGuid(), Title = "Category" };
        db.Courses.Add(new Course { Id = courseId, CourseCategory = category, Title = "Course" });
        db.CourseContents.Add(new CourseContent { Id = chapterId, CourseId = courseId, Title = "Chapter" });
        db.CourseQuizzes.Add(new CourseQuiz { Id = quizId, CourseContentId = chapterId, Title = "Quiz", Slug = "quiz" });
        db.SaveChanges();
        return db;
    }
}