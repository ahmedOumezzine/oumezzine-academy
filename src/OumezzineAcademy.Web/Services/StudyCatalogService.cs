using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Domain.Catalog;
using OumezzineAcademy.Models.Catalog;

namespace OumezzineAcademy.Web.Services;

public interface ICourseCatalogService
{
    Task<HomeViewModel> GetHomeAsync();
    Task<CourseSearchViewModel> SearchCoursesAsync(string? q,string? category,StudyLevel? level,string? sort,string? view,int page,int pageSize);
    Task<CourseDetailViewModel?> GetCourseAsync(string slug);
    Task<LessonDetailViewModel?> GetLessonAsync(string slug);
    Task<IReadOnlyList<CategoryCardViewModel>> GetCategoriesAsync();
    Task<CategoryDetailViewModel?> GetCategoryAsync(string slug);
}
public interface ILearningPathCatalogService
{
    Task<LearningPathListViewModel> GetPathsAsync(string? category,StudyLevel? level);
    Task<LearningPathDetailViewModel?> GetPathAsync(string slug);
}

public sealed class CourseCatalogService(ICourseCatalogQueries queries, ICurrentLanguageService language) : ICourseCatalogService
{
    private string Lang => language.LanguageCode;
    private static CourseCardViewModel Card(CourseSummaryDto x)=>new(){Id=x.Id,Title=x.Title,Slug=x.Slug,Summary=x.Summary,Thumbnail=x.Thumbnail,Level=x.Level,CategoryTitle=x.CategoryTitle,CategorySlug=x.CategorySlug,LessonCount=x.LessonCount,QuizCount=x.QuizCount,DurationMinutes=x.DurationMinutes,CreatedOnUtc=x.CreatedOnUtc};
    private static CategoryCardViewModel Category(CategorySummaryDto x)=>new(){Title=x.Title,Slug=x.Slug,Summary=x.Summary,CourseCount=x.CourseCount};
    private static LearningPathCardViewModel Path(LearningPathSummaryDto x)=>new(){Title=x.Title,Slug=x.Slug,Summary=x.Summary,Thumbnail=x.Thumbnail,Level=x.Level,CategoryTitle=x.CategoryTitle,CategorySlug=x.CategorySlug,CategorySummary=x.CategorySummary,CourseCount=x.CourseCount};
    private static CourseLessonViewModel Lesson(LessonSummaryDto x)=>new(){Title=x.Title,Slug=x.Slug,Summary=x.Summary,Description=x.Description,VideoUrl=x.VideoUrl,DurationMinutes=x.DurationMinutes,Order=x.Order};
    private static CourseQuizCardViewModel Quiz(QuizSummaryDto x)=>new(){Title=x.Title,Slug=x.Slug,Summary=x.Summary,QuestionCount=x.QuestionCount,Order=x.Order};
    public async Task<HomeViewModel> GetHomeAsync(){var x=await queries.GetHomeAsync(Lang);return new(){CourseCount=x.CourseCount,CategoryCount=x.CategoryCount,LearningPathCount=x.LearningPathCount,LessonCount=x.LessonCount,LatestCourses=x.LatestCourses.Select(Card).ToList(),TopCategories=x.TopCategories.Select(Category).ToList(),LatestLearningPaths=x.LatestLearningPaths.Select(Path).ToList()};}
    public async Task<CourseSearchViewModel> SearchCoursesAsync(string? q,string? category,StudyLevel? level,string? sort,string? view,int page,int pageSize){var x=await queries.SearchCoursesAsync(new(q,category,level,sort,page,pageSize,Lang));return new(){Q=q,Category=category,Level=level,Sort=sort??"recent",View=view=="list"?"list":"grid",Page=x.Page,PageSize=x.PageSize,TotalItems=x.TotalItems,Courses=x.Items.Select(Card).ToList()};}
    public async Task<IReadOnlyList<CategoryCardViewModel>> GetCategoriesAsync()=> (await queries.GetCategoriesAsync(Lang)).Select(Category).ToList();
    public async Task<CategoryDetailViewModel?> GetCategoryAsync(string slug){var x=await queries.GetCategoryAsync(slug,Lang);if(x is null)return null;return new(){Title=x.Category.Title,Slug=x.Category.Slug,Summary=x.Category.Summary,CourseCount=x.Category.CourseCount,MetaTitle=x.MetaTitle,MetaDescription=x.MetaDescription,Courses=x.Courses.Select(Card).ToList()};}
    public async Task<CourseDetailViewModel?> GetCourseAsync(string slug){var x=await queries.GetCourseAsync(slug,Lang);if(x is null)return null;return new(){Id=x.Course.Id,Title=x.Course.Title,Slug=x.Course.Slug,Summary=x.Course.Summary,Thumbnail=x.Course.Thumbnail,Level=x.Course.Level,CategoryTitle=x.Course.CategoryTitle,CategorySlug=x.Course.CategorySlug,LessonCount=x.Course.LessonCount,QuizCount=x.Course.QuizCount,DurationMinutes=x.Course.DurationMinutes,CreatedOnUtc=x.Course.CreatedOnUtc,Overview=x.Overview,WhatYouLearn=x.WhatYouLearn,Requirements=x.Requirements,Audience=x.Audience,MetaTitle=x.MetaTitle,MetaDescription=x.MetaDescription,UpdatedOnUtc=x.UpdatedOnUtc,CategorySummary=x.CategorySummary,Contents=x.Chapters.Select(c=>new CourseContentViewModel{Title=c.Title,Slug=c.Slug,Summary=c.Summary,Order=c.Order,Lessons=c.Lessons.Select(Lesson).ToList(),Quizzes=c.Quizzes.Select(Quiz).ToList()}).ToList(),RelatedCourses=x.RelatedCourses.Select(Card).ToList()};}
    public async Task<LessonDetailViewModel?> GetLessonAsync(string slug){var x=await queries.GetLessonAsync(slug,Lang);if(x is null)return null;return new(){Title=x.Lesson.Title,Slug=x.Lesson.Slug,Summary=x.Lesson.Summary,Description=x.Description,VideoUrl=x.VideoUrl,DocumentUrl=x.DocumentUrl,DurationMinutes=x.Lesson.DurationMinutes,Order=x.Lesson.Order,MetaTitle=x.MetaTitle,MetaDescription=x.MetaDescription,CourseTitle=x.CourseTitle,CourseSlug=x.CourseSlug,CourseSummary=x.CourseSummary,CourseThumbnail=x.CourseThumbnail,CategoryTitle=x.CategoryTitle,CategorySlug=x.CategorySlug,ChapterTitle=x.ChapterTitle,ChapterSummary=x.ChapterSummary,ChapterLessons=x.ChapterLessons.Select(Lesson).ToList(),ChapterQuizzes=x.ChapterQuizzes.Select(Quiz).ToList()};}
}

