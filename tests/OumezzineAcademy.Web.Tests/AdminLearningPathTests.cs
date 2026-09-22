using AhmedOumezzine.EFCore.Repository.Extensions;
using AhmedOumezzine.EFCore.Repository.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OumezzineAcademy.Application.Admin.Catalog;
using OumezzineAcademy.Application.UseCases;
using OumezzineAcademy.Areas.Admin.Models;
using OumezzineAcademy.Infrastructure.Data;
using OumezzineAcademy.Infrastructure.Media;
using OumezzineAcademy.Infrastructure.Persistence;
using OumezzineAcademy.Infrastructure.Sanitization;
using OumezzineAcademy.Web.Services;
using Xunit;

namespace OumezzineAcademy.Tests;

public sealed class AdminLearningPathTests
{
    [Fact]
    public async Task Learning_path_summaries_keep_allowed_formatting_and_remove_dangerous_markup()
    {
        await using var db = CreateDatabase();
        var category = new LearningPathCategory { Id = Guid.NewGuid(), Title = "Web", Slug = "web" };
        db.StudyLearningPathCategories.Add(category);
        db.Entry(category).Property<bool>("IsDeleted").CurrentValue = false;
        await db.SaveChangesAsync();
        var service = new AdminLearningPathService(new EfAdminLearningPathMediaCommands(db, new FileSystemMediaStorage(".")), new EfAdminLearningPathQueries(db), new EfAdminLearningPathCoreCommands(db, new HtmlSanitizerService()), new EfAdminLearningPathCommands(db, CreateRepository(db)));
        var model = new LearningPathEditViewModel
        {
            CategoryId = category.Id,
            French = new LearningPathTranslationInput { Title = "Parcours", Slug = "parcours", Summary = "<h2>Résumé</h2><p><strong>FR</strong><script>alert(1)</script></p>" },
            English = new LearningPathTranslationInput { Title = "Path", Slug = "path", Summary = "<h3>Summary</h3><p><em>EN</em><script>alert(1)</script></p>" }
        };

        var id = await service.SaveAsync(model, default);
        var translations = await db.StudyLearningPathTranslations.Where(x => x.LearningPathId == id).ToListAsync();

        Assert.Contains("<h2>Résumé</h2>", Assert.Single(translations, x => x.LanguageCode == "fr").Summary);
        Assert.Contains("<strong>FR</strong>", translations.Single(x => x.LanguageCode == "fr").Summary);
        Assert.DoesNotContain("<script", translations.Single(x => x.LanguageCode == "fr").Summary);
        Assert.Contains("<h3>Summary</h3>", translations.Single(x => x.LanguageCode == "en").Summary);
        Assert.Contains("<em>EN</em>", translations.Single(x => x.LanguageCode == "en").Summary);
        Assert.DoesNotContain("<script", translations.Single(x => x.LanguageCode == "en").Summary);
    }

