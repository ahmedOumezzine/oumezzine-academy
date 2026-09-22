using AhmedOumezzine.EFCore.Repository.Entities;

namespace OumezzineAcademy.Domain.Catalog;

public class CourseCategory : BaseEntity
{
    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Summary { get; set; }
    public StudyStatus Status { get; set; } = StudyStatus.Draft;
    public List<Course> Courses { get; set; } = new();
    public List<CourseCategoryTranslation> Translations { get; set; } = new();
}

public class Course : BaseEntity
{
    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Summary { get; set; }
    public string? Overview { get; set; }
    public string? Thumbnail { get; set; }
    public string? WhatYouLearn { get; set; }
    public string? Requirements { get; set; }
    public string? Audience { get; set; }
    public StudyLevel Level { get; set; } = StudyLevel.All;
    public StudyStatus Status { get; set; } = StudyStatus.Draft;
    public Guid CourseCategoryId { get; set; }
    public CourseCategory CourseCategory { get; set; } = null!;
    public List<CourseContent> CourseContents { get; set; } = new();
    public List<LearningPathCourse> LearningPathCourses { get; set; } = new();
    public List<CourseTranslation> Translations { get; set; } = new();
    public List<CoursePrerequisite> Prerequisites { get; set; } = new();
    public List<CoursePrerequisite> RequiredByCourses { get; set; } = new();
}

public class CourseContent : BaseEntity
{
    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Summary { get; set; }
    public int Order { get; set; }
    public StudyStatus Status { get; set; } = StudyStatus.Draft;
    public Guid CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public List<CourseLesson> CourseLessons { get; set; } = new();
    public List<CourseQuiz> CourseQuizzes { get; set; } = new();
    public List<CourseContentTranslation> Translations { get; set; } = new();
}

public class CourseLesson : BaseEntity
{
    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Summary { get; set; }
    public string? Description { get; set; }
    public string? VideoUrl { get; set; }
    public string? DocumentUrl { get; set; }
    public int? DurationMinutes { get; set; }
    public int Order { get; set; }
    public StudyStatus Status { get; set; } = StudyStatus.Draft;
    public Guid CourseContentId { get; set; }
    public CourseContent CourseContent { get; set; } = null!;
    public List<CourseLessonTranslation> Translations { get; set; } = new();
}

public class CourseQuiz : BaseEntity
{
    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Summary { get; set; }
    public int Order { get; set; }
    public StudyStatus Status { get; set; } = StudyStatus.Draft;
    public Guid CourseContentId { get; set; }
    public CourseContent CourseContent { get; set; } = null!;
    public List<QuizQuestion> QuizQuestions { get; set; } = new();
    public List<CourseQuizTranslation> Translations { get; set; } = new();
}

public class QuizQuestion : BaseEntity
{
    public string Text { get; set; } = "";
    public int Order { get; set; }
    public Guid CourseQuizId { get; set; }
    public CourseQuiz CourseQuiz { get; set; } = null!;
    public List<QuizAnswer> Answers { get; set; } = new();
    public List<QuizQuestionTranslation> Translations { get; set; } = new();
}

public class QuizAnswer : BaseEntity
{
    public string Text { get; set; } = "";
    public bool IsCorrect { get; set; }
    public Guid QuizQuestionId { get; set; }
    public QuizQuestion QuizQuestion { get; set; } = null!;
    public List<QuizAnswerTranslation> Translations { get; set; } = new();
}

public class LearningPathCategory : BaseEntity
{
    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Summary { get; set; }
    public StudyStatus Status { get; set; } = StudyStatus.Draft;
    public List<LearningPath> LearningPaths { get; set; } = new();
    public List<LearningPathCategoryTranslation> Translations { get; set; } = new();
}

public class LearningPath : BaseEntity
{
    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Summary { get; set; }
    public string? Thumbnail { get; set; }
    public StudyLevel Level { get; set; } = StudyLevel.Beginner;
    public StudyStatus Status { get; set; } = StudyStatus.Draft;
    public Guid LearningPathCategoryId { get; set; }
    public LearningPathCategory LearningPathCategory { get; set; } = null!;
    public List<LearningPathCourse> LearningPathCourses { get; set; } = new();
    public List<LearningPathTranslation> Translations { get; set; } = new();
}

public class LearningPathCourse : BaseEntity
{
    public Guid LearningPathId { get; set; }
    public LearningPath LearningPath { get; set; } = null!;
    public Guid CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public int Order { get; set; }
}