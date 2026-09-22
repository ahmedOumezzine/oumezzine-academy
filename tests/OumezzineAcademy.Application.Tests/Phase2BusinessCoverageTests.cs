using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OumezzineAcademy.Application.UseCases;
using OumezzineAcademy.Infrastructure.Data;
using OumezzineAcademy.Infrastructure.Persistence;
using OumezzineAcademy.Web.Services;
using System.Globalization;
using Xunit;

namespace OumezzineAcademy.Tests;

public sealed class Phase2BusinessCoverageTests
{
    [Fact]
    public async Task CategoryAndCoursesUsePublishedTranslationsInCurrentLanguage()
    {
        await using var db = Database();
        var graph = SeedGraph(db, includeEnglish: true);
        var service = Catalog(db);

        SetCulture("en-US");
        var category = await service.GetCategoryAsync("frontend-en");

        Assert.NotNull(category);
        Assert.Equal("Frontend EN", category!.Title);
        Assert.Single(category.Courses);
        Assert.Equal("Course EN", category.Courses[0].Title);
        Assert.Null(await service.GetCategoryAsync("frontend-fr"));
        Assert.Equal(graph.CategoryId, graph.Category.Id);
    }

    [Fact]
    public async Task MissingOrDraftCategoryTranslationIsNotVisible()
    {
        await using var db = Database();
        SeedGraph(db, includeEnglish: false);
        SetCulture("en-US");

        Assert.Empty(await Catalog(db).GetCategoriesAsync());
    }

    [Fact]
    public async Task LearningPathFiltersDraftCoursesAndPreservesOrder()
    {
        await using var db = Database();
        var graph = SeedGraph(db, includeEnglish: true);
        var courseB = new Course { Id = Guid.NewGuid(), CourseCategoryId = graph.CategoryId, CourseCategory = graph.Category, Status = StudyStatus.Published, Level = StudyLevel.Beginner };
        courseB.Translations.Add(new CourseTranslation { Id = Guid.NewGuid(), CourseId = courseB.Id, LanguageCode = "en", PublicationStatus = StudyStatus.Draft, Title = "Course B", Slug = "course-b" });
        db.StudyCourses.Add(courseB);
        db.StudyLearningPathCourses.Add(new LearningPathCourse { Id = Guid.NewGuid(), LearningPathId = graph.Path.Id, CourseId = courseB.Id, Course = courseB, Order = 2 });
        await db.SaveChangesAsync();
        SetCulture("en-US");

        var result = await new LearningPathCatalogService(new EfLearningPathQueries(db), new CurrentLanguageService()).GetPathAsync("path-en");

        Assert.NotNull(result);
        Assert.Single(result!.Courses);
        Assert.Equal("Course EN", result.Courses[0].CourseTitle);
        Assert.Equal(graph.Course.Id, result.Courses[0].CourseId);
        Assert.Equal("course-en", result.Courses[0].CourseSlug);
        Assert.Equal("/uploads/courses/course.webp", result.Courses[0].ThumbnailUrl);
        Assert.Equal(1, result.Courses[0].Order);
    }

    [Fact]
    public async Task LearningPathCourseWithoutThumbnailKeepsAnEmptyImageForTheViewFallback()
    {
        await using var db = Database();
        var graph = SeedGraph(db, includeEnglish: true);
        graph.Course.Thumbnail = null;
        await db.SaveChangesAsync();
        SetCulture("fr-FR");

        var result = await new LearningPathCatalogService(new EfLearningPathQueries(db), new CurrentLanguageService()).GetPathAsync("path-fr");

        Assert.NotNull(result);
        Assert.Single(result!.Courses);
        Assert.Null(result.Courses[0].ThumbnailUrl);
        Assert.Equal("course-fr", result.Courses[0].CourseSlug);
    }

    [Fact]
    public async Task LessonSupportsTextWithoutVideoAndRejectsWrongLanguage()
    {
        await using var db = Database();
        SeedGraph(db, includeEnglish: true);
        SetCulture("en-US");

        var lesson = await Catalog(db).GetLessonAsync("lesson-en");

        Assert.NotNull(lesson);
        Assert.Equal("<p>Text</p>", lesson!.Description);
        Assert.Null(lesson.VideoUrl);
        SetCulture("fr-FR");
        Assert.Null(await Catalog(db).GetLessonAsync("lesson-en"));
    }

    [Fact]
    public async Task QuizIsUnavailableWhenAnAnswerTranslationIsMissing()
    {
        await using var db = Database();
        var graph = SeedGraph(db, includeEnglish: true, includeEnglishAnswer: false);
        SetCulture("en-US");

        Assert.Null(await new QuizService(new EfQuizQueries(db), new QuizSubmissionHandler(new EfQuizQueries(db)), new CurrentLanguageService()).GetQuizAsync("quiz-en"));
        Assert.False(db.StudyQuizAnswerTranslations.Any(t => t.QuizAnswerId == graph.Answer.Id && t.LanguageCode == "en"));
        Assert.Contains(db.StudyQuizAnswers, a => a.IsCorrect);
    }