public sealed class LearningPathCatalogService(ILearningPathQueries queries,ICurrentLanguageService language) : ILearningPathCatalogService
{
    private string Lang=>language.LanguageCode;
    private static LearningPathCardViewModel Card(LearningPathSummaryDto x)=>new(){Title=x.Title,Slug=x.Slug,Summary=x.Summary,Thumbnail=x.Thumbnail,Level=x.Level,CategoryTitle=x.CategoryTitle,CategorySlug=x.CategorySlug,CategorySummary=x.CategorySummary,CourseCount=x.CourseCount};
    public async Task<LearningPathListViewModel> GetPathsAsync(string? category,StudyLevel? level){var x=await queries.GetPathsAsync(category,level,Lang);return new(){Category=category,Level=level,Paths=x.Select(Card).ToList()};}
    public async Task<LearningPathDetailViewModel?> GetPathAsync(string slug){var x=await queries.GetPathAsync(slug,Lang);if(x is null)return null;return new(){Title=x.LearningPath.Title,Slug=x.LearningPath.Slug,Summary=x.LearningPath.Summary,Thumbnail=x.LearningPath.Thumbnail,Level=x.LearningPath.Level,CategoryTitle=x.LearningPath.CategoryTitle,CategorySlug=x.LearningPath.CategorySlug,CategorySummary=x.LearningPath.CategorySummary,CourseCount=x.LearningPath.CourseCount,MetaTitle=x.MetaTitle,MetaDescription=x.MetaDescription,Courses=x.Courses.Select(c=>new LearningPathStepViewModel{CourseId=c.CourseId,CourseTitle=c.CourseTitle,CourseSlug=c.CourseSlug,Summary=c.Summary,ThumbnailUrl=c.Thumbnail,LessonCount=c.LessonCount,QuizCount=c.QuizCount,DurationMinutes=c.DurationMinutes,Order=c.Order}).ToList()};}
}
