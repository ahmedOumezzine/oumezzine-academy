namespace OumezzineAcademy.Domain.Catalog;

public abstract class StudyTranslationBase
{
    public Guid Id { get; set; }
    public string LanguageCode { get; set; } = "fr";
    public StudyStatus PublicationStatus { get; set; } = StudyStatus.Draft;
}

public class CourseTranslation : StudyTranslationBase
{
    public Guid CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Summary { get; set; }
    public string? Overview { get; set; }
    public string? WhatYouLearn { get; set; }
    public string? Requirements { get; set; }
    public string? Audience { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
}

public class CourseCategoryTranslation : StudyTranslationBase
{
    public Guid CourseCategoryId { get; set; }
    public CourseCategory CourseCategory { get; set; } = null!;
    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Summary { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
}

public class CourseContentTranslation : StudyTranslationBase
{
    public Guid CourseContentId { get; set; }
    public CourseContent CourseContent { get; set; } = null!;
    public string Title { get; set; } = "";
    public string? Summary { get; set; }
}

public class CourseLessonTranslation : StudyTranslationBase
{
    public Guid CourseLessonId { get; set; }
    public CourseLesson CourseLesson { get; set; } = null!;
    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Summary { get; set; }
    public string? ContentHtml { get; set; }
    public string? VideoUrl { get; set; }
    public string? DocumentUrl { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
}

public class CourseQuizTranslation : StudyTranslationBase
{
    public Guid CourseQuizId { get; set; }
    public CourseQuiz CourseQuiz { get; set; } = null!;
    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Summary { get; set; }
}

public class QuizQuestionTranslation : StudyTranslationBase
{
    public Guid QuizQuestionId { get; set; }
    public QuizQuestion QuizQuestion { get; set; } = null!;
    public string Text { get; set; } = "";
}

public class QuizAnswerTranslation : StudyTranslationBase
{
    public Guid QuizAnswerId { get; set; }
    public QuizAnswer QuizAnswer { get; set; } = null!;
    public string Text { get; set; } = "";
}

public class LearningPathTranslation : StudyTranslationBase
{
    public Guid LearningPathId { get; set; }
    public LearningPath LearningPath { get; set; } = null!;
    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Summary { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
}

public class LearningPathCategoryTranslation : StudyTranslationBase
{
    public Guid LearningPathCategoryId { get; set; }
    public LearningPathCategory LearningPathCategory { get; set; } = null!;
    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Summary { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
}

public class CoursePrerequisite
{
    public Guid CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public Guid PrerequisiteCourseId { get; set; }
    public Course PrerequisiteCourse { get; set; } = null!;
}

