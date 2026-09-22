using OumezzineAcademy.Domain.Catalog;

namespace OumezzineAcademy.Application.Abstractions;

public interface ICurrentLanguage
{
    string LanguageCode { get; }
}

public sealed record LocalizedSlugSet(Guid EntityId, IReadOnlyDictionary<string, string> Slugs);
public interface ILocalizedSlugQueries
{
    Task<LocalizedSlugSet?> FindAsync(string entityType, string slug, string languageCode, CancellationToken cancellationToken = default);
}

public interface IStudyLmsCacheInvalidator
{
    Task InvalidateCatalogAsync(CancellationToken token = default);
    Task InvalidatePublicAsync(CancellationToken token = default);
}

public interface IStudyLmsSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}

public sealed class NullStudyLmsCacheInvalidator : IStudyLmsCacheInvalidator
{
    public Task InvalidateCatalogAsync(CancellationToken token = default) => Task.CompletedTask;
    public Task InvalidatePublicAsync(CancellationToken token = default) => Task.CompletedTask;
}

public interface IHtmlSanitizer
{
    string? Sanitize(string? html);
}

public sealed record MediaUpload(string FileName, string ContentType, long Length, Stream Content);

public interface IMediaStorage
{
    Task<string> SaveImageAsync(MediaUpload upload, string area, Guid entityId, CancellationToken cancellationToken = default);
    bool IsSafeImagePath(string? relativePath, string area, Guid entityId);
    void DeleteIfSafe(string? relativePath, string area, Guid entityId);
}

public sealed record CoursePrerequisiteEdge(Guid CourseId, Guid PrerequisiteCourseId);

public interface ICoursePrerequisiteEdges
{
    Task<IReadOnlyList<CoursePrerequisiteEdge>> GetAllAsync(CancellationToken cancellationToken = default);
}

public sealed record CourseVisibilityResult(bool IsVisible, IReadOnlyList<string> Reasons);

