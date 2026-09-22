using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Areas.Admin.Models;

namespace OumezzineAcademy.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public sealed class DashboardController(IDashboardQueries queries) : Controller
{
    public async Task<IActionResult> Index(CancellationToken token)
    {
        var coursesUrl = Url.Action("Index", "Courses", new { area = "Admin" })!; var categoriesUrl = Url.Action("Index", "Categories", new { area = "Admin" })!;
        var s = await queries.GetSummaryAsync(token);
        var recent = s.RecentItems.Select(x => new AdminDashboardRecentItemViewModel { Type = x.Type, Title = x.Title, Action = x.Action, DateUtc = x.DateUtc, IconKey = x.IconKey, Url = x.ChapterId.HasValue ? Url.Action("Index", "Lessons", new { area = "Admin", courseId = x.CourseId, chapterId = x.ChapterId })! : x.Type == "Catégorie" ? categoriesUrl : coursesUrl }).ToList();
        return View(new AdminDashboardViewModel { CoursesTotal = s.CoursesTotal, CategoriesTotal = s.CategoriesTotal, FrenchPublishedCourses = s.FrenchPublishedCourses, EnglishPublishedCourses = s.EnglishPublishedCourses, EnglishDraftCourses = s.EnglishDraftCourses, EnglishMissingCourses = s.EnglishMissingCourses, LessonsTotal = s.LessonsTotal, QuizzesTotal = s.QuizzesTotal, LearningPathsTotal = s.LearningPathsTotal, CoursesWithoutThumbnail = s.CoursesWithoutThumbnail, DraftLessons = s.DraftLessons, LessonsMissingEn = s.LessonsMissingEn, RecentItems = recent });
    }
}