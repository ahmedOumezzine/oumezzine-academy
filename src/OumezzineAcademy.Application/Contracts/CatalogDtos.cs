using OumezzineAcademy.Domain.Catalog;

namespace OumezzineAcademy.Application.Abstractions;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalItems)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalItems / (double)PageSize);
}
public sealed record CourseSearchCriteria(string? Query, string? CategorySlug, StudyLevel? Level, string? Sort, int Page, int PageSize, string LanguageCode = "fr");
public sealed record HomeSummaryDto(int CourseCount, int CategoryCount, int LearningPathCount, int LessonCount, IReadOnlyList<CourseSummaryDto> LatestCourses, IReadOnlyList<CategorySummaryDto> TopCategories, IReadOnlyList<LearningPathSummaryDto> LatestLearningPaths);
public sealed record CourseSummaryDto(Guid Id, string Title, string Slug, string? Summary, string? Thumbnail, StudyLevel Level, string CategoryTitle, string CategorySlug, int LessonCount, int QuizCount, int DurationMinutes, DateTime CreatedOnUtc);
public sealed record CourseDetailsDto(CourseSummaryDto Course, string? Overview, string? WhatYouLearn, string? Requirements, string? Audience, string? MetaTitle, string? MetaDescription, DateTime? UpdatedOnUtc, IReadOnlyList<CourseChapterDto> Chapters, IReadOnlyList<CourseSummaryDto> RelatedCourses, string? CategorySummary = null);
public sealed record CourseChapterDto(string Title, string Slug, string? Summary, int Order, IReadOnlyList<LessonSummaryDto> Lessons, IReadOnlyList<QuizSummaryDto> Quizzes);
public sealed record LessonSummaryDto(string Title, string Slug, string? Summary, int DurationMinutes, int Order, string? Description = null, string? VideoUrl = null);
public sealed record LessonDetailsDto(LessonSummaryDto Lesson, string? Description, string? VideoUrl, string? DocumentUrl, string? MetaTitle, string? MetaDescription, string CourseTitle, string CourseSlug, string CourseSummary, string? CourseThumbnail, string CategoryTitle, string CategorySlug, string ChapterTitle, string? ChapterSummary, IReadOnlyList<LessonSummaryDto> ChapterLessons, IReadOnlyList<QuizSummaryDto> ChapterQuizzes);
public sealed record QuizSummaryDto(string Title, string Slug, string? Summary, int QuestionCount, int Order);
public sealed record CategorySummaryDto(string Title, string Slug, string? Summary, int CourseCount);
public sealed record CategoryDetailsDto(CategorySummaryDto Category, string? MetaTitle, string? MetaDescription, IReadOnlyList<CourseSummaryDto> Courses);
public sealed record LearningPathSummaryDto(string Title, string Slug, string? Summary, string? Thumbnail, StudyLevel Level, string CategoryTitle, string CategorySlug, string? CategorySummary, int CourseCount);
public sealed record LearningPathDetailsDto(LearningPathSummaryDto LearningPath, string? MetaTitle, string? MetaDescription, IReadOnlyList<LearningPathCourseDto> Courses);
public sealed record LearningPathCourseDto(Guid CourseId, string CourseTitle, string CourseSlug, string? Summary, string? Thumbnail, int LessonCount, int QuizCount, int DurationMinutes, int Order);

