namespace OumezzineAcademy.Areas.Admin.Models;

public sealed record AdminCoursePublicationViewModel(
    string LanguageCode, StudyStatus? Status, CourseVisibilityResult Visibility, string TooltipId)
{
    public string LanguageName => LanguageCode == "en" ? "Anglais" : "Français";
    public string StatusLabel => Status switch
    {
        StudyStatus.Published => "Publié",
        StudyStatus.Draft => "Brouillon",
        StudyStatus.Archived => "Archivé",
        _ => "Non traduit"
    };
    public string StatusClass => Status switch
    {
        StudyStatus.Published => "status-published",
        StudyStatus.Draft => "status-draft",
        _ => "status-missing"
    };
    public string VisibilityLabel => Visibility.IsVisible ? "Visible" : "Non visible";
}