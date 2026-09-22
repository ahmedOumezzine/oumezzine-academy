using System.Reflection;
using Xunit;

namespace OumezzineAcademy.Tests;

public sealed class CatalogEntityTests
{
    public static IEnumerable<object[]> CatalogEntities() =>
    [
        [typeof(CourseCategory)], [typeof(Course)], [typeof(CourseContent)],
        [typeof(CourseLesson)], [typeof(CourseQuiz)], [typeof(QuizQuestion)],
        [typeof(QuizAnswer)], [typeof(LearningPathCategory)], [typeof(LearningPath)],
        [typeof(LearningPathCourse)], [typeof(CourseTranslation)],
        [typeof(CourseCategoryTranslation)], [typeof(CourseContentTranslation)],
        [typeof(CourseLessonTranslation)], [typeof(CourseQuizTranslation)],
        [typeof(QuizQuestionTranslation)], [typeof(QuizAnswerTranslation)],
        [typeof(LearningPathTranslation)], [typeof(LearningPathCategoryTranslation)],
        [typeof(CoursePrerequisite)]
    ];

    [Theory]
    [MemberData(nameof(CatalogEntities))]
    public void Entity_can_be_created_and_all_public_properties_can_round_trip(Type entityType)
    {
        var entity = Activator.CreateInstance(entityType)!;

        foreach (var property in entityType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!property.CanRead || !property.CanWrite) continue;

            var value = ValueFor(property.PropertyType);
            property.SetValue(entity, value);
            Assert.Equal(value, property.GetValue(entity));
        }
    }

    [Fact]
    public void CourseCategory_has_draft_status_and_initialized_collections()
    {
        var category = new CourseCategory();

        Assert.Equal(StudyStatus.Draft, category.Status);
        Assert.NotNull(category.Courses);
        Assert.NotNull(category.Translations);
        Assert.Empty(category.Courses);
        Assert.Empty(category.Translations);
    }

    [Fact]
    public void Course_has_expected_defaults_and_initialized_collections()
    {
        var course = new Course();

        Assert.Equal(StudyLevel.All, course.Level);
        Assert.Equal(StudyStatus.Draft, course.Status);
        Assert.NotNull(course.CourseContents);
        Assert.NotNull(course.LearningPathCourses);
        Assert.NotNull(course.Translations);
        Assert.NotNull(course.Prerequisites);
        Assert.NotNull(course.RequiredByCourses);
    }

    [Fact]
    public void Content_lesson_quiz_and_question_graphs_start_with_expected_defaults()
    {
        var content = new CourseContent();
        var lesson = new CourseLesson();
        var quiz = new CourseQuiz();
        var question = new QuizQuestion();
        var answer = new QuizAnswer();

        Assert.Equal(StudyStatus.Draft, content.Status);
        Assert.Equal(StudyStatus.Draft, lesson.Status);
        Assert.Equal(StudyStatus.Draft, quiz.Status);
        Assert.NotNull(content.CourseLessons);
        Assert.NotNull(content.CourseQuizzes);
        Assert.NotNull(content.Translations);
        Assert.NotNull(lesson.Translations);
        Assert.NotNull(quiz.QuizQuestions);
        Assert.NotNull(quiz.Translations);
        Assert.NotNull(question.Answers);
        Assert.NotNull(question.Translations);
        Assert.NotNull(answer.Translations);
    }

    [Fact]
    public void Learning_path_entities_have_expected_defaults_and_collections()
    {
        var category = new LearningPathCategory();
        var path = new LearningPath();
        var membership = new LearningPathCourse();

        Assert.Equal(StudyStatus.Draft, category.Status);
        Assert.Equal(StudyLevel.Beginner, path.Level);
        Assert.Equal(StudyStatus.Draft, path.Status);
        Assert.NotNull(category.LearningPaths);
        Assert.NotNull(category.Translations);
        Assert.NotNull(path.LearningPathCourses);
        Assert.NotNull(path.Translations);
        Assert.Equal(0, membership.Order);
    }

    [Fact]
    public void Translation_entities_use_french_draft_defaults()
    {
        foreach (var type in CatalogEntities().Select(x => (Type)x[0]!).Where(IsTranslation))
        {
            var translation = (StudyTranslationBase)Activator.CreateInstance(type)!;
            Assert.Equal("fr", translation.LanguageCode);
            Assert.Equal(StudyStatus.Draft, translation.PublicationStatus);
        }
    }

    [Fact]
    public void Course_prerequisite_contains_both_course_keys()
    {
        var courseId = Guid.NewGuid();
        var prerequisiteId = Guid.NewGuid();
        var prerequisite = new CoursePrerequisite
        {
            CourseId = courseId,
            PrerequisiteCourseId = prerequisiteId
        };

        Assert.Equal(courseId, prerequisite.CourseId);
        Assert.Equal(prerequisiteId, prerequisite.PrerequisiteCourseId);
    }

    private static bool IsTranslation(Type type) => typeof(StudyTranslationBase).IsAssignableFrom(type);

    private static object? ValueFor(Type type)
    {
        if (type == typeof(string)) return "value";
        if (type == typeof(Guid)) return Guid.NewGuid();
        if (type == typeof(DateTime)) return DateTime.UtcNow;
        if (type == typeof(DateTime?)) return DateTime.UtcNow;
        if (type == typeof(int)) return 7;
        if (type == typeof(int?)) return 7;
        if (type == typeof(bool)) return true;
        if (type.IsEnum) return Enum.GetValues(type).GetValue(0);
        if (Nullable.GetUnderlyingType(type) is { } nullableType && nullableType.IsEnum)
            return Enum.GetValues(nullableType).GetValue(0);
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            return Activator.CreateInstance(type);
        return null;
    }
}