public sealed record QuizAttemptDto(Guid QuizId, string Title, string Slug, string? Summary, string CourseTitle, string CourseSlug, int QuestionCount, IReadOnlyList<QuizQuestionDto> Questions);
public sealed record QuizQuestionDto(Guid QuestionId, string Text, IReadOnlyList<QuizAnswerOptionDto> Answers);
public sealed record QuizAnswerOptionDto(Guid AnswerId, string Text);
public sealed record QuizSubmission(Guid QuizId, IReadOnlyDictionary<Guid, Guid> SelectedAnswers);
public sealed record QuizGradingData(string Title, string Slug, string CourseSlug, IReadOnlyList<QuizQuestionGradingData> Questions);
public sealed record QuizQuestionGradingData(Guid QuestionId, string Text, IReadOnlyList<QuizAnswerGradingData> Answers);
public sealed record QuizAnswerGradingData(Guid AnswerId, string Text, bool IsCorrect);
public sealed record QuizResultDto(string QuizTitle, string QuizSlug, string CourseSlug, int Score, int Total, IReadOnlyList<QuizQuestionResultDto> Questions);
public sealed record QuizQuestionResultDto(string Text, bool IsCorrect, string? SelectedAnswer, string CorrectAnswer);

public sealed record AdminCourseCommand(Guid? Id, string Title, string Slug, string? Summary, string? Overview, StudyLevel Level, StudyStatus Status, Guid CategoryId, IReadOnlyList<Guid> PrerequisiteCourseIds);
public sealed record AdminCategoryCommand(Guid? Id, string Title, string Slug, string? Summary, StudyStatus Status);
public sealed record AdminCategoryTranslationDto(string? Title, string? Slug, string? Summary, string? MetaTitle, string? MetaDescription, StudyStatus PublicationStatus);
public sealed record AdminCategoryListDto(Guid Id, string FrenchTitle, string EnglishTitle, int CourseCount, StudyStatus? FrenchStatus, StudyStatus? EnglishStatus);
public sealed record AdminCategoryEditDto(Guid Id, int CourseCount, DateTime CreatedOnUtc, DateTime? LastModifiedOnUtc, AdminCategoryTranslationDto French, AdminCategoryTranslationDto English);
public sealed record AdminCategorySaveCommand(Guid? Id, AdminCategoryTranslationDto French, AdminCategoryTranslationDto English);

public enum AdminCategoryDeleteStatus
{ Deleted, NotFound, InUse }

public interface IAdminCategoryQueries
{
    Task<IReadOnlyList<AdminCategoryListDto>> ListAsync(CancellationToken cancellationToken = default);

    Task<AdminCategoryEditDto?> GetForEditAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> SlugExistsAsync(string languageCode, string slug, Guid excludingId, CancellationToken cancellationToken = default);
}

public interface IAdminCategoryPersistence
{
    Task SaveAsync(AdminCategorySaveCommand command, CancellationToken cancellationToken = default);

    Task<AdminCategoryDeleteStatus> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public sealed record AdminCourseListDto(Guid Id, StudyStatus CourseStatus, string? Thumbnail, string Slug, string DisplayTitle, string FrenchTitle, string EnglishTitle, string CategoryTitle, StudyLevel Level, StudyStatus? FrenchStatus, StudyStatus? EnglishStatus, int LessonsCount, int QuizzesCount, DateTime DateUtc);
public sealed record AdminCourseTranslationDto(string? Title, string? Slug, string? Summary, string? Overview, string? WhatYouLearn, string? Requirements, string? Audience, string? MetaTitle, string? MetaDescription, StudyStatus PublicationStatus);
public sealed record AdminCourseEditDto(Guid Id, Guid CategoryId, StudyLevel Level, string? Thumbnail, DateTime CreatedOnUtc, DateTime? LastModifiedOnUtc, AdminCourseTranslationDto French, AdminCourseTranslationDto English, IReadOnlyList<Guid> PrerequisiteCourseIds);
public sealed record AdminCourseCategoryOptionDto(Guid Id, string DisplayName);
public sealed record AdminCoursePrerequisiteOptionDto(Guid Id, string Title, string Slug, StudyLevel Level, string CategoryName);

public interface IAdminCourseQueries
{
    Task<(IReadOnlyList<AdminCourseListDto> Items, int TotalCount)> ListAsync(string? search, Guid? category, StudyLevel? level, string? frStatus, string? enStatus, string? sort, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<AdminCourseEditDto?> GetForEditAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminCourseCategoryOptionDto>> GetCategoryOptionsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminCoursePrerequisiteOptionDto>> GetPrerequisiteOptionsAsync(Guid? excludingCourseId = null, CancellationToken cancellationToken = default);

    Task<string?> GetThumbnailAsync(Guid id, CancellationToken cancellationToken = default);
}

public sealed record AdminCourseCoreSaveCommand(Guid? Id, Guid CategoryId, StudyLevel Level, string? Thumbnail, AdminCourseTranslationDto French, AdminCourseTranslationDto English);

public interface IAdminCourseCoreCommands
{
    Task<(bool Success, Guid CourseId, string? ErrorKey, string? ErrorMessage)> SaveAsync(AdminCourseCoreSaveCommand command, CancellationToken cancellationToken = default);

