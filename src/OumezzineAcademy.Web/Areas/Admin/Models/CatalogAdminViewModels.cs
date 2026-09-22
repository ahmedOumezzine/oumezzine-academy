using OumezzineAcademy.Models.Catalog;
using System.ComponentModel.DataAnnotations;

namespace OumezzineAcademy.Areas.Admin.Models;

public sealed class AdminDashboardViewModel
{
    public int CoursesTotal { get; init; }
    public int CategoriesTotal { get; init; }
    public int FrenchPublishedCourses { get; init; }
    public int EnglishPublishedCourses { get; init; }
    public int EnglishDraftCourses { get; init; }
    public int EnglishMissingCourses { get; init; }
    public int LessonsTotal { get; init; }
    public int QuizzesTotal { get; init; }
    public int LearningPathsTotal { get; init; }
    public int CoursesWithoutThumbnail { get; init; }
    public int DraftLessons { get; init; }
    public int LessonsMissingEn { get; init; }
    public IReadOnlyList<AdminDashboardRecentItemViewModel> RecentItems { get; init; } = [];
}

public sealed class AdminDashboardRecentItemViewModel
{
    public string Type { get; init; } = "";
    public string Title { get; init; } = "";
    public string Action { get; init; } = "";
    public DateTime DateUtc { get; init; }
    public string Url { get; init; } = "#";
    public string IconKey { get; init; } = "file-earmark";
}

public sealed class AdminCategoryListItem
{
    public Guid Id { get; init; }
    public string FrenchTitle { get; init; } = "";
    public string EnglishTitle { get; init; } = "";
    public int CourseCount { get; init; }
    public StudyStatus? FrenchStatus { get; init; }
    public StudyStatus? EnglishStatus { get; init; }
}

public sealed class CategoryEditViewModel
{
    public Guid Id { get; set; }
    public int CourseCount { get; set; }
    public DateTime? CreatedOnUtc { get; set; }
    public DateTime? LastModifiedOnUtc { get; set; }
    public CategoryTranslationInput French { get; set; } = new();
    public CategoryTranslationInput English { get; set; } = new();
}

public sealed class CategoryTranslationInput
{
    [Display(Name = "Titre")]
    public string? Title { get; set; }

    [Display(Name = "Slug")]
    public string? Slug { get; set; }

