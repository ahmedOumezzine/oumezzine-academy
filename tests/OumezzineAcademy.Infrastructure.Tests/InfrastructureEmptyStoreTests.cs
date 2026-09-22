using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Domain.Catalog;
using OumezzineAcademy.Infrastructure.Data;
using OumezzineAcademy.Infrastructure;
using OumezzineAcademy.Infrastructure.Persistence;
using OumezzineAcademy.Infrastructure.Sanitization;
using Xunit;

namespace OumezzineAcademy.Tests;

public sealed class InfrastructureEmptyStoreTests
{
    [Fact]
    public async Task Public_catalog_and_navigation_queries_handle_empty_store()
    {
        await using var db = CreateDb();
        var catalog = new EfCourseCatalogQueries(db);
        var paths = new EfLearningPathQueries(db);
        var quizzes = new EfQuizQueries(db);
        var slugs = new EfLocalizedSlugQueries(db);
        var prerequisites = new EfCoursePrerequisiteEdges(db);

        Assert.Empty((await catalog.GetHomeAsync("fr")).LatestCourses);
        Assert.Null(await catalog.GetCourseAsync("missing", "fr"));
        Assert.Null(await catalog.GetLessonAsync("missing", "fr"));
        Assert.Empty(await catalog.GetCategoriesAsync("fr"));
        Assert.Null(await catalog.GetCategoryAsync("missing", "fr"));
        Assert.Empty((await catalog.SearchCoursesAsync(new(null, null, null, null, 1, 10, "fr"))).Items);
        Assert.Empty(await paths.GetPathsAsync(null, null, "fr"));
        Assert.Null(await paths.GetPathAsync("missing", "fr"));
        Assert.Null(await quizzes.GetQuizAsync("missing", "fr"));
        Assert.Null(await quizzes.GetGradingDataAsync(Guid.NewGuid(), "fr"));
        Assert.Null(await slugs.FindAsync("course", "missing", "fr"));
        Assert.Null(await slugs.FindAsync("category", "missing", "fr"));
        Assert.Null(await slugs.FindAsync("path", "missing", "fr"));
        Assert.Null(await slugs.FindAsync("lesson", "missing", "fr"));
        Assert.Null(await slugs.FindAsync("quiz", "missing", "fr"));
        Assert.Null(await slugs.FindAsync("unknown", "missing", "fr"));
        Assert.Empty(await prerequisites.GetAllAsync());
    }

