using Microsoft.EntityFrameworkCore;
using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Domain.Catalog;
using OumezzineAcademy.Infrastructure.Data;
namespace OumezzineAcademy.Infrastructure.Persistence;
public sealed class EfLearningPathQueries(ApplicationDbContext db) : ILearningPathQueries
{
 public async Task<IReadOnlyList<LearningPathSummaryDto>> GetPathsAsync(string? category,StudyLevel? level,string languageCode,CancellationToken token=default)
 {
  var q=db.StudyLearningPaths.AsNoTracking().Where(p=>p.Status==StudyStatus.Published
   && p.Translations.Any(t=>t.LanguageCode==languageCode&&t.PublicationStatus==StudyStatus.Published)
   );
  if(!string.IsNullOrWhiteSpace(category)) q=q.Where(p=>p.LearningPathCategory.Translations.Any(t=>t.LanguageCode==languageCode&&t.PublicationStatus==StudyStatus.Published&&t.Slug==category));
  if(level.HasValue&&level.Value!=StudyLevel.All) q=q.Where(p=>p.Level==level.Value);
  return await q.OrderByDescending(p=>p.CreatedOnUtc).Select(p=>new LearningPathSummaryDto(
   p.Translations.Where(t=>t.LanguageCode==languageCode&&t.PublicationStatus==StudyStatus.Published).Select(t=>t.Title).FirstOrDefault()!,
   p.Translations.Where(t=>t.LanguageCode==languageCode&&t.PublicationStatus==StudyStatus.Published).Select(t=>t.Slug).FirstOrDefault()!,
   p.Translations.Where(t=>t.LanguageCode==languageCode&&t.PublicationStatus==StudyStatus.Published).Select(t=>t.Summary).FirstOrDefault(),p.Thumbnail,p.Level,
   p.LearningPathCategory.Translations.Where(t=>t.LanguageCode==languageCode&&t.PublicationStatus==StudyStatus.Published).Select(t=>t.Title).FirstOrDefault()!,
   p.LearningPathCategory.Translations.Where(t=>t.LanguageCode==languageCode&&t.PublicationStatus==StudyStatus.Published).Select(t=>t.Slug).FirstOrDefault()!,
   p.LearningPathCategory.Translations.Where(t=>t.LanguageCode==languageCode&&t.PublicationStatus==StudyStatus.Published).Select(t=>t.Summary).FirstOrDefault(),
   p.LearningPathCourses.Count(pc=>pc.Course.Status==StudyStatus.Published&&pc.Course.Translations.Any(t=>t.LanguageCode==languageCode&&t.PublicationStatus==StudyStatus.Published)))).ToListAsync(token);
 }
 public async Task<LearningPathDetailsDto?> GetPathAsync(string slug,string languageCode,CancellationToken token=default)
 {
  var path=await db.StudyLearningPaths.AsNoTracking().Where(p=>p.Status==StudyStatus.Published
   && p.Translations.Any(t=>t.LanguageCode==languageCode&&t.PublicationStatus==StudyStatus.Published&&t.Slug==slug))
   .Select(p=>new {
    p.Id,p.Level,p.Thumbnail,
    Title=p.Translations.Where(t=>t.LanguageCode==languageCode&&t.PublicationStatus==StudyStatus.Published).Select(t=>t.Title).FirstOrDefault()!,
    PathSlug=p.Translations.Where(t=>t.LanguageCode==languageCode&&t.PublicationStatus==StudyStatus.Published).Select(t=>t.Slug).FirstOrDefault()!,
    Summary=p.Translations.Where(t=>t.LanguageCode==languageCode&&t.PublicationStatus==StudyStatus.Published).Select(t=>t.Summary).FirstOrDefault(),
    MetaTitle=p.Translations.Where(t=>t.LanguageCode==languageCode&&t.PublicationStatus==StudyStatus.Published).Select(t=>t.MetaTitle).FirstOrDefault(),
    MetaDescription=p.Translations.Where(t=>t.LanguageCode==languageCode&&t.PublicationStatus==StudyStatus.Published).Select(t=>t.MetaDescription).FirstOrDefault(),
    CategoryTitle=p.LearningPathCategory.Translations.Where(t=>t.LanguageCode==languageCode&&t.PublicationStatus==StudyStatus.Published).Select(t=>t.Title).FirstOrDefault()!,
    CategorySlug=p.LearningPathCategory.Translations.Where(t=>t.LanguageCode==languageCode&&t.PublicationStatus==StudyStatus.Published).Select(t=>t.Slug).FirstOrDefault()!,
    CategorySummary=p.LearningPathCategory.Translations.Where(t=>t.LanguageCode==languageCode&&t.PublicationStatus==StudyStatus.Published).Select(t=>t.Summary).FirstOrDefault(),
    CourseCount=p.LearningPathCourses.Count(pc=>pc.Course.Status==StudyStatus.Published&&pc.Course.Translations.Any(t=>t.LanguageCode==languageCode&&t.PublicationStatus==StudyStatus.Published))
   }).FirstOrDefaultAsync(token);
  if(path is null)return null;
  var courses=await db.StudyLearningPathCourses.AsNoTracking().Where(pc=>pc.LearningPathId==path.Id&&pc.Course.Status==StudyStatus.Published&&pc.Course.Translations.Any(t=>t.LanguageCode==languageCode&&t.PublicationStatus==StudyStatus.Published)).OrderBy(pc=>pc.Order).Select(pc=>new LearningPathCourseDto(
   pc.Course.Id,
   pc.Course.Translations.Where(t=>t.LanguageCode==languageCode&&t.PublicationStatus==StudyStatus.Published).Select(t=>t.Title).FirstOrDefault()!,
   pc.Course.Translations.Where(t=>t.LanguageCode==languageCode&&t.PublicationStatus==StudyStatus.Published).Select(t=>t.Slug).FirstOrDefault()!,
   pc.Course.Translations.Where(t=>t.LanguageCode==languageCode&&t.PublicationStatus==StudyStatus.Published).Select(t=>t.Summary).FirstOrDefault(),pc.Course.Thumbnail,
   pc.Course.CourseContents.SelectMany(x=>x.CourseLessons).Count(l=>l.Status==StudyStatus.Published&&l.Translations.Any(t=>t.LanguageCode==languageCode&&t.PublicationStatus==StudyStatus.Published)),
   pc.Course.CourseContents.SelectMany(x=>x.CourseQuizzes).Count(q=>q.Status==StudyStatus.Published&&q.Translations.Any(t=>t.LanguageCode==languageCode&&t.PublicationStatus==StudyStatus.Published)),
   pc.Course.CourseContents.SelectMany(x=>x.CourseLessons).Where(l=>l.Status==StudyStatus.Published&&l.Translations.Any(t=>t.LanguageCode==languageCode&&t.PublicationStatus==StudyStatus.Published)).Sum(l=>l.DurationMinutes??0),pc.Order)).ToListAsync(token);
  return new LearningPathDetailsDto(new LearningPathSummaryDto(path.Title,path.PathSlug,path.Summary,path.Thumbnail,path.Level,path.CategoryTitle,path.CategorySlug,path.CategorySummary,path.CourseCount),path.MetaTitle,path.MetaDescription,courses);
 }
}