    public string? Summary { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public StudyStatus PublicationStatus { get; set; } = StudyStatus.Draft;
}

public sealed class AdminCourseListItem
{
    public StudyStatus CourseStatus { get; init; }
    public OumezzineAcademy.Application.Abstractions.CourseVisibilityResult FrenchVisibility { get; set; } = new(false, []);
    public OumezzineAcademy.Application.Abstractions.CourseVisibilityResult EnglishVisibility { get; set; } = new(false, []);
    public Guid Id { get; init; }
    public string? Thumbnail { get; init; }
    public string Slug { get; init; } = "";
    public string DisplayTitle { get; init; } = "Cours sans traduction";
    public string FrenchTitle { get; init; } = "";
    public string EnglishTitle { get; init; } = "";
    public string CategoryTitle { get; init; } = "";
    public StudyLevel Level { get; init; }
    public StudyStatus? FrenchStatus { get; init; }
    public StudyStatus? EnglishStatus { get; init; }
    public int LessonsCount { get; init; }
    public int QuizzesCount { get; init; }
    public DateTime DateUtc { get; init; }
}

public sealed class AdminCoursesListViewModel
{
    public IReadOnlyList<AdminCourseListItem> Items { get; init; } = [];
    public IReadOnlyList<AdminCategoryOption> Categories { get; init; } = [];
    public string? Search { get; init; }
    public Guid? Category { get; init; }
    public StudyLevel? Level { get; init; }
    public string? FrenchStatus { get; init; }
    public string? EnglishStatus { get; init; }
    public string Sort { get; init; } = "modified";
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public int TotalCount { get; init; }
    public int PageCount => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
}

public sealed class CourseEditViewModel
{
    public Guid Id { get; set; }
    [Required] public Guid CategoryId { get; set; }
    public StudyLevel Level { get; set; } = StudyLevel.Beginner;
    public string? ThumbnailUrl { get; set; }
    public IFormFile? Thumbnail { get; set; }
    public List<Guid> PrerequisiteCourseIds { get; set; } = [];
    public IReadOnlyList<AdminCategoryOption> Categories { get; set; } = [];
    public IReadOnlyList<AdminCourseOption> AvailablePrerequisites { get; set; } = [];
    public CourseTranslationInput French { get; set; } = new();
    public CourseTranslationInput English { get; set; } = new();
}

public sealed class CourseTranslationInput
{
    public string? Title { get; set; }
    public string? Slug { get; set; }
    public string? Summary { get; set; }
    public string? Overview { get; set; }
    public string? WhatYouLearn { get; set; }
    public string? Requirements { get; set; }
    public string? Audience { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public StudyStatus PublicationStatus { get; set; } = StudyStatus.Draft;
}

public sealed class AdminChapterListItem
{
    public Guid Id { get; init; }
    public int Order { get; init; }
    public string FrenchTitle { get; init; } = "";
    public string EnglishTitle { get; init; } = "Missing";
    public StudyStatus? FrenchStatus { get; init; }
    public StudyStatus? EnglishStatus { get; init; }
    public int LessonsCount { get; init; }
    public int QuizzesCount { get; init; }
    public bool IsFirst { get; set; }
    public bool IsLast { get; set; }
}

public sealed class AdminCategoryOption
{
    public Guid Id { get; init; }
    public string DisplayName { get; init; } = "";
}

public sealed class AdminCourseOption
{
    public Guid Id { get; init; }
    public string Title { get; init; } = "";
    public string? CategoryName { get; init; }
    public StudyLevel Level { get; init; }
    public string? Slug { get; init; }
    public StudyStatus? FrenchStatus { get; init; }
    public StudyStatus? EnglishStatus { get; init; }
}

public sealed class AdminLearningPathCategoryOption
{
    public Guid Id { get; init; }
    public string Title { get; init; } = "";
}

public sealed class AdminCourseContentViewModel
{
    public Guid CourseId { get; init; }
    public string DisplayTitle { get; init; } = "Cours sans traduction";
    public string? ThumbnailUrl { get; init; }
    public string CategoryTitle { get; init; } = "Sans catégorie";
    public StudyLevel Level { get; init; }
    public StudyStatus? FrenchStatus { get; init; }
    public StudyStatus? EnglishStatus { get; init; }
    public int ChapterCount { get; init; }
    public int LessonCount { get; init; }
    public int QuizCount { get; init; }
    public IReadOnlyList<AdminChapterListItem> Chapters { get; init; } = [];
}

public sealed class ChapterEditViewModel
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    [Range(1, int.MaxValue)] public int Order { get; set; } = 1;
    public string CourseFrenchTitle { get; set; } = "";
    public string CourseEnglishTitle { get; set; } = "Missing";
    public ChapterTranslationInput French { get; set; } = new();
    public ChapterTranslationInput English { get; set; } = new();
}

public sealed class ChapterTranslationInput
{
    public string? Title { get; set; }
    public string? Summary { get; set; }
    public StudyStatus PublicationStatus { get; set; } = StudyStatus.Draft;
}

public sealed class AdminLessonListItem
{
    public Guid Id { get; init; }
    public int Order { get; init; }
    public string FrenchTitle { get; init; } = "";
    public string EnglishTitle { get; init; } = "Missing";
    public int? DurationMinutes { get; init; }
    public StudyStatus? FrenchStatus { get; init; }
    public StudyStatus? EnglishStatus { get; init; }
    public bool HasText { get; init; }
    public bool HasVideo { get; init; }
    public bool HasDocument { get; init; }
    public string? Slug { get; init; }
    public DateTime? LastModifiedOnUtc { get; init; }
    public bool IsFirst { get; set; }
    public bool IsLast { get; set; }
}

public sealed class AdminLessonListViewModel
{
    public Guid CourseId { get; init; }
    public Guid ChapterId { get; init; }
    public string CourseTitle { get; init; } = "Cours sans traduction";
    public string ChapterTitle { get; init; } = "Chapitre sans traduction";
    public int ChapterOrder { get; init; }
    public StudyStatus? ChapterFrenchStatus { get; init; }
    public StudyStatus? ChapterEnglishStatus { get; init; }
    public IReadOnlyList<AdminLessonListItem> Lessons { get; init; } = [];
}

public sealed class LessonEditViewModel
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public Guid ChapterId { get; set; }
    public IFormFile? LessonImage { get; set; }
    [Range(1, int.MaxValue)] public int Order { get; set; } = 1;
    [Range(0, 100000)] public int? DurationMinutes { get; set; }
    public string CourseFrenchTitle { get; set; } = "";
    public string CourseEnglishTitle { get; set; } = "Missing";
    public string ChapterTitle { get; set; } = "";
    public LessonTranslationInput French { get; set; } = new();
    public LessonTranslationInput English { get; set; } = new();
}