    [Fact]
    public async Task Course_picker_excludes_existing_courses_and_returns_language_statuses()
    {
        await using var db = CreateDatabase();
        var courseCategory = new CourseCategory { Id = Guid.NewGuid(), Title = "Frontend", Slug = "frontend" };
        var pathCategory = new LearningPathCategory { Id = Guid.NewGuid(), Title = "Web", Slug = "web" };
        var path = new LearningPath { Id = Guid.NewGuid(), Title = "Path", Slug = "path", LearningPathCategoryId = pathCategory.Id, LearningPathCategory = pathCategory };
        var addedCourse = new Course { Id = Guid.NewGuid(), Title = "Already added", Slug = "added", CourseCategoryId = courseCategory.Id, CourseCategory = courseCategory };
        var availableCourse = new Course { Id = Guid.NewGuid(), Title = "Angular", Slug = "angular", CourseCategoryId = courseCategory.Id, CourseCategory = courseCategory, Level = StudyLevel.Intermediate };
        availableCourse.Translations.Add(new CourseTranslation { Id = Guid.NewGuid(), CourseId = availableCourse.Id, LanguageCode = "fr", PublicationStatus = StudyStatus.Published, Title = "Angular FR", Slug = "angular-fr" });
        availableCourse.Translations.Add(new CourseTranslation { Id = Guid.NewGuid(), CourseId = availableCourse.Id, LanguageCode = "en", PublicationStatus = StudyStatus.Draft, Title = "Angular EN", Slug = "angular-en" });
        path.LearningPathCourses.Add(new LearningPathCourse { Id = Guid.NewGuid(), LearningPathId = path.Id, LearningPath = path, CourseId = addedCourse.Id, Course = addedCourse, Order = 1 });
        db.StudyCourses.AddRange(addedCourse, availableCourse);
        db.StudyLearningPaths.Add(path);
        await db.SaveChangesAsync();

        var service = new AdminLearningPathService(new EfAdminLearningPathMediaCommands(db, new FileSystemMediaStorage(".")), new EfAdminLearningPathQueries(db), new EfAdminLearningPathCoreCommands(db, new HtmlSanitizerService()), new EfAdminLearningPathCommands(db, CreateRepository(db)));
        var viewModel = await service.CoursesAsync(path.Id, default);

        Assert.NotNull(viewModel);
        var available = Assert.Single(viewModel!.AvailableCourses);
        Assert.Equal(availableCourse.Id, available.Id);
        Assert.Equal(StudyStatus.Published, available.FrenchStatus);
        Assert.Equal(StudyStatus.Draft, available.EnglishStatus);

        await service.AddCourseAsync(path.Id, availableCourse.Id, default);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.AddCourseAsync(path.Id, availableCourse.Id, default));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.AddCourseAsync(path.Id, Guid.NewGuid(), default));
    }

    [Fact]
    public async Task Prerequisite_validation_rejects_self_reference()
    {
        await using var db = CreateDatabase();
        var a = await AddCourseAsync(db, "A");
        var service = new CoursePrerequisiteValidationService(new CoursePrerequisiteValidator(new EfCoursePrerequisiteEdges(db)));

        Assert.True(await service.WouldCreateCycleAsync(a, [a], CancellationToken.None));
    }

    [Fact]
    public async Task Prerequisite_validation_rejects_direct_and_long_cycles()
    {
        await using var db = CreateDatabase();
        var a = await AddCourseAsync(db, "A");
        var b = await AddCourseAsync(db, "B");
        var c = await AddCourseAsync(db, "C");
        var d = await AddCourseAsync(db, "D");
        db.StudyCoursePrerequisites.AddRange(new CoursePrerequisite { CourseId = b, PrerequisiteCourseId = a }, new CoursePrerequisite { CourseId = c, PrerequisiteCourseId = b }, new CoursePrerequisite { CourseId = d, PrerequisiteCourseId = c });
        await db.SaveChangesAsync();
        var service = new CoursePrerequisiteValidationService(new CoursePrerequisiteValidator(new EfCoursePrerequisiteEdges(db)));

        Assert.True(await service.WouldCreateCycleAsync(a, [b], CancellationToken.None));
        Assert.True(await service.WouldCreateCycleAsync(a, [d], CancellationToken.None));
    }

    [Fact]
    public async Task Prerequisite_validation_accepts_acyclic_graph()
    {
        await using var db = CreateDatabase();
        var a = await AddCourseAsync(db, "A");
        var b = await AddCourseAsync(db, "B");
        var c = await AddCourseAsync(db, "C");
        db.StudyCoursePrerequisites.Add(new CoursePrerequisite { CourseId = b, PrerequisiteCourseId = a });
        await db.SaveChangesAsync();

        Assert.False(await new CoursePrerequisiteValidationService(new CoursePrerequisiteValidator(new EfCoursePrerequisiteEdges(db))).WouldCreateCycleAsync(c, [b], CancellationToken.None));
    }

    private static ApplicationDbContext CreateDatabase() => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static async Task<Guid> AddCourseAsync(ApplicationDbContext db, string title)
    {
        var category = await db.StudyCourseCategories.FirstOrDefaultAsync() ?? new CourseCategory { Id = Guid.NewGuid(), Title = "Backend", Slug = "backend", CreatedOnUtc = DateTime.UtcNow };
        if (category.Id != Guid.Empty && db.Entry(category).State == EntityState.Detached) db.StudyCourseCategories.Add(category);
        var course = new Course { Id = Guid.NewGuid(), CourseCategoryId = category.Id, Title = title, Slug = title.ToLowerInvariant(), CreatedOnUtc = DateTime.UtcNow };
        db.StudyCourses.Add(course); await db.SaveChangesAsync(); return course.Id;
    }

    private static IRepository CreateRepository(ApplicationDbContext db)
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => db);
        services.AddGenericRepository<ApplicationDbContext>();
        return services.BuildServiceProvider().GetRequiredService<IRepository>();
    }
}