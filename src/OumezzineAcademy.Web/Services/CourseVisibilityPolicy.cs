using System.Linq.Expressions;
using OumezzineAcademy.Domain.Catalog;
using OumezzineAcademy.Models.Catalog;
using OumezzineAcademy.Application.Abstractions;

namespace OumezzineAcademy.Web.Services;

/// <summary>
/// Publication gates used by the general public catalogue and course details.
/// Lessons, category publication, artwork and optional content are not gates.
/// A detail request additionally selects the published translation by its slug.
/// </summary>
public static class CourseVisibilityPolicy
{
    public static Expression<Func<Course, bool>> ForLanguage(string languageCode)
        => course => course.Status == StudyStatus.Published
            && course.Translations.Any(t => t.LanguageCode == languageCode && t.PublicationStatus == StudyStatus.Published);

    // Uses the scalar statuses already selected in the Admin page query; no database calls.
    public static CourseVisibilityResult Evaluate(StudyStatus courseStatus, StudyStatus? translationStatus, string languageCode)
    {
        var language = languageCode == "en" ? "anglaise" : "française";
        var reasons = new List<string>();
        if (courseStatus != StudyStatus.Published)
            reasons.Add(courseStatus switch
            {
                StudyStatus.Draft => "Le statut global du cours est en brouillon.",
                StudyStatus.Archived => "Le cours est archivé au niveau global.",
                _ => "Le statut global du cours n’est pas publié."
            });
        if (translationStatus != StudyStatus.Published)
            reasons.Add(translationStatus switch
            {
                null => $"Aucune traduction {language}.",
                StudyStatus.Draft => $"La traduction {language} est en brouillon.",
                StudyStatus.Archived => $"La traduction {language} est archivée.",
                _ => $"La traduction {language} n’est pas publiée."
            });
        return new(reasons.Count == 0, reasons.AsReadOnly());
    }
}