    Task<bool> SlugExistsAsync(string languageCode, string slug, Guid excludingId, CancellationToken cancellationToken = default);
}

public sealed record AdminCoursePrerequisiteSyncResult(bool Success, IReadOnlyList<string> Errors);

public interface IAdminCoursePrerequisiteCommands
{
    Task<AdminCoursePrerequisiteSyncResult> SynchronizeAsync(Guid courseId, IReadOnlyList<Guid> prerequisiteCourseIds, CancellationToken cancellationToken = default);
}

public sealed record AdminChapterCommand(Guid? Id, Guid CourseId, string Title, string Slug, string? Summary, int Order, StudyStatus Status);
public sealed record AdminChapterTranslationDto(string? Title, string? Summary, StudyStatus PublicationStatus);
public sealed record AdminChapterListDto(Guid Id, int Order, string? FrenchTitle, string? EnglishTitle, StudyStatus? FrenchStatus, StudyStatus? EnglishStatus, int LessonsCount, int QuizzesCount);
public sealed record AdminChapterCourseDto(Guid Id, string DisplayTitle, string? Thumbnail, StudyLevel Level, string CategoryTitle, StudyStatus? FrenchStatus, StudyStatus? EnglishStatus);
public sealed record AdminChapterEditDto(Guid Id, Guid CourseId, int Order, string CourseFrenchTitle, string CourseEnglishTitle, AdminChapterTranslationDto French, AdminChapterTranslationDto English);

public interface IAdminChapterQueries
{
    Task<(AdminChapterCourseDto Course, IReadOnlyList<AdminChapterListDto> Chapters)?> ListAsync(Guid courseId, CancellationToken cancellationToken = default);

    Task<AdminChapterEditDto?> GetForEditAsync(Guid courseId, Guid? chapterId, CancellationToken cancellationToken = default);
}

public sealed record AdminChapterSaveCommand(Guid? Id, Guid CourseId, int Order, AdminChapterTranslationDto French, AdminChapterTranslationDto English);

public interface IAdminChapterMediaCommands
{
    Task<string?> UploadAsync(Guid courseId, Guid chapterId, MediaUpload upload, CancellationToken cancellationToken = default);
}

public sealed record AdminChapterDeleteResult(bool Success, bool NotFound, string Message);

public interface IAdminChapterPersistence
{
    Task<(bool Success, Guid ChapterId, string? Error)> SaveAsync(AdminChapterSaveCommand command, CancellationToken cancellationToken = default);

    Task MoveAsync(Guid courseId, Guid chapterId, int direction, CancellationToken cancellationToken = default);

