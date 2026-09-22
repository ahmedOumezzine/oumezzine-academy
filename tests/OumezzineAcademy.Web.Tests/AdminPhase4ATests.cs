using Xunit;
using System.Text.RegularExpressions;

namespace OumezzineAcademy.Tests;

public sealed class AdminPhase4ATests
{
    private static string ProjectFile(string relativePath) => TestProjectFiles.FindOumezzineAcademyFile(relativePath);

    [Fact]
    public void SensitiveCatalogControllersRequireAdminRole()
    {
        Assert.Contains("Authorize(Roles = \"Admin\")", File.ReadAllText(ProjectFile("Areas/Admin/Controllers/CategoriesController.cs")));
        Assert.Contains("Authorize(Roles = \"Admin\")", File.ReadAllText(ProjectFile("Areas/Admin/Controllers/CoursesController.cs")));
    }

    [Fact]
    public void CatalogMutationsUseAntiforgery()
    {
        var categories = File.ReadAllText(ProjectFile("Areas/Admin/Controllers/CategoriesController.cs"));
        var courses = File.ReadAllText(ProjectFile("Areas/Admin/Controllers/CoursesController.cs"));
        Assert.True(categories.Count(x => x == '[') > 0 && categories.Contains("ValidateAntiForgeryToken"));
        Assert.Contains("ValidateAntiForgeryToken", courses);
    }

    [Fact]
    public void AdminUsesDedicatedTranslationViewModels()
    {
        var models = File.ReadAllText(ProjectFile("Areas/Admin/Models/CatalogAdminViewModels.cs"));
        Assert.Contains("class CategoryEditViewModel", models);
        Assert.Contains("class CourseEditViewModel", models);
        Assert.Contains("CategoryTranslationInput", models);
        Assert.Contains("CourseTranslationInput", models);
    }

    [Fact]
    public void CategoryAndCourseScreensAreNotComingSoonPlaceholders()
    {
        Assert.DoesNotContain("ComingSoon", File.ReadAllText(ProjectFile("Areas/Admin/Views/Courses/Index.cshtml")));
        Assert.DoesNotContain("ComingSoon", File.ReadAllText(ProjectFile("Areas/Admin/Views/Categories/Index.cshtml")));
    }

