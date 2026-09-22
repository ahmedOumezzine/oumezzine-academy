using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OumezzineAcademy.Domain.Catalog;

namespace OumezzineAcademy.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions options)
        : base(options)
    {
    }

    public DbSet<CourseCategory> CourseCategories => Set<CourseCategory>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<CourseContent> CourseContents => Set<CourseContent>();
    public DbSet<CourseLesson> CourseLessons => Set<CourseLesson>();
    public DbSet<CourseQuiz> CourseQuizzes => Set<CourseQuiz>();
    public DbSet<QuizQuestion> QuizQuestions => Set<QuizQuestion>();
    public DbSet<QuizAnswer> QuizAnswers => Set<QuizAnswer>();
    public DbSet<LearningPathCategory> LearningPathCategories => Set<LearningPathCategory>();
    public DbSet<LearningPath> LearningPaths => Set<LearningPath>();
    public DbSet<LearningPathCourse> LearningPathCourses => Set<LearningPathCourse>();
    public DbSet<CourseTranslation> CourseTranslations => Set<CourseTranslation>();
    public DbSet<CourseCategoryTranslation> CourseCategoryTranslations => Set<CourseCategoryTranslation>();
    public DbSet<CourseContentTranslation> CourseContentTranslations => Set<CourseContentTranslation>();
    public DbSet<CourseLessonTranslation> CourseLessonTranslations => Set<CourseLessonTranslation>();
    public DbSet<CourseQuizTranslation> CourseQuizTranslations => Set<CourseQuizTranslation>();
    public DbSet<QuizQuestionTranslation> QuizQuestionTranslations => Set<QuizQuestionTranslation>();
    public DbSet<QuizAnswerTranslation> QuizAnswerTranslations => Set<QuizAnswerTranslation>();
    public DbSet<LearningPathTranslation> LearningPathTranslations => Set<LearningPathTranslation>();
    public DbSet<LearningPathCategoryTranslation> LearningPathCategoryTranslations => Set<LearningPathCategoryTranslation>();
    public DbSet<CoursePrerequisite> CoursePrerequisites => Set<CoursePrerequisite>();

    public DbSet<CourseCategory> StudyCourseCategories => CourseCategories;
    public DbSet<Course> StudyCourses => Courses;
    public DbSet<CourseContent> StudyCourseContents => CourseContents;
    public DbSet<CourseLesson> StudyCourseLessons => CourseLessons;
    public DbSet<CourseQuiz> StudyCourseQuizzes => CourseQuizzes;
    public DbSet<QuizQuestion> StudyQuizQuestions => QuizQuestions;
    public DbSet<QuizAnswer> StudyQuizAnswers => QuizAnswers;
    public DbSet<LearningPathCategory> StudyLearningPathCategories => LearningPathCategories;
    public DbSet<LearningPath> StudyLearningPaths => LearningPaths;
    public DbSet<LearningPathCourse> StudyLearningPathCourses => LearningPathCourses;
    public DbSet<CourseTranslation> StudyCourseTranslations => CourseTranslations;
    public DbSet<CourseCategoryTranslation> StudyCourseCategoryTranslations => CourseCategoryTranslations;
    public DbSet<CourseContentTranslation> StudyCourseContentTranslations => CourseContentTranslations;
    public DbSet<CourseLessonTranslation> StudyCourseLessonTranslations => CourseLessonTranslations;
    public DbSet<CourseQuizTranslation> StudyCourseQuizTranslations => CourseQuizTranslations;
    public DbSet<QuizQuestionTranslation> StudyQuizQuestionTranslations => QuizQuestionTranslations;
    public DbSet<QuizAnswerTranslation> StudyQuizAnswerTranslations => QuizAnswerTranslations;
    public DbSet<LearningPathTranslation> StudyLearningPathTranslations => LearningPathTranslations;
    public DbSet<LearningPathCategoryTranslation> StudyLearningPathCategoryTranslations => LearningPathCategoryTranslations;
    public DbSet<CoursePrerequisite> StudyCoursePrerequisites => CoursePrerequisites;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<CourseCategory>().ToTable("CourseCategories");
        builder.Entity<Course>().ToTable("Courses");
        builder.Entity<CourseContent>().ToTable("CourseContents");
        builder.Entity<CourseLesson>().ToTable("CourseLessons");
        builder.Entity<CourseQuiz>().ToTable("CourseQuizzes");
        builder.Entity<QuizQuestion>().ToTable("QuizQuestions");
        builder.Entity<QuizAnswer>().ToTable("QuizAnswers");
        builder.Entity<LearningPathCategory>().ToTable("LearningPathCategories");
        builder.Entity<LearningPath>().ToTable("LearningPaths");
        builder.Entity<LearningPathCourse>().ToTable("LearningPathCourses");
        builder.Entity<CourseTranslation>().ToTable("CourseTranslations");
        builder.Entity<CourseCategoryTranslation>().ToTable("CourseCategoryTranslations");
        builder.Entity<CourseContentTranslation>().ToTable("CourseContentTranslations");
        builder.Entity<CourseLessonTranslation>().ToTable("CourseLessonTranslations");
        builder.Entity<CourseQuizTranslation>().ToTable("CourseQuizTranslations");
        builder.Entity<QuizQuestionTranslation>().ToTable("QuizQuestionTranslations");
        builder.Entity<QuizAnswerTranslation>().ToTable("QuizAnswerTranslations");
        builder.Entity<LearningPathTranslation>().ToTable("LearningPathTranslations");
        builder.Entity<LearningPathCategoryTranslation>().ToTable("LearningPathCategoryTranslations");
        builder.Entity<CoursePrerequisite>().ToTable("CoursePrerequisites");

        // Legacy shared tables may have no default constraint. Always send the value,
        // including false, instead of relying on the database to generate it.
        foreach (var type in new[] { typeof(CourseCategory), typeof(Course), typeof(CourseContent), typeof(CourseLesson), typeof(CourseQuiz), typeof(QuizQuestion), typeof(QuizAnswer), typeof(LearningPathCategory), typeof(LearningPath), typeof(LearningPathCourse) })
            builder.Entity(type).Property<bool>("IsDeleted").HasDefaultValue(false).ValueGeneratedNever();

        builder.Entity<CoursePrerequisite>().HasKey(x => new { x.CourseId, x.PrerequisiteCourseId });
        builder.Entity<CoursePrerequisite>().HasOne(x => x.Course).WithMany(x => x.Prerequisites).HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<CoursePrerequisite>().HasOne(x => x.PrerequisiteCourse).WithMany(x => x.RequiredByCourses).HasForeignKey(x => x.PrerequisiteCourseId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Course>().HasOne(x => x.CourseCategory).WithMany(x => x.Courses).HasForeignKey(x => x.CourseCategoryId);
        builder.Entity<CourseContent>().HasOne(x => x.Course).WithMany(x => x.CourseContents).HasForeignKey(x => x.CourseId);
        builder.Entity<CourseLesson>().HasOne(x => x.CourseContent).WithMany(x => x.CourseLessons).HasForeignKey(x => x.CourseContentId);
        builder.Entity<CourseQuiz>().HasOne(x => x.CourseContent).WithMany(x => x.CourseQuizzes).HasForeignKey(x => x.CourseContentId);
        builder.Entity<QuizQuestion>().HasOne(x => x.CourseQuiz).WithMany(x => x.QuizQuestions).HasForeignKey(x => x.CourseQuizId);
        builder.Entity<QuizAnswer>().HasOne(x => x.QuizQuestion).WithMany(x => x.Answers).HasForeignKey(x => x.QuizQuestionId);
        builder.Entity<LearningPath>().HasOne(x => x.LearningPathCategory).WithMany(x => x.LearningPaths).HasForeignKey(x => x.LearningPathCategoryId);
        builder.Entity<LearningPathCourse>().HasOne(x => x.LearningPath).WithMany(x => x.LearningPathCourses).HasForeignKey(x => x.LearningPathId);
        builder.Entity<LearningPathCourse>().HasOne(x => x.Course).WithMany(x => x.LearningPathCourses).HasForeignKey(x => x.CourseId);

        builder.Entity<CourseTranslation>().HasIndex(x => new { x.CourseId, x.LanguageCode }).IsUnique();
        builder.Entity<CourseCategoryTranslation>().HasIndex(x => new { x.CourseCategoryId, x.LanguageCode }).IsUnique();
        builder.Entity<CourseContentTranslation>().HasIndex(x => new { x.CourseContentId, x.LanguageCode }).IsUnique();
        builder.Entity<CourseLessonTranslation>().HasIndex(x => new { x.CourseLessonId, x.LanguageCode }).IsUnique();
        builder.Entity<CourseQuizTranslation>().HasIndex(x => new { x.CourseQuizId, x.LanguageCode }).IsUnique();
        builder.Entity<QuizQuestionTranslation>().HasIndex(x => new { x.QuizQuestionId, x.LanguageCode }).IsUnique();
        builder.Entity<QuizAnswerTranslation>().HasIndex(x => new { x.QuizAnswerId, x.LanguageCode }).IsUnique();
        builder.Entity<LearningPathTranslation>().HasIndex(x => new { x.LearningPathId, x.LanguageCode }).IsUnique();
        builder.Entity<LearningPathCategoryTranslation>().HasIndex(x => new { x.LearningPathCategoryId, x.LanguageCode }).IsUnique();
        builder.Entity<CourseTranslation>().HasIndex(x => new { x.LanguageCode, x.Slug }).IsUnique();
        builder.Entity<CourseCategoryTranslation>().HasIndex(x => new { x.LanguageCode, x.Slug }).IsUnique();
        builder.Entity<CourseLessonTranslation>().HasIndex(x => new { x.LanguageCode, x.Slug }).IsUnique();
        builder.Entity<CourseQuizTranslation>().HasIndex(x => new { x.LanguageCode, x.Slug }).IsUnique();
        builder.Entity<LearningPathTranslation>().HasIndex(x => new { x.LanguageCode, x.Slug }).IsUnique();
        builder.Entity<LearningPathCategoryTranslation>().HasIndex(x => new { x.LanguageCode, x.Slug }).IsUnique();
    }
}