public interface ICourseCatalogQueries
{
    Task<HomeSummaryDto> GetHomeAsync(string languageCode, CancellationToken cancellationToken = default);
    Task<PagedResult<CourseSummaryDto>> SearchCoursesAsync(CourseSearchCriteria criteria, CancellationToken cancellationToken = default);
    Task<CourseDetailsDto?> GetCourseAsync(string slug, string languageCode, CancellationToken cancellationToken = default);
    Task<LessonDetailsDto?> GetLessonAsync(string slug, string languageCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CategorySummaryDto>> GetCategoriesAsync(string languageCode, CancellationToken cancellationToken = default);
    Task<CategoryDetailsDto?> GetCategoryAsync(string slug, string languageCode, CancellationToken cancellationToken = default);
}

public sealed record SitemapSlugs(IReadOnlyList<string> Courses, IReadOnlyList<string> Categories, IReadOnlyList<string> LearningPaths, IReadOnlyList<string> Lessons);
public interface ISitemapQueries
{
    Task<SitemapSlugs> GetSlugsAsync(string languageCode, CancellationToken cancellationToken = default);
}

public sealed record DashboardRecentItem(string Type, string Title, string Action, DateTime DateUtc, Guid? CourseId, Guid? ChapterId, string IconKey);
public sealed record DashboardSummary(int CoursesTotal, int CategoriesTotal, int FrenchPublishedCourses, int EnglishPublishedCourses, int EnglishDraftCourses, int EnglishMissingCourses, int LessonsTotal, int QuizzesTotal, int LearningPathsTotal, int CoursesWithoutThumbnail, int DraftLessons, int LessonsMissingEn, IReadOnlyList<DashboardRecentItem> RecentItems);
public interface IDashboardQueries
{
    Task<DashboardSummary> GetSummaryAsync(CancellationToken cancellationToken = default);
}

public interface ILearningPathQueries
{
    Task<IReadOnlyList<LearningPathSummaryDto>> GetPathsAsync(string? category, StudyLevel? level, string languageCode, CancellationToken cancellationToken = default);
    Task<LearningPathDetailsDto?> GetPathAsync(string slug, string languageCode, CancellationToken cancellationToken = default);
}

public interface IQuizQueries
{
    Task<QuizAttemptDto?> GetQuizAsync(string slug, string languageCode, CancellationToken cancellationToken = default);
    Task<QuizGradingData?> GetGradingDataAsync(Guid quizId, string languageCode, CancellationToken cancellationToken = default);
}

public interface IQuizSubmissionHandler
{
    Task<QuizResultDto?> SubmitAsync(QuizSubmission submission, string languageCode, CancellationToken cancellationToken = default);
}

public interface ICoursePrerequisiteValidator
{
    Task<bool> WouldCreateCycleAsync(Guid courseId, IEnumerable<Guid> prerequisites, CancellationToken cancellationToken = default);
}

public interface IAdminCourseCommands
{
    Task<Guid> SaveAsync(AdminCourseCommand command, CancellationToken cancellationToken = default);
    Task<DeleteCourseResult> DeleteAsync(Guid courseId, CancellationToken cancellationToken = default);
}

public interface IAdminCategoryCommands
{
    Task SaveAsync(AdminCategoryCommand command, CancellationToken cancellationToken = default);
    Task<AdminLearningPathCategoryDeleteStatus> DeleteAsync(Guid categoryId, CancellationToken cancellationToken = default);
}

public interface IAdminChapterCommands
{
    Task<Guid> SaveAsync(AdminChapterCommand command, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid courseId, Guid chapterId, CancellationToken cancellationToken = default);
}

public interface IAdminLessonCommands
{
    Task<Guid> SaveAsync(AdminLessonCommand command, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid courseId, Guid chapterId, Guid lessonId, CancellationToken cancellationToken = default);
}

public interface IAdminLearningPathCommands
{
    Task DeleteAsync(Guid learningPathId, CancellationToken cancellationToken = default);
    Task AddCourseAsync(Guid learningPathId, Guid courseId, CancellationToken cancellationToken = default);
    Task RemoveCourseAsync(Guid learningPathId, Guid learningPathCourseId, CancellationToken cancellationToken = default);
    Task MoveCourseAsync(Guid learningPathId, Guid learningPathCourseId, int direction, CancellationToken cancellationToken = default);
    Task SynchronizeCoursesAsync(Guid learningPathId, IReadOnlyCollection<Guid> selectedCourseIds, CancellationToken cancellationToken = default);
}

public interface IAdminLearningPathCategoryCommands
{
    Task<AdminLearningPathCategorySaveResult> SaveAsync(AdminLearningPathCategorySaveCommand command, CancellationToken cancellationToken = default);
    Task<AdminLearningPathCategoryDeleteStatus> DeleteAsync(Guid categoryId, CancellationToken cancellationToken = default);
}

public interface IAdminQuizCommands
{
    Task<Guid> SaveAsync(AdminQuizCommand command, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid chapterId, Guid quizId, CancellationToken cancellationToken = default);
}




public interface IAdminLearningPathCategoryQueries
{
    Task<IReadOnlyList<AdminLearningPathCategoryListDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<AdminLearningPathCategoryEditDto?> GetForEditAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> SlugExistsAsync(string languageCode, string slug, Guid excludingId, CancellationToken cancellationToken = default);
}



public interface IAdminQuestionQueries
{
    Task<AdminQuestionQuizContextDto?> ListAsync(Guid courseId, Guid chapterId, Guid quizId, CancellationToken cancellationToken = default);
    Task<AdminQuestionEditDto?> GetFormAsync(Guid courseId, Guid chapterId, Guid quizId, Guid? questionId, CancellationToken cancellationToken = default);
}
public interface IAdminQuestionCommands
{
    Task<Guid> SaveAsync(AdminQuestionSaveCommand command, CancellationToken cancellationToken = default);
    Task MoveAsync(AdminQuestionMoveCommand command, CancellationToken cancellationToken = default);
    Task<AdminQuestionDeleteStatus> DeleteAsync(Guid courseId, Guid chapterId, Guid quizId, Guid questionId, CancellationToken cancellationToken = default);
}