    Task<AdminChapterDeleteResult> DeleteAsync(Guid courseId, Guid chapterId, CancellationToken cancellationToken = default);
}

public sealed record AdminLessonCommand(Guid? Id, Guid CourseId, Guid ChapterId, string Title, string Slug, string? Summary, string? Content, string? VideoUrl, string? DocumentUrl, int? DurationMinutes, int Order, StudyStatus Status);
public sealed record AdminLessonTranslationDto(string? Title, string? Slug, string? Summary, string? ContentHtml, string? VideoUrl, string? DocumentUrl, string? MetaTitle, string? MetaDescription, StudyStatus PublicationStatus);
public sealed record AdminLessonListDto(Guid Id, int Order, int? DurationMinutes, string? Slug, DateTime? LastModifiedOnUtc, string? FrenchTitle, string? EnglishTitle, StudyStatus? FrenchStatus, StudyStatus? EnglishStatus, bool HasText, bool HasVideo, bool HasDocument);
public sealed record AdminLessonContextDto(Guid CourseId, Guid ChapterId, int ChapterOrder, string CourseTitle, string ChapterTitle, StudyStatus? ChapterFrenchStatus, StudyStatus? ChapterEnglishStatus);
public sealed record AdminLessonEditDto(Guid Id, Guid CourseId, Guid ChapterId, int Order, int? DurationMinutes, string CourseFrenchTitle, string CourseEnglishTitle, string ChapterTitle, AdminLessonTranslationDto French, AdminLessonTranslationDto English);

public interface IAdminLessonQueries
{
    Task<(AdminLessonContextDto Context, IReadOnlyList<AdminLessonListDto> Lessons)?> ListAsync(Guid courseId, Guid chapterId, CancellationToken cancellationToken = default);

    Task<AdminLessonEditDto?> GetForEditAsync(Guid courseId, Guid chapterId, Guid? lessonId, CancellationToken cancellationToken = default);

    Task<bool> SlugExistsAsync(string language, string slug, Guid excludingLessonId, CancellationToken cancellationToken = default);
}

public sealed record AdminLessonSaveCommand(Guid? Id, Guid CourseId, Guid ChapterId, int Order, int? DurationMinutes, AdminLessonTranslationDto French, AdminLessonTranslationDto English);

public interface IAdminLessonCoreCommands
{
    Task<(bool Success, Guid LessonId, string? Error)> SaveAsync(AdminLessonSaveCommand command, CancellationToken cancellationToken = default);

    Task MoveAsync(Guid courseId, Guid chapterId, Guid lessonId, int direction, CancellationToken cancellationToken = default);
}

public sealed record AdminLessonMediaResult(bool Found, string? StoredPath);

public interface IAdminLessonMediaCommands
{
    Task<AdminLessonMediaResult> UploadAsync(Guid lessonId, MediaUpload upload, CancellationToken cancellationToken = default);

    Task RemoveAsync(Guid lessonId, string storedPath, CancellationToken cancellationToken = default);
}

public sealed record AdminLessonDeleteResult(bool Success, bool NotFound, string Message);

public interface IAdminLessonDeleteCommands
{
    Task<AdminLessonDeleteResult> DeleteAsync(Guid courseId, Guid chapterId, Guid lessonId, CancellationToken cancellationToken = default);
}

public sealed record AdminLearningPathCommand(Guid? Id, string Title, string Slug, string? Summary, StudyLevel Level, StudyStatus Status, Guid CategoryId, IReadOnlyList<Guid> CourseIds);
public sealed record AdminLearningPathTranslationDto(string? Title, string? Slug, string? Summary, string? MetaTitle, string? MetaDescription, StudyStatus PublicationStatus);
public sealed record AdminLearningPathListDto(Guid Id, string Title, string CategoryTitle, StudyLevel Level, StudyStatus? FrenchStatus, StudyStatus? EnglishStatus, int CourseCount);
public sealed record AdminLearningPathCategoryOptionDto(Guid Id, string Title);
public sealed record AdminLearningPathEditDto(Guid Id, Guid CategoryId, StudyLevel Level, string? Thumbnail, string SelectedCategoryTitle, AdminLearningPathTranslationDto French, AdminLearningPathTranslationDto English, IReadOnlyList<AdminLearningPathCategoryOptionDto> Categories);

public interface IAdminLearningPathQueries
{
    Task<IReadOnlyList<AdminLearningPathListDto>> ListAsync(CancellationToken cancellationToken = default);