    [Fact]
    public void Infrastructure_registration_exposes_all_canonical_ports()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure(Directory.GetCurrentDirectory());

        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IMediaStorage));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IAdminChapterMediaCommands));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IAdminLearningPathCoreCommands));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IAdminLearningPathCommands));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IStudyLmsSeeder));
    }

    [Fact]
    public void Design_time_factory_uses_configured_connection_string()
    {
        const string variable = "ConnectionStrings__DefaultConnection";
        var previous = Environment.GetEnvironmentVariable(variable);
        try
        {
            Environment.SetEnvironmentVariable(variable, "Server=(localdb)\\mssqllocaldb;Database=Coverage;Trusted_Connection=True;");
            using var db = new ApplicationDbContextFactory().CreateDbContext([]);
            Assert.Contains("Coverage", db.Database.GetDbConnection().ConnectionString);
        }
        finally
        {
            Environment.SetEnvironmentVariable(variable, previous);
        }
    }

    [Fact]
    public void Catalog_pending_guard_keeps_unsupported_projection_explicit()
    {
        var method = typeof(EfCourseCatalogQueries).GetMethod("Pending", BindingFlags.Static | BindingFlags.NonPublic);
        var exception = Assert.IsType<NotSupportedException>(method!.Invoke(null, null));
        Assert.Contains("next catalog step", exception.Message);
    }

    [Fact]
    public async Task Admin_query_ports_handle_missing_entities()
    {
        await using var db = CreateDb();
        var id = Guid.NewGuid();

        var categories = new EfAdminCategoryPersistence(db);
        Assert.Empty(await categories.ListAsync());
        Assert.Null(await categories.GetForEditAsync(id));
        Assert.False(await categories.SlugExistsAsync("fr", "missing", id));

        var courses = new EfAdminCourseQueries(db);
        Assert.Empty((await courses.ListAsync(null, null, null, null, null, null, 1, 10)).Items);
        Assert.Null(await courses.GetForEditAsync(id));
        Assert.Empty(await courses.GetCategoryOptionsAsync());
        Assert.Empty(await courses.GetPrerequisiteOptionsAsync());
        Assert.Null(await courses.GetThumbnailAsync(id));

        var chapters = new EfAdminChapterQueries(db);
        Assert.Null(await chapters.ListAsync(id));
        Assert.Null(await chapters.GetForEditAsync(id, null));

        var lessons = new EfAdminLessonQueries(db);
        Assert.Null(await lessons.ListAsync(id, id));
        Assert.Null(await lessons.GetForEditAsync(id, id, null));
        Assert.False(await lessons.SlugExistsAsync("fr", "missing", id));

        var quizzes = new EfAdminQuizQueries(db);
        Assert.Null(await quizzes.ListAsync(id, id));
        Assert.Null(await quizzes.GetForEditAsync(id, id, null));
        Assert.False(await quizzes.SlugExistsAsync("fr", "missing", id));

        var questions = new EfAdminQuestionQueries(db);
        Assert.Null(await questions.ListAsync(id, id, id));
        Assert.Null(await questions.GetFormAsync(id, id, id, null));

        var pathCategories = new EfAdminLearningPathCategoryQueries(db);
        Assert.Empty(await pathCategories.ListAsync());
        Assert.Null(await pathCategories.GetForEditAsync(id));
        Assert.False(await pathCategories.SlugExistsAsync("fr", "missing", id));

        var pathQueries = new EfAdminLearningPathQueries(db);
        Assert.Empty(await pathQueries.ListAsync());
        Assert.NotNull(await pathQueries.GetForEditAsync(null));
        Assert.False(await pathQueries.SlugExistsAsync("fr", "missing", id));
        Assert.Null(await pathQueries.GetCompositionAsync(id));

        var sitemap = new EfSitemapQueries(db);
        Assert.Empty((await sitemap.GetSlugsAsync("fr")).Courses);
        var dashboard = new EfDashboardQueries(db);
        Assert.Empty((await dashboard.GetSummaryAsync()).RecentItems);
    }

    [Fact]
    public async Task Admin_media_and_delete_commands_return_safe_missing_results()
    {
        await using var db = CreateDb();
        var id = Guid.NewGuid();
        var media = new MediaSpy();
        var upload = new MediaUpload("image.png", "image/png", 0, new MemoryStream());

        Assert.Null(await new EfAdminChapterMediaCommands(db, media).UploadAsync(id, id, upload));
        Assert.Equal(new(false, null), await new EfAdminLessonMediaCommands(db, media).UploadAsync(id, upload));
        Assert.Null(await new EfAdminLearningPathMediaCommands(db, media).UploadAsync(id, upload));
        Assert.Equal(new(false, null), await new EfAdminCourseMediaCommands(db, media).UploadAsync(id, upload));
        Assert.False(await new EfAdminCourseMediaCommands(db, media).RemoveAsync(id));
        Assert.True((await new EfAdminCourseDeleteCommands(db, media).DeleteAsync(id)).NotFound);
        Assert.True((await new EfAdminLessonDeleteCommands(db).DeleteAsync(id, id, id)).NotFound);
        Assert.Equal(AdminLearningPathCategoryDeleteStatus.NotFound, await new EfAdminLearningPathCategoryCommands(db).DeleteAsync(id));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => new EfAdminLearningPathCommands(db).DeleteAsync(id));
    }

    [Fact]
    public async Task Course_and_lesson_media_commands_preserve_replacement_and_safety_rules()
    {
        await using var db = CreateDb();
        var category = new CourseCategory { Id = Guid.NewGuid(), Title = "Category" };
        var course = new Course { Id = Guid.NewGuid(), CourseCategoryId = category.Id, Thumbnail = "/uploads/courses/old.png" };
        var chapter = new CourseContent { Id = Guid.NewGuid(), CourseId = course.Id, Title = "Chapter" };
        var lesson = new CourseLesson { Id = Guid.NewGuid(), CourseContentId = chapter.Id, Title = "Lesson" };
        db.CourseCategories.Add(category);
        db.Courses.Add(course);
        db.CourseContents.Add(chapter);
        db.CourseLessons.Add(lesson);
        await db.SaveChangesAsync();
        var media = new MediaSpy();
        var upload = new MediaUpload("image.png", "image/png", 0, new MemoryStream());

        var courseMedia = new EfAdminCourseMediaCommands(db, media);
        var uploaded = await courseMedia.UploadAsync(course.Id, upload);
        Assert.True(uploaded.Found);
        Assert.Equal(uploaded.StoredPath, (await db.Courses.FindAsync(course.Id))!.Thumbnail);
        Assert.Contains(media.Deleted, path => path.EndsWith("old.png", StringComparison.Ordinal));
        Assert.True(await courseMedia.RemoveAsync(course.Id));
        (await db.Courses.FindAsync(course.Id))!.Thumbnail = "/uploads/courses/throw.png";
        await db.SaveChangesAsync();
        media.ThrowOnDelete = true;
        await courseMedia.UploadAsync(course.Id, upload);

        media.ThrowOnDelete = false;
        var lessonMedia = new EfAdminLessonMediaCommands(db, media);
        var lessonUpload = await lessonMedia.UploadAsync(lesson.Id, upload);
        Assert.True(lessonUpload.Found);
        await lessonMedia.RemoveAsync(lesson.Id, lessonUpload.StoredPath!);
        media.Safe = false;
        await Assert.ThrowsAsync<ArgumentException>(() => lessonMedia.RemoveAsync(lesson.Id, "/uploads/lessons/unsafe.png"));

        media.ThrowOnDelete = false;
        var chapterMedia = new EfAdminChapterMediaCommands(db, media);
        Assert.NotNull(await chapterMedia.UploadAsync(course.Id, chapter.Id, upload));
    }

    [Fact]
    public async Task Course_delete_commands_reject_dependencies_and_delete_orphans()
    {
        await using var db = CreateDb();
        var category = new CourseCategory { Id = Guid.NewGuid(), Title = "Category" };
        var course = new Course { Id = Guid.NewGuid(), CourseCategoryId = category.Id, Thumbnail = "/uploads/courses/delete.png" };
        db.CourseCategories.Add(category);
        db.Courses.Add(course);
        await db.SaveChangesAsync();
        var commands = new EfAdminCourseDeleteCommands(db, new MediaSpy());

        Assert.True((await commands.DeleteAsync(Guid.Empty)).NotFound);
        var chapter = new CourseContent { Id = Guid.NewGuid(), CourseId = course.Id, Title = "Chapter" };
        db.CourseContents.Add(chapter);
        await db.SaveChangesAsync();
        Assert.False((await commands.DeleteAsync(course.Id)).Success);
        db.CourseContents.Remove(chapter);
        await db.SaveChangesAsync();
        Assert.True((await commands.DeleteAsync(course.Id)).Success);
    }

    [Fact]
    public async Task Course_category_persistence_supports_save_edit_and_delete_rules()
    {
        await using var db = CreateDb();
        var persistence = new EfAdminCategoryPersistence(db);
        var command = new AdminCategorySaveCommand(null,
            new(" French ", " french ", "Summary", "Meta", "Description", StudyStatus.Published),
            new(" English ", " english ", null, null, null, StudyStatus.Draft));

        await persistence.SaveAsync(command);
        var category = await db.CourseCategories.SingleAsync();
        Assert.Equal(" French ", category.Title);
        Assert.Equal(" french ", category.Slug);
        Assert.NotNull(await persistence.GetForEditAsync(category.Id));
        Assert.True(await persistence.SlugExistsAsync("fr", "french", Guid.NewGuid()));

        await persistence.SaveAsync(command with { Id = category.Id, French = command.French with { Title = "Updated" } });
        Assert.Equal("Updated", (await db.CourseCategories.FindAsync(category.Id))!.Title);

        db.Courses.Add(new Course { Id = Guid.NewGuid(), CourseCategoryId = category.Id });
        await db.SaveChangesAsync();
        Assert.Equal(AdminCategoryDeleteStatus.InUse, await persistence.DeleteAsync(category.Id));
    }

    [Fact]
    public async Task Learning_path_composition_commands_add_remove_move_and_sync()
    {
        await using var db = CreateDb();
        var category = new LearningPathCategory { Id = Guid.NewGuid(), Title = "Paths" };
        var path = new LearningPath { Id = Guid.NewGuid(), LearningPathCategoryId = category.Id, Title = "Path" };
        var course1 = new Course { Id = Guid.NewGuid(), Title = "One" };
        var course2 = new Course { Id = Guid.NewGuid(), Title = "Two" };
        db.LearningPathCategories.Add(category);
        db.LearningPaths.Add(path);
        db.Courses.AddRange(course1, course2);
        await db.SaveChangesAsync();
        var commands = new EfAdminLearningPathCommands(db);

        await commands.AddCourseAsync(path.Id, course1.Id);
        await Assert.ThrowsAsync<InvalidOperationException>(() => commands.AddCourseAsync(path.Id, course1.Id));
        await commands.AddCourseAsync(path.Id, course2.Id);
        var links = await db.LearningPathCourses.OrderBy(x => x.Order).ToListAsync();
        await commands.MoveCourseAsync(path.Id, links[1].Id, -1);
        await commands.RemoveCourseAsync(path.Id, links[0].Id);
        await commands.SynchronizeCoursesAsync(path.Id, [course2.Id]);
        Assert.Single(await db.LearningPathCourses.Where(x => x.LearningPathId == path.Id).ToListAsync());
    }

    [Fact]
    public async Task Published_catalog_queries_project_details_search_and_learning_paths()
    {
        await using var db = CreateDb();
        var category = new CourseCategory { Id = Guid.NewGuid(), Status = StudyStatus.Published, Title = "Category" };
        category.Translations.Add(new CourseCategoryTranslation { Id = Guid.NewGuid(), CourseCategoryId = category.Id, LanguageCode = "fr", PublicationStatus = StudyStatus.Published, Title = "Catégorie", Slug = "categorie", Summary = "Résumé" });
        var course = new Course { Id = Guid.NewGuid(), CourseCategoryId = category.Id, Status = StudyStatus.Published, Level = StudyLevel.Advanced, CreatedOnUtc = DateTime.UtcNow, Thumbnail = "/course.png" };
        course.Translations.Add(new CourseTranslation { Id = Guid.NewGuid(), CourseId = course.Id, LanguageCode = "fr", PublicationStatus = StudyStatus.Published, Title = "Cours avancé", Slug = "cours-avance", Summary = "Résumé cours", Overview = "Vue", MetaTitle = "Meta", MetaDescription = "Description" });
        var chapter = new CourseContent { Id = Guid.NewGuid(), CourseId = course.Id, Status = StudyStatus.Published, Order = 1, Slug = "chapitre" };
        chapter.Translations.Add(new CourseContentTranslation { Id = Guid.NewGuid(), CourseContentId = chapter.Id, LanguageCode = "fr", PublicationStatus = StudyStatus.Published, Title = "Chapitre", Summary = "Résumé chapitre" });
        var lesson = new CourseLesson { Id = Guid.NewGuid(), CourseContentId = chapter.Id, Status = StudyStatus.Published, Order = 1, DurationMinutes = 15 };
        lesson.Translations.Add(new CourseLessonTranslation { Id = Guid.NewGuid(), CourseLessonId = lesson.Id, LanguageCode = "fr", PublicationStatus = StudyStatus.Published, Title = "Leçon", Slug = "lecon", ContentHtml = "<p>Contenu</p>", VideoUrl = "video" });
        var quiz = new CourseQuiz { Id = Guid.NewGuid(), CourseContentId = chapter.Id, Status = StudyStatus.Published, Order = 1 };
        quiz.Translations.Add(new CourseQuizTranslation { Id = Guid.NewGuid(), CourseQuizId = quiz.Id, LanguageCode = "fr", PublicationStatus = StudyStatus.Published, Title = "Quiz", Slug = "quiz" });
        var pathCategory = new LearningPathCategory { Id = Guid.NewGuid(), Status = StudyStatus.Published, Title = "Parcours" };
        pathCategory.Translations.Add(new LearningPathCategoryTranslation { Id = Guid.NewGuid(), LearningPathCategoryId = pathCategory.Id, LanguageCode = "fr", PublicationStatus = StudyStatus.Published, Title = "Parcours", Slug = "parcours", Summary = "Résumé parcours" });
        var path = new LearningPath { Id = Guid.NewGuid(), LearningPathCategoryId = pathCategory.Id, Status = StudyStatus.Published, Level = StudyLevel.Advanced, CreatedOnUtc = DateTime.UtcNow, Title = "Path" };
        path.Translations.Add(new LearningPathTranslation { Id = Guid.NewGuid(), LearningPathId = path.Id, LanguageCode = "fr", PublicationStatus = StudyStatus.Published, Title = "Parcours avancé", Slug = "parcours-avance", Summary = "Résumé" });
        path.LearningPathCourses.Add(new LearningPathCourse { Id = Guid.NewGuid(), LearningPathId = path.Id, CourseId = course.Id, Order = 1 });
        db.CourseCategories.Add(category);
        db.Courses.Add(course);
        db.CourseContents.Add(chapter);
        db.CourseLessons.Add(lesson);
        db.CourseQuizzes.Add(quiz);
        db.LearningPathCategories.Add(pathCategory);
        db.LearningPaths.Add(path);
        await db.SaveChangesAsync();

        var catalog = new EfCourseCatalogQueries(db);
        var home = await catalog.GetHomeAsync("fr");
        Assert.Single(home.LatestCourses);
        Assert.Single(home.TopCategories);
        Assert.Single(home.LatestLearningPaths);
        Assert.Equal("Cours avancé", (await catalog.GetCourseAsync("cours-avance", "fr"))!.Course.Title);
        Assert.Single((await catalog.GetCourseAsync("cours-avance", "fr"))!.Chapters);
        Assert.Equal("Leçon", (await catalog.GetLessonAsync("lecon", "fr"))!.Lesson.Title);
        Assert.Single((await catalog.GetCategoryAsync("categorie", "fr"))!.Courses);
        Assert.Single((await catalog.SearchCoursesAsync(new("avancé", "categorie", StudyLevel.Advanced, "az", 0, 100, "fr"))).Items);
        Assert.Single((await catalog.SearchCoursesAsync(new(null, null, StudyLevel.All, "level", 1, 6, "fr"))).Items);

        var paths = new EfLearningPathQueries(db);
        Assert.Single(await paths.GetPathsAsync("parcours", StudyLevel.Advanced, "fr"));
        var details = await paths.GetPathAsync("parcours-avance", "fr");
        Assert.NotNull(details);
        Assert.Single(details!.Courses);
    }

    [Fact]
    public async Task Core_commands_report_missing_parent_entities()
    {
        await using var db = CreateDb();
        var id = Guid.NewGuid();
        var sanitizer = new HtmlSanitizerService();
        var french = new AdminLessonTranslationDto("Lesson", "lesson", null, null, null, null, null, null, StudyStatus.Draft);
        var english = new AdminLessonTranslationDto("Lesson", "lesson-en", null, null, null, null, null, null, StudyStatus.Draft);

        Assert.False((await new EfAdminChapterPersistence(db, sanitizer).SaveAsync(new(null, id, 1, new("Chapter", null, StudyStatus.Draft), new("Chapter", null, StudyStatus.Draft)))).Success);
        Assert.False((await new EfAdminLessonCoreCommands(db, sanitizer).SaveAsync(new(null, id, id, 1, null, french, english))).Success);
        Assert.False((await new EfAdminQuizPersistence(db).SaveAsync(new(null, id, id, 1, new("Quiz", "quiz", null, StudyStatus.Draft), new("Quiz", "quiz-en", null, StudyStatus.Draft)))).Success);
        Assert.False((await new EfAdminLearningPathCoreCommands(db, sanitizer).SaveAsync(new(null, id, StudyLevel.Beginner, null, new("Path", "path", null, null, null, StudyStatus.Draft), new("Path", "path-en", null, null, null, StudyStatus.Draft)))).Success);
        await Assert.ThrowsAsync<InvalidOperationException>(() => new EfAdminQuestionCommands(db).SaveAsync(new(null, id, id, id, 1, null, StudyStatus.Draft, null, StudyStatus.Draft, [])));
    }

    private static ApplicationDbContext CreateDb() => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private sealed class MediaSpy : IMediaStorage
    {
        public List<string> Deleted { get; } = [];
        public bool Safe { get; set; } = true;
        public bool ThrowOnDelete { get; set; }
        public Task<string> SaveImageAsync(MediaUpload upload, string area, Guid entityId, CancellationToken cancellationToken = default) => Task.FromResult($"/uploads/{area}/{entityId:D}/image.png");
        public bool IsSafeImagePath(string? relativePath, string area, Guid entityId) => Safe;
        public void DeleteIfSafe(string? relativePath, string area, Guid entityId)
        {
            if (ThrowOnDelete) throw new ArgumentException("test");
            if (relativePath is not null) Deleted.Add(relativePath);
        }
    }
}