public sealed class LessonTranslationInput
{
    public string? Title { get; set; }
    public string? Slug { get; set; }
    public string? Summary { get; set; }
    public string? ContentHtml { get; set; }
    public string? VideoUrl { get; set; }
    public string? DocumentUrl { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public StudyStatus PublicationStatus { get; set; } = StudyStatus.Draft;
}

public sealed class AdminQuizListViewModel
{
    public Guid CourseId { get; init; }
    public Guid ChapterId { get; init; }
    public string CourseTitle { get; init; } = "Cours sans traduction";
    public string ChapterTitle { get; init; } = "Chapitre sans traduction";
    public int ChapterOrder { get; init; }
    public int LessonsCount { get; init; }
    public StudyStatus? FrenchStatus { get; init; }
    public StudyStatus? EnglishStatus { get; init; }
    public IReadOnlyList<AdminQuizListItem> Quizzes { get; init; } = [];
}

public sealed class AdminQuizListItem
{
    public Guid Id { get; init; }
    public int Order { get; init; }
    public string Title { get; init; } = "Quiz sans traduction";
    public StudyStatus? FrenchStatus { get; init; }
    public StudyStatus? EnglishStatus { get; init; }
    public int QuestionsCount { get; init; }
    public bool IsFirst { get; set; }
    public bool IsLast { get; set; }
}

public sealed class AdminQuizFormViewModel
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public Guid ChapterId { get; set; }
    [Range(1, int.MaxValue)] public int Order { get; set; } = 1;
    public string CourseTitle { get; set; } = "Cours sans traduction";
    public string ChapterTitle { get; set; } = "Chapitre sans traduction";
    public QuizTranslationInput French { get; set; } = new();
    public QuizTranslationInput English { get; set; } = new();
}

public sealed class QuizTranslationInput
{
    public string? Title { get; set; }
    public string? Slug { get; set; }
    public string? Summary { get; set; }
    public StudyStatus PublicationStatus { get; set; } = StudyStatus.Draft;
}

public sealed class AdminQuestionListViewModel
{
    public Guid CourseId { get; init; }
    public Guid ChapterId { get; init; }
    public Guid QuizId { get; init; }
    public string CourseTitle { get; init; } = "Cours sans traduction";
    public string ChapterTitle { get; init; } = "Chapitre sans traduction";
    public string QuizTitle { get; init; } = "Quiz sans traduction";
    public StudyStatus? FrenchStatus { get; init; }
    public StudyStatus? EnglishStatus { get; init; }
    public IReadOnlyList<AdminQuestionListItem> Questions { get; init; } = [];
}

public sealed class AdminQuestionListItem
{
    public Guid Id { get; init; }
    public int Order { get; init; }
    public string Text { get; init; } = "Question sans traduction";
    public StudyStatus? FrenchStatus { get; init; }
    public StudyStatus? EnglishStatus { get; init; }
    public int AnswersCount { get; init; }
    public bool HasCorrectAnswer { get; init; }
    public bool IsFirst { get; set; }
    public bool IsLast { get; set; }
}

