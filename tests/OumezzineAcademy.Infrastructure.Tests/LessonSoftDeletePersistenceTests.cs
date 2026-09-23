using AhmedOumezzine.EFCore.Repository.Extensions;
using AhmedOumezzine.EFCore.Repository.Interface;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Areas.Admin.Models;
using OumezzineAcademy.Infrastructure.Data;
using OumezzineAcademy.Infrastructure.Media;
using OumezzineAcademy.Infrastructure.Persistence;
using OumezzineAcademy.Infrastructure.Sanitization;
using OumezzineAcademy.Web.Services;
using Xunit;

namespace OumezzineAcademy.Tests;

public sealed class LessonSoftDeletePersistenceTests
{
    private static IRepository CreateRepository(ApplicationDbContext db)
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => db);
        services.AddGenericRepository<ApplicationDbContext>();
        return services.BuildServiceProvider().GetRequiredService<IRepository>();
    }

    [Fact]
    public void SqlServer_sends_soft_delete_values_for_all_shared_catalog_tables()
    {
        using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=unused;Database=ModelOnly;Trusted_Connection=True;").Options);
        var properties = db.Model.GetEntityTypes().Select(x => x.FindProperty("IsDeleted"))
            .Where(x => x is not null).ToList();

        Assert.Equal(10, properties.Count);
        Assert.All(properties, property =>
        {
            Assert.False(property!.IsNullable);
            Assert.Equal(ValueGenerated.Never, property.ValueGenerated);
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Lesson_create_and_edit_work_with_or_without_database_default(bool hasDefault)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        if (!hasDefault)
        {
            // Reproduce the shared SQL Server schema: NOT NULL, with no default constraint.
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE CourseLessons DROP COLUMN IsDeleted");
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE CourseLessons ADD COLUMN IsDeleted INTEGER NOT NULL");
        }

        var category = new CourseCategory { Id = Guid.NewGuid(), Title = "Category" };
        var course = new Course { Id = Guid.NewGuid(), CourseCategory = category, Title = "Course" };
        var chapter = new CourseContent { Id = Guid.NewGuid(), Course = course, Title = "Chapter" };
        db.CourseContents.Add(chapter);
        await db.SaveChangesAsync();
        var service = new AdminLessonService(new EfAdminLessonQueries(CreateRepository(db)), new EfAdminLessonCoreCommands(db, new HtmlSanitizerService(), CreateRepository(db)), new EfAdminLessonMediaCommands(CreateRepository(db), new FileSystemMediaStorage(".")), new EfAdminLessonDeleteCommands(CreateRepository(db)), new NullStudyLmsCacheInvalidator());
        var input = new LessonEditViewModel
        {
            CourseId = course.Id,
            ChapterId = chapter.Id,
            Order = 1,
            French = new() { Title = "Leçon", Slug = "lecon", ContentHtml = "<p>Bonjour</p>" },
            English = new() { Title = "Lesson", Slug = "lesson", ContentHtml = "<p>Hello</p>" }
        };

        var id = await service.SaveAsync(input, CancellationToken.None);
        db.ChangeTracker.Clear();
        Assert.False(await db.CourseLessons.Where(x => x.Id == id)
            .Select(x => EF.Property<bool>(x, "IsDeleted")).SingleAsync());

        input.Id = id;
        input.French.Title = "Leçon modifiée";
        await service.SaveAsync(input, CancellationToken.None);
        db.ChangeTracker.Clear();
        var saved = await db.CourseLessons.Include(x => x.Translations).SingleAsync();
        Assert.Equal(id, saved.Id);
        Assert.Equal(chapter.Id, saved.CourseContentId);
        Assert.Equal("Leçon modifiée", saved.Translations.Single(x => x.LanguageCode == "fr").Title);
        Assert.Equal("Lesson", saved.Translations.Single(x => x.LanguageCode == "en").Title);
        Assert.False(db.Entry(saved).Property<bool>("IsDeleted").CurrentValue);
    }
}