    Task<AdminLearningPathEditDto?> GetForEditAsync(Guid? id, CancellationToken cancellationToken = default);

    Task<bool> SlugExistsAsync(string language, string slug, Guid excludingId, CancellationToken cancellationToken = default);

    Task<AdminLearningPathCompositionDto?> GetCompositionAsync(Guid learningPathId, CancellationToken cancellationToken = default);
}

public sealed record AdminLearningPathCourseOptionDto(Guid LinkId, Guid CourseId, int Order, string Title, string Slug, StudyLevel Level, string CategoryName, StudyStatus? FrenchStatus, StudyStatus? EnglishStatus, bool IsSelected);
public sealed record AdminLearningPathCompositionDto(Guid LearningPathId, string LearningPathTitle, IReadOnlyList<AdminLearningPathCourseOptionDto> SelectedCourses, IReadOnlyList<AdminLearningPathCourseOptionDto> AvailableCourses);
public sealed record AdminLearningPathSaveCommand(Guid? Id, Guid CategoryId, StudyLevel Level, string? Thumbnail, AdminLearningPathTranslationDto French, AdminLearningPathTranslationDto English);

public interface IAdminLearningPathCoreCommands
{
    Task<(bool Success, Guid LearningPathId, string? Error)> SaveAsync(AdminLearningPathSaveCommand command, CancellationToken cancellationToken = default);
}

public interface IAdminLearningPathMediaCommands
{
    Task<string?> UploadAsync(Guid learningPathId, MediaUpload upload, CancellationToken cancellationToken = default);
}

public sealed record AdminLearningPathCategoryCommand(Guid? Id, string Title, string Slug, string? Summary, StudyStatus Status);
public sealed record AdminQuizCommand(Guid? Id, Guid CourseId, Guid ChapterId, string Title, string Slug, string? Summary, int Order, StudyStatus Status);
public sealed record AdminQuizTranslationDto(string? Title, string? Slug, string? Summary, StudyStatus PublicationStatus);
public sealed record AdminQuizContextDto(Guid CourseId, Guid ChapterId, string CourseTitle, string ChapterTitle, int ChapterOrder, int LessonsCount, StudyStatus? FrenchStatus, StudyStatus? EnglishStatus);
public sealed record AdminQuizListDto(Guid Id, int Order, string Title, StudyStatus? FrenchStatus, StudyStatus? EnglishStatus, int QuestionsCount);
public sealed record AdminQuizEditDto(Guid Id, Guid CourseId, Guid ChapterId, int Order, string CourseTitle, string ChapterTitle, AdminQuizTranslationDto French, AdminQuizTranslationDto English);

public interface IAdminQuizQueries
{
    Task<(AdminQuizContextDto Context, IReadOnlyList<AdminQuizListDto> Quizzes)?> ListAsync(Guid courseId, Guid chapterId, CancellationToken cancellationToken = default);

    Task<AdminQuizEditDto?> GetForEditAsync(Guid courseId, Guid chapterId, Guid? quizId, CancellationToken cancellationToken = default);

    Task<bool> SlugExistsAsync(string language, string slug, Guid excludingQuizId, CancellationToken cancellationToken = default);
}

public sealed record AdminQuizSaveCommand(Guid? Id, Guid CourseId, Guid ChapterId, int Order, AdminQuizTranslationDto French, AdminQuizTranslationDto English);
public sealed record AdminQuizDeleteResult(bool Success, bool NotFound, string Message);

public interface IAdminQuizPersistence
{
    Task<(bool Success, Guid QuizId, string? Error)> SaveAsync(AdminQuizSaveCommand command, CancellationToken cancellationToken = default);

    Task MoveAsync(Guid courseId, Guid chapterId, Guid quizId, int direction, CancellationToken cancellationToken = default);