    [Fact]
    public async Task CompleteQuizIsAvailableAndUsesThePrincipalCorrectnessFlag()
    {
        await using var db = Database();
        var graph = SeedGraph(db, includeEnglish: true);
        SetCulture("en-US");

        var quiz = await new QuizService(new EfQuizQueries(db), new QuizSubmissionHandler(new EfQuizQueries(db)), new CurrentLanguageService()).GetQuizAsync("quiz-en");

        Assert.NotNull(quiz);
        Assert.Single(quiz!.Questions);
        Assert.True(graph.Answer.IsCorrect);
        Assert.DoesNotContain(typeof(QuizAnswerTranslation).GetProperties(), property => property.Name == "IsCorrect");
    }

    [Fact]
    public async Task QuizResultRetainsThePublishedSlugInTheCurrentLanguage()
    {
        await using var db = Database();
        SeedGraph(db, includeEnglish: true);
        SetCulture("en-US");
        var quiz = await db.StudyCourseQuizzes.SingleAsync();
        var question = await db.StudyQuizQuestions.SingleAsync();
        var answer = await db.StudyQuizAnswers.SingleAsync();
        var form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            [$"question_{question.Id}"] = answer.Id.ToString()
        });

        var result = await new QuizService(new EfQuizQueries(db), new QuizSubmissionHandler(new EfQuizQueries(db)), new CurrentLanguageService()).GradeAsync(quiz.Id, form);

        Assert.NotNull(result);
        Assert.Equal("quiz-en", result!.QuizSlug);
    }

    [Fact]
    public async Task DraftQuizIsNotAvailable()
    {
        await using var db = Database();
        var graph = SeedGraph(db, includeEnglish: true);
        var quiz = await db.StudyCourseQuizzes.SingleAsync();
        quiz.Status = StudyStatus.Draft;
        await db.SaveChangesAsync();
        SetCulture("en-US");

        Assert.Null(await new QuizService(new EfQuizQueries(db), new QuizSubmissionHandler(new EfQuizQueries(db)), new CurrentLanguageService()).GetQuizAsync("quiz-en"));
        _ = graph;
    }

    [Fact]
    public async Task LessonSupportsVideoAndDocumentWhenProvided()
    {
        await using var db = Database();
        SeedGraph(db, includeEnglish: true);
        var translation = await db.StudyCourseLessonTranslations.SingleAsync();
        translation.VideoUrl = "https://example.test/video";
        translation.DocumentUrl = "/docs/lesson.pdf";
        await db.SaveChangesAsync();
        SetCulture("en-US");

        var lesson = await Catalog(db).GetLessonAsync("lesson-en");

        Assert.Equal("https://example.test/video", lesson!.VideoUrl);
        Assert.Equal("/docs/lesson.pdf", lesson.DocumentUrl);
    }

    [Fact]
    public async Task EntityLocalizedUrlUsesPublishedTargetSlug()
    {
        await using var db = Database();
        SeedGraph(db, includeEnglish: true);
        var service = new LocalizedUrlService(new EfLocalizedSlugQueries(db));
        var context = new DefaultHttpContext();
        context.Request.Path = "/fr/cours/course-fr";
        context.Request.RouteValues["slug"] = "course-fr";

        var urls = await service.ResolveAsync(context);

        Assert.Equal("/fr/cours/course-fr", urls.French);
        Assert.Equal("/en/courses/course-en", urls.English);
    }

    [Theory]
    [InlineData("/fr/cours", "/en/courses")]
    [InlineData("/fr/categories", "/en/categories")]
    [InlineData("/fr/parcours", "/en/learning-paths")]
    [InlineData("/fr/a-propos", "/en/about")]
    public async Task GeneralLocalizedUrlsPairFrenchAndEnglish(string french, string english)
    {
        await using var db = Database();
        var service = new LocalizedUrlService(new EfLocalizedSlugQueries(db));
        var context = new DefaultHttpContext();
        context.Request.Path = french;

        var urls = await service.ResolveAsync(context);

        Assert.Equal(french, urls.French);
        Assert.Equal(english, urls.English);
    }

    private static ApplicationDbContext Database() => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static CourseCatalogService Catalog(ApplicationDbContext db) => new(new EfCourseCatalogQueries(db), new CurrentLanguageService());

    private static void SetCulture(string name) => CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(name);

    private static Graph SeedGraph(ApplicationDbContext db, bool includeEnglish, bool includeEnglishAnswer = true)
    {
        var category = new CourseCategory { Id = Guid.NewGuid(), Status = StudyStatus.Published };
        category.Translations.Add(new CourseCategoryTranslation { Id = Guid.NewGuid(), CourseCategoryId = category.Id, LanguageCode = "fr", PublicationStatus = StudyStatus.Published, Title = "Frontend FR", Slug = "frontend-fr" });
        if (includeEnglish) category.Translations.Add(new CourseCategoryTranslation { Id = Guid.NewGuid(), CourseCategoryId = category.Id, LanguageCode = "en", PublicationStatus = StudyStatus.Published, Title = "Frontend EN", Slug = "frontend-en" });
        var course = new Course { Id = Guid.NewGuid(), CourseCategory = category, CourseCategoryId = category.Id, Status = StudyStatus.Published, Level = StudyLevel.Beginner, Thumbnail = "/uploads/courses/course.webp" };
        course.Translations.Add(new CourseTranslation { Id = Guid.NewGuid(), CourseId = course.Id, LanguageCode = "fr", PublicationStatus = StudyStatus.Published, Title = "Course FR", Slug = "course-fr", Summary = "Résumé FR" });
        if (includeEnglish) course.Translations.Add(new CourseTranslation { Id = Guid.NewGuid(), CourseId = course.Id, LanguageCode = "en", PublicationStatus = StudyStatus.Published, Title = "Course EN", Slug = "course-en", Summary = "Summary EN" });
        var content = new CourseContent { Id = Guid.NewGuid(), Course = course, CourseId = course.Id, Status = StudyStatus.Published, Order = 1 };
        content.Translations.Add(new CourseContentTranslation { Id = Guid.NewGuid(), CourseContentId = content.Id, LanguageCode = "en", PublicationStatus = StudyStatus.Published, Title = "Chapter EN" });
        var lesson = new CourseLesson { Id = Guid.NewGuid(), CourseContent = content, CourseContentId = content.Id, Status = StudyStatus.Published, Order = 1, DurationMinutes = 20 };
        lesson.Translations.Add(new CourseLessonTranslation { Id = Guid.NewGuid(), CourseLessonId = lesson.Id, LanguageCode = "en", PublicationStatus = StudyStatus.Published, Title = "Lesson EN", Slug = "lesson-en", Summary = "Lesson", ContentHtml = "<p>Text</p>" });
        var quiz = new CourseQuiz { Id = Guid.NewGuid(), CourseContent = content, CourseContentId = content.Id, Status = StudyStatus.Published, Order = 1 };
        quiz.Translations.Add(new CourseQuizTranslation { Id = Guid.NewGuid(), CourseQuizId = quiz.Id, LanguageCode = "en", PublicationStatus = StudyStatus.Published, Title = "Quiz EN", Slug = "quiz-en" });
        var question = new QuizQuestion { Id = Guid.NewGuid(), CourseQuiz = quiz, CourseQuizId = quiz.Id, Order = 1 };
        question.Translations.Add(new QuizQuestionTranslation { Id = Guid.NewGuid(), QuizQuestionId = question.Id, LanguageCode = "en", PublicationStatus = StudyStatus.Published, Text = "Question" });
        var answer = new QuizAnswer { Id = Guid.NewGuid(), QuizQuestion = question, QuizQuestionId = question.Id, IsCorrect = true };
        if (includeEnglishAnswer) answer.Translations.Add(new QuizAnswerTranslation { Id = Guid.NewGuid(), QuizAnswerId = answer.Id, LanguageCode = "en", PublicationStatus = StudyStatus.Published, Text = "Answer" });
        question.Answers.Add(answer); quiz.QuizQuestions.Add(question); content.CourseLessons.Add(lesson); content.CourseQuizzes.Add(quiz); course.CourseContents.Add(content);
        var pathCategory = new LearningPathCategory { Id = Guid.NewGuid(), Status = StudyStatus.Published };
        pathCategory.Translations.Add(new LearningPathCategoryTranslation { Id = Guid.NewGuid(), LearningPathCategoryId = pathCategory.Id, LanguageCode = "en", PublicationStatus = StudyStatus.Published, Title = "Path category", Slug = "path-category" });
        var path = new LearningPath { Id = Guid.NewGuid(), LearningPathCategory = pathCategory, LearningPathCategoryId = pathCategory.Id, Status = StudyStatus.Published, Level = StudyLevel.Beginner };
        path.Translations.Add(new LearningPathTranslation { Id = Guid.NewGuid(), LearningPathId = path.Id, LanguageCode = "en", PublicationStatus = StudyStatus.Published, Title = "Path EN", Slug = "path-en" });
        path.Translations.Add(new LearningPathTranslation { Id = Guid.NewGuid(), LearningPathId = path.Id, LanguageCode = "fr", PublicationStatus = StudyStatus.Published, Title = "Parcours FR", Slug = "path-fr" });
        path.LearningPathCourses.Add(new LearningPathCourse { Id = Guid.NewGuid(), LearningPathId = path.Id, CourseId = course.Id, Course = course, Order = 1 });
        db.StudyCourses.Add(course); db.StudyLearningPaths.Add(path); db.SaveChanges();
        return new Graph(category.Id, category, course, path, answer);
    }

    private sealed record Graph(Guid CategoryId, CourseCategory Category, Course Course, LearningPath Path, QuizAnswer Answer);
}