    [Fact]
    public void PublicNavigationExplicitlyExitsArea()
    {
        var viewsRoot = Path.GetDirectoryName(ProjectFile("Views/Shared/_Layout.cshtml"))!;
        var publicViews = Directory.GetFiles(viewsRoot, "*.cshtml", SearchOption.AllDirectories);
        var linkPattern = new Regex("<a\\b[^>]*(?:asp-controller|asp-action)\\b[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        foreach (var file in publicViews)
        {
            var markup = File.ReadAllText(file);
            foreach (Match link in linkPattern.Matches(markup))
            {
                Assert.Contains("asp-area=\"\"", link.Value, StringComparison.Ordinal);
            }
        }

        var layout = File.ReadAllText(ProjectFile("Views/Shared/_Layout.cshtml"));
        Assert.DoesNotContain("href=\"fr/", layout, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("href=\"en/", layout, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("href=\"/admin", layout, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AnonymousAdminLoginDoesNotRenderSensitiveNavigation()
    {
        var layout = File.ReadAllText(ProjectFile("Areas/Admin/Views/Shared/_Layout.cshtml"));
        var login = File.ReadAllText(ProjectFile("Areas/Admin/Views/Auth/Login.cshtml"));

        Assert.Contains("User.Identity?.IsAuthenticated == true && User.IsInRole(\"Admin\")", layout);
        Assert.Contains("if (isAdmin)", layout);
        Assert.Contains("href=\"@publicHome\"", layout);
        Assert.DoesNotContain("asp-area=\"\" href=\"@publicHome\"", layout);
        Assert.Contains("autocomplete=\"username\"", login);
        Assert.Contains("autocomplete=\"current-password\"", login);
        Assert.Contains("asp-for=\"RememberMe\"", login);
        Assert.Contains("method=\"post\"", login); // The Form Tag Helper emits the antiforgery token for POST.
        Assert.Contains("admin-login-submit", login);
    }

    [Fact]
    public void CourseFormHasSafeValidationAndThumbnailWorkflow()
    {
        var form = File.ReadAllText(ProjectFile("Areas/Admin/Views/Courses/_CourseForm.cshtml"));
        var create = File.ReadAllText(ProjectFile("Areas/Admin/Views/Courses/Create.cshtml"));
        var script = File.ReadAllText(ProjectFile("wwwroot/js/course-form.js"));
        var controller = File.ReadAllText(ProjectFile("Areas/Admin/Controllers/CoursesController.cs"));

        Assert.Contains("_AdminValidationSummary", form);
        Assert.Contains("data-thumbnail-input", form);
        Assert.Contains("DeleteThumbnail", form);
        Assert.Contains("ValidateAntiForgeryToken", controller);
        Assert.Contains("course-form.js", create);
        Assert.Contains("URL.createObjectURL", script);
        Assert.DoesNotContain("Phase 4D", form);
    }

    [Fact]
    public void AdminValidationSummaryFiltersEmptyAndTechnicalErrors()
    {
        var summary = File.ReadAllText(ProjectFile("Areas/Admin/Views/Shared/_AdminValidationSummary.cshtml"));
        var formPaths = new[]
        {
            "Areas/Admin/Views/Categories/Create.cshtml",
            "Areas/Admin/Views/Chapters/Edit.cshtml",
            "Areas/Admin/Views/Courses/_CourseForm.cshtml",
            "Areas/Admin/Views/Lessons/_LessonForm.cshtml"
        };

        Assert.Contains("errors.Count > 0", summary);
        Assert.Contains("!string.IsNullOrWhiteSpace(error.ErrorMessage)", summary);
        Assert.Contains("error.Exception is not null", summary);
        Assert.Contains("Distinct", summary);
        Assert.DoesNotContain("asp-validation-summary", summary);
        Assert.DoesNotContain("<li></li>", summary, StringComparison.OrdinalIgnoreCase);
        foreach (var path in formPaths)
            Assert.Contains("_AdminValidationSummary", File.ReadAllText(ProjectFile(path)));
    }

    [Fact]
    public void LessonFormMatchesVisualConceptWithoutChangingServerSanitization()
    {
        var edit = File.ReadAllText(ProjectFile("Areas/Admin/Views/Lessons/Edit.cshtml"));
        var form = File.ReadAllText(ProjectFile("Areas/Admin/Views/Lessons/_LessonForm.cshtml"));
        var fields = File.ReadAllText(ProjectFile("Areas/Admin/Views/Lessons/_LessonTranslationFields.cshtml"));
        var seo = File.ReadAllText(ProjectFile("Areas/Admin/Views/Lessons/_LessonSeoFields.cshtml"));
        var script = File.ReadAllText(ProjectFile("wwwroot/js/lesson-form.js"));
        var controller = File.ReadAllText(ProjectFile("Areas/Admin/Controllers/LessonsController.cs"));
        var service = File.ReadAllText(ProjectFile("Services/AdminContentServices.cs"));

        Assert.Contains("_LessonForm", edit);
        Assert.Contains("lesson-form-columns", form);
        Assert.Contains("data-lesson-tab", form);
        Assert.Contains("data-lesson-image-input", form);
        Assert.Contains("id=\"lessonImageUpload\"", form);
        Assert.Contains("for=\"lessonImageUpload\"", form);
        Assert.Contains("multipart/form-data", form);
        Assert.Contains("image/jpeg", form);
        Assert.Contains("data-preview-title", form);
        Assert.Contains("data-preview-image", form);
        Assert.DoesNotContain("data-editor-command", fields);
        Assert.DoesNotContain("contenteditable=\"true\"", fields);
        Assert.Contains("data-lesson-editor", fields);
        Assert.Contains("data-editor-textarea", fields);
        Assert.Contains("aria-label=\"Contenu de la leçon", fields);
        Assert.DoesNotContain("MetaTitle", fields);
        Assert.DoesNotContain("MetaDescription", fields);
        Assert.Contains("data-count-for", seo);
        Assert.Contains("MetaTitle", seo);
        Assert.Contains("MetaDescription", seo);
        Assert.Contains("data-preview-html", form);
        Assert.Contains("data-word-count", fields);
        Assert.Contains("data-seo-tab", form);
        Assert.Contains("_LessonSeoFields", form);
        Assert.Contains("UpdatePreview", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("safeHtml", script);
        var editorScript = File.ReadAllText(ProjectFile("wwwroot/js/admin-lesson-editor.js"));
        Assert.Contains("CK.WordCount", editorScript);
        Assert.Contains("addEventListener('submit'", editorScript);
        Assert.Contains("dragenter", script);
        Assert.Contains("dragover", script);
        Assert.Contains("dragleave", script);
        Assert.Contains("drop", script);
        Assert.Contains("insertTable", editorScript);
        Assert.Contains("CK.Heading", editorScript);
        Assert.Contains("lesson-action-bar", form);
        Assert.Contains("IAdminLessonCoreCommands", service);
        Assert.Contains("ValidateAntiForgeryToken", controller);
        Assert.Contains("DeleteImage", controller);
        Assert.DoesNotContain("<script>", edit, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CourseContentScreensKeepScopedContextAndPostOnlyOrdering()
    {
        var models = File.ReadAllText(ProjectFile("Areas/Admin/Models/CatalogAdminViewModels.cs"));
        var services = File.ReadAllText(ProjectFile("Services/AdminContentServices.cs"));
        var chapters = File.ReadAllText(ProjectFile("Areas/Admin/Views/Chapters/Index.cshtml"));
        var lessons = File.ReadAllText(ProjectFile("Areas/Admin/Views/Lessons/Index.cshtml"));

        Assert.Contains("class AdminCourseContentViewModel", models);
        Assert.Contains("class AdminLessonListViewModel", models);
        Assert.DoesNotContain("ApplicationDbContext", services);
        Assert.DoesNotContain("SaveChanges", services);
        Assert.Contains("asp-action=\"Move\"", chapters);
        Assert.Contains("asp-action=\"Move\"", lessons);
        Assert.Contains("method=\"post\"", chapters);
        Assert.Contains("method=\"post\"", lessons);
        Assert.Contains("asp-route-courseId=\"@Model.CourseId\"", lessons);
        Assert.Contains("Leçon sans traduction", lessons);
    }

    [Fact]
    public void QuizAdminCrudIsScopedAndPostOnly()
    {
        var controller = File.ReadAllText(ProjectFile("Areas/Admin/Controllers/QuizzesController.cs"));
        var questionsController = File.ReadAllText(ProjectFile("Areas/Admin/Controllers/QuestionsController.cs"));
        var service = File.ReadAllText(ProjectFile("Services/AdminQuizService.cs")) + File.ReadAllText(ProjectFile("Services/AdminQuestionService.cs"));
        var quizList = File.ReadAllText(ProjectFile("Areas/Admin/Views/Quizzes/Index.cshtml"));
        var quizForm = File.ReadAllText(ProjectFile("Areas/Admin/Views/Quizzes/Edit.cshtml"));
        var quizTranslation = File.ReadAllText(ProjectFile("Areas/Admin/Views/Quizzes/_QuizTranslationFields.cshtml"));
        var questionList = File.ReadAllText(ProjectFile("Areas/Admin/Views/Questions/Index.cshtml"));
        var questionForm = File.ReadAllText(ProjectFile("Areas/Admin/Views/Questions/Edit.cshtml"));
        var answerFields = File.ReadAllText(ProjectFile("Areas/Admin/Views/Questions/_AnswerFields.cshtml"));

        Assert.Contains("Authorize(Roles = \"Admin\")", controller);
        Assert.Contains("/quizzes", controller);
        Assert.Contains("ValidateAntiForgeryToken", controller);
        Assert.Contains("QuestionsController", questionsController);
        Assert.Contains("IAdminQuizPersistence", service);
        Assert.Contains("SlugExistsAsync", service);
        Assert.Contains("IAdminQuizQueries", service);
        Assert.Contains("asp-controller=\"Questions\"", quizList);
        Assert.Contains("data-quiz-tab", quizForm);
        Assert.Contains("data-slug-target", quizTranslation);
        Assert.Contains("asp-action=\"Edit\"", questionList);
        Assert.Contains("data-question-tab", questionForm);
        Assert.Contains("CorrectAnswerIndex", answerFields);
        Assert.Contains("type=\"radio\"", answerFields);
        Assert.Contains("data-add-answer", questionForm);
    }
}

