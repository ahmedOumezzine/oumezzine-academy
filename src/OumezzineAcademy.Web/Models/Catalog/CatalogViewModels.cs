namespace OumezzineAcademy.Models.Catalog;

public record SeoViewModel(string Title, string Description, string? Canonical = null, string? Image = null, string Type = "website", string Robots = "index, follow");

public class HomeViewModel
{
    public IReadOnlyList<CourseCardViewModel> LatestCourses { get; set; } = [];
    public IReadOnlyList<CategoryCardViewModel> TopCategories { get; set; } = [];
    public IReadOnlyList<LearningPathCardViewModel> LatestLearningPaths { get; set; } = [];
    public int CourseCount { get; set; }
    public int CategoryCount { get; set; }
    public int LearningPathCount { get; set; }
    public int LessonCount { get; set; }
}

public class CourseSearchViewModel
{
    public string? Q { get; set; }
    public string? Category { get; set; }
    public StudyLevel? Level { get; set; }
    public string Sort { get; set; } = "recent";
    public string View { get; set; } = "grid";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 9;
    public int TotalItems { get; set; }
    public IReadOnlyList<CourseCardViewModel> Courses { get; set; } = [];
    public IReadOnlyList<CategoryCardViewModel> Categories { get; set; } = [];
    public int TotalPages => PageSize <= 0 ? 1 : (int)Math.Ceiling(TotalItems / (double)PageSize);
}

public class CourseCardViewModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Summary { get; set; }
    public string? Thumbnail { get; set; }
    public StudyLevel Level { get; set; }
    public string CategoryTitle { get; set; } = "";
    public string CategorySlug { get; set; } = "";
    public int LessonCount { get; set; }
    public int QuizCount { get; set; }
    public int DurationMinutes { get; set; }
    public DateTime CreatedOnUtc { get; set; }
}

public class CourseDetailViewModel : CourseCardViewModel
{
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? Overview { get; set; }
    public string? WhatYouLearn { get; set; }
    public string? Requirements { get; set; }
    public string? Audience { get; set; }
    public string? CategorySummary { get; set; }
    public DateTime? UpdatedOnUtc { get; set; }
    public IReadOnlyList<CourseContentViewModel> Contents { get; set; } = [];
    public IReadOnlyList<CourseCardViewModel> RelatedCourses { get; set; } = [];
}

public class CourseContentViewModel
{
    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Summary { get; set; }
    public int Order { get; set; }
    public IReadOnlyList<CourseLessonViewModel> Lessons { get; set; } = [];
    public IReadOnlyList<CourseQuizCardViewModel> Quizzes { get; set; } = [];
}

public class CourseLessonViewModel
{
    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Summary { get; set; }
    public string? Description { get; set; }
    public string? VideoUrl { get; set; }
    public int DurationMinutes { get; set; }
    public int Order { get; set; }
}

public class LessonDetailViewModel : CourseLessonViewModel
{
    public string? CourseThumbnail { get; set; }
    public string? DocumentUrl { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string CourseTitle { get; set; } = "";
    public string CourseSlug { get; set; } = "";
    public string CourseSummary { get; set; } = "";
    public string CategoryTitle { get; set; } = "";
    public string CategorySlug { get; set; } = "";
    public string ChapterTitle { get; set; } = "";
    public string? ChapterSummary { get; set; }
    public IReadOnlyList<CourseLessonViewModel> ChapterLessons { get; set; } = [];
    public IReadOnlyList<CourseQuizCardViewModel> ChapterQuizzes { get; set; } = [];
}

public class CourseQuizCardViewModel
{
    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Summary { get; set; }
    public int QuestionCount { get; set; }
    public int Order { get; set; }
}

public class CategoryCardViewModel
{
    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Summary { get; set; }
    public int CourseCount { get; set; }
}

public class CategoryDetailViewModel : CategoryCardViewModel
{
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public IReadOnlyList<CourseCardViewModel> Courses { get; set; } = [];
}

public class LearningPathCardViewModel
{
    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Summary { get; set; }
    public string? Thumbnail { get; set; }
    public StudyLevel Level { get; set; }
    public string CategoryTitle { get; set; } = "";
    public string CategorySlug { get; set; } = "";
    public string? CategorySummary { get; set; }
    public int CourseCount { get; set; }
}

public class LearningPathListViewModel
{
    public string? Category { get; set; }
    public StudyLevel? Level { get; set; }
    public IReadOnlyList<CategoryCardViewModel> Categories { get; set; } = [];
    public IReadOnlyList<LearningPathCardViewModel> Paths { get; set; } = [];
}

public class LearningPathDetailViewModel : LearningPathCardViewModel
{
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public IReadOnlyList<LearningPathStepViewModel> Courses { get; set; } = [];
}

public class LearningPathStepViewModel
{
    public Guid CourseId { get; set; }
    public string CourseTitle { get; set; } = "";
    public string CourseSlug { get; set; } = "";
    public string? Summary { get; set; }
    public string? ThumbnailUrl { get; set; }
    public int LessonCount { get; set; }
    public int QuizCount { get; set; }
    public int DurationMinutes { get; set; }
    public int Order { get; set; }
}

public class QuizAttemptViewModel
{
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public Guid QuizId { get; set; }
    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Summary { get; set; }
    public string CourseTitle { get; set; } = "";
    public string CourseSlug { get; set; } = "";
    public int QuestionCount { get; set; }
    public IReadOnlyList<QuizQuestionViewModel> Questions { get; set; } = [];
}

public class QuizQuestionViewModel
{
    public Guid QuestionId { get; set; }
    public string Text { get; set; } = "";
    public IReadOnlyList<QuizAnswerOptionViewModel> Answers { get; set; } = [];
}

public class QuizAnswerOptionViewModel
{
    public Guid AnswerId { get; set; }
    public string Text { get; set; } = "";
}

public class QuizResultViewModel
{
    public string QuizTitle { get; set; } = "";
    public string QuizSlug { get; set; } = "";
    public string CourseSlug { get; set; } = "";
    public int Score { get; set; }
    public int Total { get; set; }
    public IReadOnlyList<QuizQuestionResultViewModel> Questions { get; set; } = [];
}

public class QuizQuestionResultViewModel
{
    public string Text { get; set; } = "";
    public bool IsCorrect { get; set; }
    public string? SelectedAnswer { get; set; }
    public string CorrectAnswer { get; set; } = "";
}

