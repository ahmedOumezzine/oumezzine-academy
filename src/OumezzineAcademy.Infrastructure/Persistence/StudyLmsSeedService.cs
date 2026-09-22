using OumezzineAcademy.Infrastructure.Data;
using OumezzineAcademy.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using OumezzineAcademy.Application.Abstractions;

namespace OumezzineAcademy.Infrastructure.Persistence;

public sealed class StudyLmsSeedService : IStudyLmsSeeder
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<StudyLmsSeedService> _logger;
    public StudyLmsSeedService(ApplicationDbContext db, ILogger<StudyLmsSeedService> logger) => (_db, _logger) = (db, logger);

    public async Task SeedAsync(CancellationToken token = default)
    {
        _logger.LogInformation("Starting Oumezzine Academy demo seed.");
        await using var transaction = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(token)
            : null;
        var categories = new Dictionary<string, CourseCategory>(StringComparer.OrdinalIgnoreCase)
        {
            ["web"] = await CategoryAsync("web-development", "Développement Web", "Web Development", token),
            ["frontend"] = await CategoryAsync("frontend", "Frontend", "Frontend", token),
            ["backend"] = await CategoryAsync("backend", "Backend", "Backend", token),
            ["dotnet"] = await CategoryAsync("dotnet", ".NET", ".NET", token),
            ["databases"] = await CategoryAsync("databases", "Bases de données", "Databases", token),
            ["devops"] = await CategoryAsync("devops-tools", "DevOps & Outils", "DevOps & Tools", token)
        };
        var courses = new Dictionary<string, Course>(StringComparer.OrdinalIgnoreCase);
        foreach (var d in Definitions)
        {
            var course = await CourseAsync(d.Slug, d.FrTitle, categories[d.Category], d.Level, token);
            courses[d.Key] = course;
            await CourseTranslationAsync(course, "fr", d.FrTitle, d.FrSlug, d.FrSummary, token);
            await CourseTranslationAsync(course, "en", d.EnTitle, d.EnSlug, d.EnSummary, token);
        }

        var prerequisites = new[] { ("javascript", "html"), ("react", "javascript"), ("aspnet", "csharp"), ("efcore", "csharp"), ("api", "csharp"), ("api", "efcore"), ("docker", "git") };
        foreach (var (course, required) in prerequisites) await PrerequisiteAsync(courses[course], courses[required], token);

        var beginnerPaths = await PathCategoryAsync("getting-started", "Débuter", "Getting Started", token);
        var backendPaths = await PathCategoryAsync("backend", "Backend", "Backend", token);
        var frontendPaths = await PathCategoryAsync("frontend", "Frontend", "Frontend", token);
        var fullStackPaths = await PathCategoryAsync("full-stack", "Full-Stack", "Full-Stack", token);
        await PathAsync(beginnerPaths, "developpeur-web-debutant", "Développeur Web — Débutant", "Web Developer — Beginner", [courses["html"], courses["javascript"], courses["git"]], token);
        await PathAsync(backendPaths, "developpeur-backend-dotnet", "Développeur Backend .NET", ".NET Backend Developer", [courses["csharp"], courses["git"], courses["sql"], courses["efcore"], courses["aspnet"], courses["api"], courses["docker"]], token);
        await PathAsync(frontendPaths, "developpeur-frontend", "Développeur Frontend", "Frontend Developer", [courses["html"], courses["javascript"], courses["git"], courses["react"]], token);
        await PathAsync(fullStackPaths, "developpeur-full-stack-dotnet", "Développeur Full-Stack .NET", ".NET Full-Stack Developer", [courses["html"], courses["javascript"], courses["git"], courses["react"], courses["csharp"], courses["aspnet"], courses["api"], courses["sql"], courses["efcore"]], token);

        foreach (var definition in Definitions)
        {
            var course = courses[definition.Key];
            for (var chapterOrder = 1; chapterOrder <= 3; chapterOrder++)
                await ContentAsync(course, definition, chapterOrder, token);
        }
        SetSoftDeleteDefaults();
        await _db.SaveChangesAsync(token);
        foreach (var key in new[] { "html", "javascript", "csharp", "aspnet", "api", "efcore", "sql", "react", "docker" })
            foreach (var chapterOrder in new[] { 2, 3 }) await QuizAsync(courses[key], chapterOrder, token);
        SetSoftDeleteDefaults();
        await _db.SaveChangesAsync(token);
        if (transaction is not null) await transaction.CommitAsync(token);
        _logger.LogInformation("Demo seed completed: {CategoryCount} categories, {CourseCount} courses and {PathCount} paths.", categories.Count, courses.Count, 4);
    }

    private void SetSoftDeleteDefaults()
    {
        foreach (var entry in _db.ChangeTracker.Entries()
                     .Where(x => x.State == EntityState.Added && x.Metadata.FindProperty("IsDeleted") is not null))
        {
            var property = entry.Property("IsDeleted");
            if (property.CurrentValue is null)
                property.CurrentValue = false;
        }
    }

    private static Guid StableId(string key) => new(SHA256.HashData(Encoding.UTF8.GetBytes("oumezzine-demo:" + key))[..16]);

    private async Task<CourseCategory> CategoryAsync(string slug, string fr, string en, CancellationToken token)
    {
        var entity = await _db.StudyCourseCategories.AsNoTracking().FirstOrDefaultAsync(x => x.Slug == slug, token);
        if (entity is null) { entity = new CourseCategory { Id = StableId("category:" + slug), Slug = slug, Title = fr, Summary = fr, Status = StudyStatus.Published, CreatedOnUtc = DateTime.UtcNow }; _db.StudyCourseCategories.Add(entity); }
        await CategoryTranslationAsync(entity, "fr", fr, slug, token); await CategoryTranslationAsync(entity, "en", en, slug, token); return entity;
    }
    private async Task CategoryTranslationAsync(CourseCategory e, string lang, string title, string slug, CancellationToken token)
    { if (!await _db.StudyCourseCategoryTranslations.AnyAsync(x => x.CourseCategoryId == e.Id && x.LanguageCode == lang, token)) _db.StudyCourseCategoryTranslations.Add(new CourseCategoryTranslation { Id = StableId($"category-translation:{e.Id}:{lang}"), CourseCategoryId = e.Id, LanguageCode = lang, PublicationStatus = StudyStatus.Published, Title = title, Slug = slug, Summary = title }); }
    private async Task<Course> CourseAsync(string slug, string title, CourseCategory category, StudyLevel level, CancellationToken token)
    { var e = await _db.StudyCourses.AsNoTracking().FirstOrDefaultAsync(x => x.Slug == slug, token); if (e is null) { e = new Course { Id = StableId("course:" + slug), Slug = slug, Title = title, Summary = title, CourseCategoryId = category.Id, Level = level, Status = StudyStatus.Published, CreatedOnUtc = DateTime.UtcNow }; _db.StudyCourses.Add(e); } return e; }
    private async Task CourseTranslationAsync(Course e, string lang, string title, string slug, string summary, CancellationToken token)
    { if (!await _db.StudyCourseTranslations.AnyAsync(x => x.CourseId == e.Id && x.LanguageCode == lang, token)) _db.StudyCourseTranslations.Add(new CourseTranslation { Id = StableId($"course-translation:{e.Id}:{lang}"), CourseId = e.Id, LanguageCode = lang, PublicationStatus = StudyStatus.Published, Title = title, Slug = slug, Summary = summary, Overview = summary, MetaTitle = $"{title} | Oumezzine Academy", MetaDescription = summary }); }
    private async Task PrerequisiteAsync(Course e, Course required, CancellationToken token)
    { if (e.Id != required.Id && !await _db.StudyCoursePrerequisites.AnyAsync(x => x.CourseId == e.Id && x.PrerequisiteCourseId == required.Id, token)) _db.StudyCoursePrerequisites.Add(new CoursePrerequisite { CourseId = e.Id, PrerequisiteCourseId = required.Id }); }
    private async Task<LearningPathCategory> PathCategoryAsync(string slug, string fr, string en, CancellationToken token)
    { var e = await _db.StudyLearningPathCategories.AsNoTracking().FirstOrDefaultAsync(x => x.Slug == slug, token); if (e is null) { e = new LearningPathCategory { Id = StableId("path-category:" + slug), Slug = slug, Title = fr, Summary = fr, Status = StudyStatus.Published, CreatedOnUtc = DateTime.UtcNow }; _db.StudyLearningPathCategories.Add(e); } foreach (var (lang, title) in new[] { ("fr", fr), ("en", en) }) if (!await _db.StudyLearningPathCategoryTranslations.AnyAsync(x => x.LearningPathCategoryId == e.Id && x.LanguageCode == lang, token)) _db.StudyLearningPathCategoryTranslations.Add(new LearningPathCategoryTranslation { Id = StableId($"path-category-translation:{e.Id}:{lang}"), LearningPathCategoryId = e.Id, LanguageCode = lang, PublicationStatus = StudyStatus.Published, Title = title, Slug = $"{slug}-{lang}", Summary = title }); return e; }
    private async Task PathAsync(LearningPathCategory category, string slug, string fr, string en, IReadOnlyList<Course> courses, CancellationToken token)
    { var e = await _db.StudyLearningPaths.AsNoTracking().FirstOrDefaultAsync(x => x.Slug == slug, token); if (e is null) { e = new LearningPath { Id = StableId("path:" + slug), Slug = slug, Title = fr, Summary = fr, LearningPathCategoryId = category.Id, Level = slug.Contains("backend") || slug.Contains("full-stack") ? StudyLevel.Intermediate : StudyLevel.Beginner, Status = StudyStatus.Published, CreatedOnUtc = DateTime.UtcNow }; _db.StudyLearningPaths.Add(e); } foreach (var (lang, title) in new[] { ("fr", fr), ("en", en) }) if (!await _db.StudyLearningPathTranslations.AnyAsync(x => x.LearningPathId == e.Id && x.LanguageCode == lang, token)) _db.StudyLearningPathTranslations.Add(new LearningPathTranslation { Id = StableId($"path-translation:{e.Id}:{lang}"), LearningPathId = e.Id, LanguageCode = lang, PublicationStatus = StudyStatus.Published, Title = title, Slug = lang == "fr" ? slug : $"{slug}-en", Summary = title, MetaTitle = $"{title} | Oumezzine Academy", MetaDescription = title }); for (var i = 0; i < courses.Count; i++) if (!await _db.StudyLearningPathCourses.AnyAsync(x => x.LearningPathId == e.Id && x.CourseId == courses[i].Id, token)) _db.StudyLearningPathCourses.Add(new LearningPathCourse { Id = StableId($"path-course:{e.Id}:{courses[i].Id}"), LearningPathId = e.Id, CourseId = courses[i].Id, Order = i + 1 }); }
    private async Task ContentAsync(Course course, CourseSeed definition, int chapterOrder, CancellationToken token)
    {
        var topics = ChapterTopics[definition.Key];
        var chapterTopic = topics[chapterOrder - 1];
        var chapter = await _db.StudyCourseContents.AsNoTracking().FirstOrDefaultAsync(x => x.CourseId == course.Id && x.Order == chapterOrder, token);
        if (chapter is null) { chapter = new CourseContent { Id = StableId($"chapter:{course.Id}:{chapterOrder}"), CourseId = course.Id, Title = chapterTopic.Fr, Slug = $"{definition.FrSlug}-module-{chapterOrder}", Summary = chapterTopic.Fr, Order = chapterOrder, Status = StudyStatus.Published, CreatedOnUtc = DateTime.UtcNow }; _db.StudyCourseContents.Add(chapter); }
        foreach (var (lang, title) in new[] { ("fr", chapterTopic.Fr), ("en", chapterTopic.En) }) if (!await _db.StudyCourseContentTranslations.AnyAsync(x => x.CourseContentId == chapter.Id && x.LanguageCode == lang, token)) _db.StudyCourseContentTranslations.Add(new CourseContentTranslation { Id = StableId($"chapter-translation:{chapter.Id}:{lang}"), CourseContentId = chapter.Id, LanguageCode = lang, PublicationStatus = StudyStatus.Published, Title = title, Summary = title });
        for (var lessonOrder = 1; lessonOrder <= 2; lessonOrder++)
        {
            var topic = lessonOrder == 1 ? chapterTopic.LessonOne : chapterTopic.LessonTwo;
            var lesson = await _db.StudyCourseLessons.AsNoTracking().FirstOrDefaultAsync(x => x.CourseContentId == chapter.Id && x.Order == lessonOrder, token);
            var frSlug = $"{definition.FrSlug}-module-{chapterOrder}-lecon-{lessonOrder}";
            if (lesson is null) { lesson = new CourseLesson { Id = StableId($"lesson:{chapter.Id}:{lessonOrder}"), CourseContentId = chapter.Id, Title = topic.Fr, Slug = frSlug, Summary = topic.FrSummary, Description = topic.FrContent, DurationMinutes = 8 + lessonOrder * 3, Order = lessonOrder, Status = StudyStatus.Published, CreatedOnUtc = DateTime.UtcNow }; _db.StudyCourseLessons.Add(lesson); }
            foreach (var (lang, title, slug, summary, html) in new[] { ("fr", topic.Fr, frSlug, topic.FrSummary, topic.FrContent), ("en", topic.En, $"{definition.EnSlug}-module-{chapterOrder}-lesson-{lessonOrder}", topic.EnSummary, topic.EnContent) })
                if (!await _db.StudyCourseLessonTranslations.AnyAsync(x => x.CourseLessonId == lesson.Id && x.LanguageCode == lang, token)) _db.StudyCourseLessonTranslations.Add(new CourseLessonTranslation { Id = StableId($"lesson-translation:{lesson.Id}:{lang}"), CourseLessonId = lesson.Id, LanguageCode = lang, PublicationStatus = StudyStatus.Published, Title = title, Slug = slug, Summary = summary, ContentHtml = html });
        }
    }
    private async Task QuizAsync(Course course, int chapterOrder, CancellationToken token)
    {
        var content = await _db.StudyCourseContents.AsNoTracking().FirstOrDefaultAsync(x => x.CourseId == course.Id && x.Order == chapterOrder, token);
        if (content is null || await _db.StudyCourseQuizzes.AnyAsync(x => x.CourseContentId == content.Id, token)) return;
        var quizId = StableId($"quiz:{course.Id}:{chapterOrder}");
        var quiz = new CourseQuiz { Id = quizId, CourseContentId = content.Id, Title = $"Révision : {course.Title} — {chapterOrder}", Slug = $"{course.Slug}-revision-{chapterOrder}", Summary = "Vérifiez les notions et les pratiques abordées dans le cours.", Order = 3, Status = StudyStatus.Published, CreatedOnUtc = DateTime.UtcNow };
        _db.StudyCourseQuizzes.Add(quiz);
        foreach (var (lang, title, slug, summary) in new[] { ("fr", $"Révision : {course.Title} — {chapterOrder}", $"{course.Slug}-revision-{chapterOrder}", "Vérifiez les notions et les pratiques abordées dans le cours."), ("en", $"Review: {course.Title} — {chapterOrder}", $"{course.Slug}-review-{chapterOrder}", "Check the concepts and practices covered in the course.") })
            _db.StudyCourseQuizTranslations.Add(new CourseQuizTranslation { Id = StableId($"quiz-translation:{quizId}:{lang}"), CourseQuizId = quizId, LanguageCode = lang, PublicationStatus = StudyStatus.Published, Title = title, Slug = slug, Summary = summary });
        var questions = new[]
        {
            ("Quel est le meilleur premier geste avant de modifier un projet ?", "What is the best first step before changing a project?", new[] { ("Comprendre la structure et le comportement actuel", "Understand the current structure and behavior", true), ("Supprimer les fichiers inconnus", "Delete unfamiliar files", false), ("Désactiver les validations", "Disable validation", false), ("Modifier plusieurs couches à la fois", "Change several layers at once", false) }),
            ("Quelle pratique aide à repérer une régression après un changement ?", "Which practice helps detect a regression after a change?", new[] { ("Vérifier un cas représentatif", "Check a representative case", true), ("Ignorer les erreurs", "Ignore errors", false), ("Retirer les contraintes", "Remove constraints", false), ("Masquer le résultat", "Hide the result", false) }),
            ("Que devrait contenir une modification facile à relire ?", "What should an easy-to-review change contain?", new[] { ("Un objectif clair et un périmètre cohérent", "A clear goal and a coherent scope", true), ("Des changements sans lien", "Unrelated changes", false), ("Des valeurs de test en production", "Test values in production", false), ("Des noms ambigus", "Ambiguous names", false) })
        };
        for (var i = 0; i < questions.Length; i++)
        {
            var q = questions[i]; var questionId = StableId($"quiz-question:{quizId}:{i + 1}");
            var question = new QuizQuestion { Id = questionId, CourseQuizId = quizId, Text = q.Item1, Order = i + 1, CreatedOnUtc = DateTime.UtcNow };
            _db.StudyQuizQuestions.Add(question);
            _db.StudyQuizQuestionTranslations.AddRange(new QuizQuestionTranslation { Id = StableId($"quiz-question-translation:{questionId}:fr"), QuizQuestionId = questionId, LanguageCode = "fr", PublicationStatus = StudyStatus.Published, Text = q.Item1 }, new QuizQuestionTranslation { Id = StableId($"quiz-question-translation:{questionId}:en"), QuizQuestionId = questionId, LanguageCode = "en", PublicationStatus = StudyStatus.Published, Text = q.Item2 });
            for (var j = 0; j < q.Item3.Length; j++)
            {
                var answerId = StableId($"quiz-answer:{questionId}:{j + 1}"); var answer = q.Item3[j];
                _db.StudyQuizAnswers.Add(new QuizAnswer { Id = answerId, QuizQuestionId = questionId, Text = answer.Item1, IsCorrect = answer.Item3, CreatedOnUtc = DateTime.UtcNow });
                _db.StudyQuizAnswerTranslations.AddRange(new QuizAnswerTranslation { Id = StableId($"quiz-answer-translation:{answerId}:fr"), QuizAnswerId = answerId, LanguageCode = "fr", PublicationStatus = StudyStatus.Published, Text = answer.Item1 }, new QuizAnswerTranslation { Id = StableId($"quiz-answer-translation:{answerId}:en"), QuizAnswerId = answerId, LanguageCode = "en", PublicationStatus = StudyStatus.Published, Text = answer.Item2 });
            }
        }
    }
    private sealed record CourseSeed(string Key, string Slug, string Category, string FrTitle, StudyLevel Level, string FrSlug, string FrSummary, string EnTitle, string EnSlug, string EnSummary);
    private sealed record LessonSeed(string Fr, string En, string FrSummary, string EnSummary, string FrContent, string EnContent);
    private sealed record ChapterSeed(string Fr, string En, LessonSeed LessonOne, LessonSeed LessonTwo);
    private static readonly CourseSeed[] Definitions = [
        new("html", "html-css-basics", "frontend", "HTML & CSS — Les bases", StudyLevel.Beginner, "html-css-bases", "Créez des pages accessibles et apprenez à les mettre en forme avec HTML sémantique et CSS responsive.", "HTML & CSS Fundamentals", "html-css-fundamentals", "Build accessible pages with semantic HTML and style them using responsive CSS."),
        new("javascript", "javascript-beginners", "frontend", "JavaScript pour débutants", StudyLevel.Beginner, "javascript-debutants", "Découvrez les variables, fonctions, conditions et événements pour rendre vos pages interactives.", "JavaScript for Beginners", "javascript-for-beginners", "Learn variables, functions, conditions, and events to make web pages interactive."),
        new("git", "git-github-developers", "devops", "Git & GitHub pour développeurs", StudyLevel.Beginner, "git-github-developpeurs", "Suivez l’évolution du code, organisez des branches et collaborez avec des pull requests.", "Git & GitHub for Developers", "git-github-for-developers", "Track code changes, organize branches, and collaborate through pull requests."),
        new("csharp", "csharp-fundamentals", "dotnet", "C# — Les fondamentaux", StudyLevel.Beginner, "csharp-fondamentaux", "Maîtrisez les types, méthodes, collections et objets pour construire des applications .NET.", "C# Fundamentals", "csharp-fundamentals", "Learn types, methods, collections, and objects to build .NET applications."),
        new("aspnet", "aspnet-core-mvc", "dotnet", "ASP.NET Core MVC", StudyLevel.Intermediate, "aspnet-core-mvc", "Construisez des applications web structurées avec ASP.NET Core MVC, Razor et des ViewModels.", "ASP.NET Core MVC", "aspnet-core-mvc", "Build structured web applications with ASP.NET Core MVC, Razor, and view models."),
        new("api", "rest-api-aspnet-core", "backend", "API REST avec ASP.NET Core", StudyLevel.Intermediate, "api-rest-aspnet-core", "Concevez des endpoints REST avec validation, réponses HTTP cohérentes et persistance.", "REST APIs with ASP.NET Core", "rest-api-aspnet-core", "Design REST endpoints with validation, consistent HTTP responses, and persistence."),
        new("efcore", "entity-framework-core", "dotnet", "Entity Framework Core", StudyLevel.Intermediate, "entity-framework-core", "Modélisez vos données et interrogez une base avec LINQ, migrations et suivi maîtrisé.", "Entity Framework Core", "entity-framework-core", "Model data and query a database with LINQ, migrations, and deliberate tracking."),
        new("sql", "sql-server-developers", "databases", "SQL Server pour développeurs", StudyLevel.Intermediate, "sql-server-developpeurs", "Écrivez des requêtes fiables et comprenez tables, jointures, contraintes et index.", "SQL Server for Developers", "sql-server-for-developers", "Write reliable queries and understand tables, joins, constraints, and indexes."),
        new("react", "react-introduction", "frontend", "React — Introduction", StudyLevel.Intermediate, "react-introduction", "Créez des interfaces composées avec composants, props, état local et rendu de listes.", "Introduction to React", "introduction-to-react", "Build composed interfaces with components, props, local state, and list rendering."),
        new("docker", "docker-developers", "devops", "Docker pour développeurs", StudyLevel.Intermediate, "docker-developpeurs", "Empaquetez une application dans une image et lancez des services avec Docker Compose.", "Docker for Developers", "docker-for-developers", "Package an application into an image and run services with Docker Compose.")
    ];

    private static readonly Dictionary<string, ChapterSeed[]> ChapterTopics = new()
    {
        ["html"] = Chapters(("Document HTML sémantique", "Semantic HTML Documents", "Structurer une page", "Structuring a page", "Les éléments sémantiques décrivent le rôle de chaque région. Utilisez header, nav, main et footer pour rendre la structure compréhensible aux navigateurs et technologies d’assistance.", "Semantic elements describe each region's role. Use header, nav, main, and footer to make structure understandable to browsers and assistive technology."), ("Sélecteurs et mise en page CSS", "CSS Selectors and Layout", "Cibler et organiser les éléments", "Targeting and arranging elements", "Les sélecteurs relient les règles CSS aux éléments. Flexbox organise une dimension; Grid répartit les éléments sur lignes et colonnes.", "Selectors connect CSS rules to elements. Flexbox arranges one dimension; Grid distributes items across rows and columns."), ("Responsive et accessibilité", "Responsive Design and Accessibility", "Adapter les pages aux écrans", "Adapting pages to screens", "Une mise en page fluide combine unités relatives, images flexibles et points de rupture choisis selon le contenu. Vérifiez aussi contraste, focus et ordre de lecture.", "A fluid layout combines relative units, flexible images, and content-driven breakpoints. Also check contrast, focus, and reading order.")),
        ["javascript"] = Chapters(("Valeurs, variables et expressions", "Values, Variables, and Expressions", "Manipuler des données", "Working with data", "Déclarez une valeur avec const lorsqu’elle ne sera pas réassignée, et let lorsqu’elle doit évoluer. Les expressions combinent valeurs et opérateurs pour produire un résultat.", "Use const for values that will not be reassigned and let for values that change. Expressions combine values and operators to produce a result."), ("Fonctions et tableaux", "Functions and Arrays", "Réutiliser la logique", "Reusing logic", "Une fonction nomme une tâche et peut recevoir des paramètres. Les tableaux conservent des collections ordonnées; map transforme chaque élément sans modifier le tableau d’origine.", "A function names a task and can receive parameters. Arrays hold ordered collections; map transforms each item without changing the original array."), ("DOM et événements", "The DOM and Events", "Réagir aux interactions", "Responding to interactions", "Le DOM expose le document sous forme d’objets. Ajoutez un écouteur avec addEventListener et mettez à jour le contenu de façon contrôlée après une action utilisateur.", "The DOM exposes the document as objects. Add a listener with addEventListener and update content deliberately after a user action.")),
        ["git"] = Chapters(("Dépôt et commits", "Repositories and Commits", "Enregistrer une évolution", "Recording a change", "Un dépôt contient l’historique du projet. Un commit enregistre un ensemble cohérent de changements; rédigez un message qui explique l’intention.", "A repository contains project history. A commit records a coherent set of changes; write a message that explains the intent."), ("Branches et intégration", "Branches and Integration", "Travailler en parallèle", "Working in parallel", "Une branche isole un travail sans modifier la branche principale. Fusionnez après vérification et résolvez les conflits en comparant l’intention des deux versions.", "A branch isolates work without changing the main line. Merge after review and resolve conflicts by comparing the intent of both versions."), ("Collaboration avec GitHub", "Collaborating with GitHub", "Partager et relire le code", "Sharing and reviewing code", "Une pull request décrit le changement et invite à la relecture. Gardez-la ciblée, liez le contexte utile et traitez les retours avant la fusion.", "A pull request describes a change and invites review. Keep it focused, provide context, and address feedback before merging.")),
        ["csharp"] = Chapters(("Types et variables", "Types and Variables", "Choisir des types adaptés", "Choosing suitable types", "C# est un langage à typage statique. Les types int, decimal, string et bool expriment la forme des données et permettent au compilateur de détecter des erreurs tôt.", "C# is statically typed. Types such as int, decimal, string, and bool express data shape and let the compiler catch mistakes early."), ("Conditions, boucles et méthodes", "Conditions, Loops, and Methods", "Organiser le traitement", "Organizing logic", "Les conditions sélectionnent un chemin, les boucles répètent une opération et les méthodes regroupent une tâche nommée. Préférez des méthodes courtes avec des paramètres explicites.", "Conditions choose a path, loops repeat work, and methods group a named task. Prefer short methods with explicit parameters."), ("Classes et collections", "Classes and Collections", "Modéliser des objets", "Modeling objects", "Une classe définit des données et comportements liés. Les collections génériques comme List<T> conservent des objets du même type et rendent les opérations prévisibles.", "A class defines related data and behavior. Generic collections such as List<T> hold values of one type and make operations predictable.")),
        ["aspnet"] = Chapters(("Démarrer une application MVC", "Starting an MVC Application", "Comprendre le démarrage", "Understanding startup", "ASP.NET Core construit un pipeline de middleware et enregistre les services nécessaires. La configuration sépare les options de l’application du code de traitement des requêtes.", "ASP.NET Core builds a middleware pipeline and registers required services. Configuration keeps application options separate from request handling."), ("Controllers et routing", "Controllers and Routing", "Relier une URL à une action", "Connecting a URL to an action", "Le routage associe une requête à une action de contrôleur. Une action valide les paramètres, appelle la logique applicative et choisit une réponse adaptée.", "Routing maps a request to a controller action. An action validates parameters, calls application logic, and chooses an appropriate response."), ("Razor et ViewModels", "Razor and View Models", "Présenter un modèle dédié", "Presenting a purpose-built model", "Une vue Razor rend un modèle de présentation. Un ViewModel limite les données transmises au navigateur et évite de lier directement une entité persistée au formulaire.", "A Razor view renders a presentation model. A view model limits data sent to the browser and avoids binding a persisted entity directly to a form.")),
        ["api"] = Chapters(("Principes REST et HTTP", "REST and HTTP Principles", "Concevoir des ressources", "Designing resources", "Une API REST expose des ressources identifiées par des URL. GET lit, POST crée, PUT remplace et DELETE supprime généralement une représentation selon le contrat défini.", "A REST API exposes resources identified by URLs. GET reads, POST creates, PUT replaces, and DELETE generally removes a representation according to the contract."), ("Endpoints et validation", "Endpoints and Validation", "Valider avant de persister", "Validate before persisting", "Les endpoints doivent vérifier les entrées et renvoyer des statuts HTTP cohérents. Une erreur de validation lisible aide le client à corriger sa requête sans exposer de détails internes.", "Endpoints should validate input and return consistent HTTP status codes. A clear validation error helps clients correct requests without exposing internals."), ("Persistance et sécurité", "Persistence and Security", "Protéger les opérations", "Protecting operations", "La persistance passe par une couche adaptée et des requêtes paramétrées. Authentifiez les clients, autorisez chaque opération et ne retournez que les champs nécessaires.", "Persistence belongs in an appropriate layer with parameterized queries. Authenticate clients, authorize each operation, and return only necessary fields.")),
        ["efcore"] = Chapters(("Entités et relations", "Entities and Relationships", "Décrire le modèle", "Describing the model", "Une entité représente une donnée persistée. Les clés et relations décrivent les liens; configurez les suppressions et contraintes en fonction des règles du domaine.", "An entity represents persisted data. Keys and relationships describe links; configure deletes and constraints to match domain rules."), ("Requêtes LINQ et suivi", "LINQ Queries and Tracking", "Lire sans charger trop", "Read without overloading", "Projetez les listes vers des modèles légers et utilisez AsNoTracking pour les lectures sans modification. Filtrez dans la requête avant matérialisation.", "Project lists into lightweight models and use AsNoTracking for read-only operations. Filter in the query before materialization."), ("Écritures et migrations", "Writes and Migrations", "Faire évoluer le schéma", "Evolving the schema", "SaveChanges regroupe les modifications suivies. Les migrations décrivent l’évolution du modèle; relisez-les et appliquez-les dans un processus contrôlé.", "SaveChanges persists tracked modifications. Migrations describe model evolution; review them and apply them through a controlled process.")),
        ["sql"] = Chapters(("Tables et clés", "Tables and Keys", "Structurer les données", "Structuring data", "Une table regroupe des lignes de même nature. Une clé primaire identifie chaque ligne et une clé étrangère exprime une relation vérifiable.", "A table groups rows of the same kind. A primary key identifies each row and a foreign key expresses a verifiable relationship."), ("Requêtes et jointures", "Queries and Joins", "Combiner des résultats", "Combining results", "WHERE filtre les lignes avant le résultat; ORDER BY définit leur ordre. Une jointure combine les lignes reliées par une condition explicite.", "WHERE filters rows and ORDER BY defines their order. A join combines rows connected by an explicit condition."), ("Contraintes et index", "Constraints and Indexes", "Protéger et accélérer", "Protecting and improving access", "Les contraintes empêchent des valeurs invalides. Un index peut accélérer une lecture fréquente, mais augmente le coût des écritures et doit répondre à un besoin mesuré.", "Constraints prevent invalid values. An index can speed up a frequent read, but adds write cost and should address a measured need.")),
        ["react"] = Chapters(("Composants et props", "Components and Props", "Composer une interface", "Composing an interface", "Un composant React décrit une partie d’interface à partir de ses props. Décomposez l’écran en éléments réutilisables dont les responsabilités restent faciles à comprendre.", "A React component describes part of an interface from its props. Split screens into reusable pieces with clear responsibilities."), ("État et événements", "State and Events", "Gérer les interactions", "Managing interactions", "L’état local conserve une valeur qui change pendant l’utilisation. Une mise à jour d’état déclenche un nouveau rendu; ne modifiez pas directement l’objet existant.", "Local state stores a value that changes during use. Updating state triggers a render; do not mutate the existing object directly."), ("Listes et formulaires", "Lists and Forms", "Rendre des collections", "Rendering collections", "Rendez une collection avec map et fournissez une clé stable à chaque élément. Les champs contrôlés reçoivent leur valeur depuis l’état et signalent les changements par événements.", "Render a collection with map and provide a stable key for each item. Controlled fields receive their value from state and report changes through events.")),
        ["docker"] = Chapters(("Images et conteneurs", "Images and Containers", "Isoler une application", "Isolating an application", "Une image est un modèle immuable et un conteneur en est une instance en exécution. Un Dockerfile décrit les étapes nécessaires pour construire l’image.", "An image is an immutable template and a container is a running instance. A Dockerfile describes the steps needed to build the image."), ("Construire une image efficace", "Building an Efficient Image", "Réduire le contexte", "Reducing the build context", "Un .dockerignore exclut les fichiers inutiles. Les étapes ordonnées et les couches réutilisables accélèrent les constructions; ne copiez pas de secrets dans l’image.", "A .dockerignore excludes unnecessary files. Ordered steps and reusable layers speed builds; never copy secrets into an image."), ("Services avec Compose", "Services with Compose", "Décrire plusieurs services", "Describing multiple services", "Compose décrit les services, réseaux et volumes d’un environnement local. Les variables de configuration et volumes permettent de séparer le code de l’état local.", "Compose describes services, networks, and volumes for a local environment. Configuration variables and volumes separate code from local state."))
    };

    private static ChapterSeed[] Chapters((string Fr, string En, string FrLesson, string EnLesson, string FrText, string EnText) a, (string Fr, string En, string FrLesson, string EnLesson, string FrText, string EnText) b, (string Fr, string En, string FrLesson, string EnLesson, string FrText, string EnText) c) => [Make(a), Make(b), Make(c)];
    private static ChapterSeed Make((string Fr, string En, string FrLesson, string EnLesson, string FrText, string EnText) x) => new(x.Fr, x.En, Lesson(x.FrLesson, x.EnLesson, x.FrText, x.EnText), Lesson($"Exercices : {x.FrLesson}", $"Practice: {x.EnLesson}", $"Appliquez « {x.FrLesson} » dans un exemple court, puis vérifiez le résultat et les cas limites.", $"Apply “{x.EnLesson}” in a short example, then verify the result and edge cases."));
    private static LessonSeed Lesson(string fr, string en, string frText, string enText) => new(fr, en, frText, enText, $"<h2>{fr}</h2><p>{frText}</p><p>Commencez par un exemple minimal, observez le résultat, puis modifiez une seule chose à la fois. Cette méthode aide à distinguer la règle du comportement accidentel.</p><h3>À retenir</h3><ul><li>Choisissez des noms explicites.</li><li>Vérifiez le résultat avec un cas simple.</li></ul>", $"<h2>{en}</h2><p>{enText}</p><p>Start with a minimal example, observe the result, then change one thing at a time. This helps distinguish the intended rule from accidental behavior.</p><h3>Key points</h3><ul><li>Choose explicit names.</li><li>Verify the result with a simple case.</li></ul>");
}