    Task<AdminQuizDeleteResult> DeleteAsync(Guid courseId, Guid chapterId, Guid quizId, CancellationToken cancellationToken = default);
}

public sealed record AdminQuestionCommand(Guid? Id, Guid QuizId, string Text, int Order, IReadOnlyList<AdminAnswerCommand> Answers);
public sealed record AdminAnswerCommand(Guid? Id, string Text, bool IsCorrect);

public sealed record DeleteCourseResult(bool Success, bool NotFound, string Message);

public interface IAdminCourseDeleteCommands
{
    Task<DeleteCourseResult> DeleteAsync(Guid courseId, CancellationToken cancellationToken = default);
}

public sealed record AdminCourseMediaResult(bool Found, string? StoredPath);

public interface IAdminCourseMediaCommands
{
    Task<AdminCourseMediaResult> UploadAsync(Guid courseId, MediaUpload upload, CancellationToken cancellationToken = default);

    Task<bool> RemoveAsync(Guid courseId, CancellationToken cancellationToken = default);
}

public sealed record AdminLearningPathCategoryListDto(Guid Id, string FrenchTitle, string EnglishTitle, int PathCount, StudyStatus? FrenchStatus, StudyStatus? EnglishStatus);
public sealed record AdminLearningPathCategoryTranslationDto(string LanguageCode, string? Title, string? Slug, string? Summary, string? MetaTitle, string? MetaDescription, StudyStatus PublicationStatus);
public sealed record AdminLearningPathCategoryEditDto(Guid Id, string Title, string Slug, StudyStatus Status, IReadOnlyList<AdminLearningPathCategoryTranslationDto> Translations, int PathCount);
public sealed record AdminLearningPathCategorySaveCommand(Guid? Id, string? FrenchTitle, string? FrenchSlug, string? FrenchSummary, string? FrenchMetaTitle, string? FrenchMetaDescription, StudyStatus FrenchStatus, string? EnglishTitle, string? EnglishSlug, string? EnglishSummary, string? EnglishMetaTitle, string? EnglishMetaDescription, StudyStatus EnglishStatus);

public enum AdminLearningPathCategoryDeleteStatus
{ Deleted, NotFound, InUse }

public sealed record AdminLearningPathCategorySaveResult(Guid Id, bool Created);
public sealed record AdminQuestionListItemDto(Guid Id, int Order, string Text, StudyStatus? FrenchStatus, StudyStatus? EnglishStatus, int AnswersCount, bool HasCorrectAnswer);
public sealed record AdminQuestionQuizContextDto(Guid CourseId, Guid ChapterId, Guid QuizId, string QuizTitle, string CourseTitle, string ChapterTitle, StudyStatus? FrenchStatus, StudyStatus? EnglishStatus, IReadOnlyList<AdminQuestionListItemDto> Questions);
public sealed record AdminAnswerDto(Guid Id, string? FrenchText, string? EnglishText, bool IsCorrect);
public sealed record AdminQuestionEditDto(Guid Id, Guid CourseId, Guid ChapterId, Guid QuizId, int Order, string CourseTitle, string ChapterTitle, string QuizTitle, string? FrenchText, StudyStatus FrenchStatus, string? EnglishText, StudyStatus EnglishStatus, IReadOnlyList<AdminAnswerDto> Answers);
public sealed record AdminQuestionSaveCommand(Guid? Id, Guid CourseId, Guid ChapterId, Guid QuizId, int Order, string? FrenchText, StudyStatus FrenchStatus, string? EnglishText, StudyStatus EnglishStatus, IReadOnlyList<AdminAnswerDto> Answers);

public enum AdminQuestionDeleteStatus
{ Deleted, NotFound }

public sealed record AdminQuestionMoveCommand(Guid CourseId, Guid ChapterId, Guid QuizId, Guid QuestionId, int Direction);