public sealed class AdminQuestionFormViewModel
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public Guid ChapterId { get; set; }
    public Guid QuizId { get; set; }
    [Range(1, int.MaxValue)] public int Order { get; set; } = 1;
    public string CourseTitle { get; set; } = "Cours sans traduction";
    public string ChapterTitle { get; set; } = "Chapitre sans traduction";
    public string QuizTitle { get; set; } = "Quiz sans traduction";
    public int CorrectAnswerIndex { get; set; } = -1;
    public QuestionTranslationInput French { get; set; } = new();
    public QuestionTranslationInput English { get; set; } = new();
    public List<AdminAnswerInput> Answers { get; set; } = [];
}

public sealed class QuestionTranslationInput
{
    public string? Text { get; set; }
    public StudyStatus PublicationStatus { get; set; } = StudyStatus.Draft;
}

public sealed class AdminLearningPathCategoryListItem
{
    public Guid Id { get; init; }
    public string FrenchTitle { get; init; } = "";
    public string EnglishTitle { get; init; } = "Missing";
    public int PathCount { get; init; }
    public StudyStatus? FrenchStatus { get; init; }
    public StudyStatus? EnglishStatus { get; init; }
}

public sealed class LearningPathCategoryEditViewModel
{
    public Guid Id { get; set; }
    public LearningPathCategoryTranslationInput French { get; set; } = new();
    public LearningPathCategoryTranslationInput English { get; set; } = new();
}

public sealed class LearningPathCategoryTranslationInput
{
    public string? Title { get; set; }
    public string? Slug { get; set; }
    public string? Summary { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public StudyStatus PublicationStatus { get; set; } = StudyStatus.Draft;
}

public sealed class AdminLearningPathListItem
{
    public Guid Id { get; init; }
    public string Title { get; init; } = "Parcours sans traduction";
    public string CategoryTitle { get; init; } = "Sans catégorie";
    public StudyLevel Level { get; init; }
    public StudyStatus? FrenchStatus { get; init; }
    public StudyStatus? EnglishStatus { get; init; }
    public int CourseCount { get; init; }
}

public sealed class AdminLearningPathsListViewModel
{
    public IReadOnlyList<AdminLearningPathListItem> Items { get; init; } = [];
}

public sealed class LearningPathEditViewModel
{
    public List<Guid> SelectedCourseIds { get; set; } = [];
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public StudyLevel Level { get; set; } = StudyLevel.Beginner;
    public IFormFile? Thumbnail { get; set; }
    public string? ThumbnailUrl { get; set; }
    public IReadOnlyList<AdminLearningPathCategoryOption> Categories { get; set; } = [];
    public string SelectedCategoryTitle { get; set; } = "Catégorie";
    public LearningPathTranslationInput French { get; set; } = new();
    public LearningPathTranslationInput English { get; set; } = new();
}

public sealed class LearningPathTranslationInput
{
    public string? Title { get; set; }
    public string? Slug { get; set; }
    public string? Summary { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public StudyStatus PublicationStatus { get; set; } = StudyStatus.Draft;
}

public sealed class AdminLearningPathCourseItem
{
    public Guid Id { get; init; }
    public Guid CourseId { get; init; }
    public int Order { get; init; }
    public string Title { get; init; } = "Cours sans traduction";
    public string CategoryTitle { get; init; } = "Sans catégorie";
    public StudyLevel Level { get; init; }
    public StudyStatus? FrenchStatus { get; init; }
    public StudyStatus? EnglishStatus { get; init; }
    public bool IsFirst { get; set; }
    public bool IsLast { get; set; }
}

public sealed class AdminLearningPathCoursesViewModel
{
    public Guid LearningPathId { get; init; }
    public string LearningPathTitle { get; init; } = "Parcours sans traduction";
    public IReadOnlyList<AdminLearningPathCourseItem> SelectedCourses { get; init; } = [];
    public IReadOnlyList<AdminCourseOption> AvailableCourses { get; init; } = [];
}

public sealed class AdminAnswerInput
{
    public Guid Id { get; set; }
    public string? FrenchText { get; set; }
    public string? EnglishText { get; set; }
    public bool IsCorrect { get; set; }
}

