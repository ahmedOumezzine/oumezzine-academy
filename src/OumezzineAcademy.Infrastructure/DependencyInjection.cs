using AhmedOumezzine.EFCore.Repository.Extensions;
using Microsoft.Extensions.DependencyInjection;
using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Infrastructure.Data;
using OumezzineAcademy.Infrastructure.Media;
using OumezzineAcademy.Infrastructure.Persistence;

namespace OumezzineAcademy.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string webRootPath)
    {
        services.AddGenericRepository<ApplicationDbContext>();
        services.AddScoped<IMediaStorage>(_ => new FileSystemMediaStorage(webRootPath));
        services.AddScoped<ISitemapQueries, EfSitemapQueries>();
        services.AddScoped<IDashboardQueries, EfDashboardQueries>();
        services.AddScoped<IQuizQueries, EfQuizQueries>();
        services.AddScoped<ILocalizedSlugQueries, EfLocalizedSlugQueries>();
        services.AddScoped<ICourseCatalogQueries, EfCourseCatalogQueries>();
        services.AddScoped<ILearningPathQueries, EfLearningPathQueries>();
        services.AddScoped<ICoursePrerequisiteEdges, EfCoursePrerequisiteEdges>();
        services.AddScoped<IAdminLearningPathCategoryQueries, EfAdminLearningPathCategoryQueries>();
        services.AddScoped<IAdminLearningPathCategoryCommands, EfAdminLearningPathCategoryCommands>();
        services.AddScoped<IAdminCategoryQueries, EfAdminCategoryPersistence>();
        services.AddScoped<IAdminCategoryPersistence, EfAdminCategoryPersistence>();
        services.AddScoped<IAdminCourseQueries, EfAdminCourseQueries>();
        services.AddScoped<IAdminCourseCoreCommands, EfAdminCourseCoreCommands>();
        services.AddScoped<IAdminCoursePrerequisiteCommands, EfAdminCoursePrerequisiteCommands>();
        services.AddScoped<IAdminCourseDeleteCommands, EfAdminCourseDeleteCommands>();
        services.AddScoped<IAdminCourseMediaCommands, EfAdminCourseMediaCommands>();
        services.AddScoped<IAdminChapterQueries, EfAdminChapterQueries>();
        services.AddScoped<IAdminChapterPersistence, EfAdminChapterPersistence>();
        services.AddScoped<IAdminChapterMediaCommands, EfAdminChapterMediaCommands>();
        services.AddScoped<IAdminLessonQueries, EfAdminLessonQueries>();
        services.AddScoped<IAdminLessonCoreCommands, EfAdminLessonCoreCommands>();
        services.AddScoped<IAdminLessonMediaCommands, EfAdminLessonMediaCommands>();
        services.AddScoped<IAdminLessonDeleteCommands, EfAdminLessonDeleteCommands>();
        services.AddScoped<IAdminQuizQueries, EfAdminQuizQueries>();
        services.AddScoped<IAdminQuizPersistence, EfAdminQuizPersistence>();
        services.AddScoped<IAdminLearningPathQueries, EfAdminLearningPathQueries>();
        services.AddScoped<IAdminLearningPathCoreCommands, EfAdminLearningPathCoreCommands>();
        services.AddScoped<IAdminLearningPathCommands, EfAdminLearningPathCommands>();
        services.AddScoped<IAdminLearningPathMediaCommands, EfAdminLearningPathMediaCommands>();
        services.AddScoped<IStudyLmsSeeder, StudyLmsSeedService>();
        services.AddScoped<IAdminQuestionQueries, EfAdminQuestionQueries>();
        services.AddScoped<IAdminQuestionCommands, EfAdminQuestionCommands>();
        services.AddScoped<ICoursePrerequisiteValidator, OumezzineAcademy.Application.UseCases.CoursePrerequisiteValidator>();
        return services;
    }
}