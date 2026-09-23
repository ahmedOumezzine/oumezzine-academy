using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Application.Admin.Catalog;
using OumezzineAcademy.Application.UseCases;
using OumezzineAcademy.Infrastructure;
using OumezzineAcademy.Infrastructure.Data;
using OumezzineAcademy.Infrastructure.Sanitization;
using OumezzineAcademy.Web.Services;
using OumezzineAcademy.Web.Services.Caching;
using System.IO.Compression;
using System.Security.Cryptography;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddInfrastructure(Path.Combine(builder.Environment.WebRootPath ?? builder.Environment.ContentRootPath, "wwwroot"));

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (builder.Environment.IsEnvironment("Testing")) options.UseSqlite(connectionString);
    else options.UseSqlServer(connectionString, sql => sql.MigrationsAssembly(typeof(OumezzineAcademy.Infrastructure.Data.ApplicationDbContext).Assembly.FullName).MigrationsHistoryTable("__EFMigrationsHistory_OumezzineAcademy"));
});
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    // The shared development database has no email-confirmation workflow.
    // Admin access remains protected by the Admin role on the controllers.
    options.SignIn.RequireConfirmedAccount = false;
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(2);
    options.Lockout.MaxFailedAccessAttempts = 3;
})
.AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/admin/auth/login";
    options.AccessDeniedPath = "/admin/auth/access-denied";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

builder.Services.AddControllersWithViews();
builder.Services.AddLocalization();
builder.Services.AddScoped<ICurrentLanguageService, CurrentLanguageService>();
builder.Services.AddScoped<OumezzineAcademy.Application.Abstractions.ICurrentLanguage>(sp => sp.GetRequiredService<ICurrentLanguageService>());
builder.Services.AddScoped<ILocalizedUrlService>(sp => new LocalizedUrlService(sp.GetRequiredService<OumezzineAcademy.Application.Abstractions.ILocalizedSlugQueries>()));
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture("fr");
    options.SupportedCultures = [new System.Globalization.CultureInfo("fr"), new System.Globalization.CultureInfo("en")];
    options.SupportedUICultures = [new System.Globalization.CultureInfo("fr"), new System.Globalization.CultureInfo("en")];
    options.RequestCultureProviders.Insert(0, new RouteLanguageRequestCultureProvider());
});
builder.Services.AddRazorPages().AddRazorRuntimeCompilation();
builder.Services.AddOutputCache(options =>
{
    options.AddPolicy("PublicCatalog", policy => policy.Expire(TimeSpan.FromMinutes(10)).Tag(StudyLmsCacheKeys.PublicTag).SetVaryByQuery("*"));
});
builder.Services.AddSingleton<IStudyLmsCacheInvalidator, StudyLmsCacheInvalidator>();
builder.Services.AddScoped<IHtmlSanitizer, HtmlSanitizerService>();
builder.Services.AddScoped<ICourseCatalogService, CourseCatalogService>();
builder.Services.AddScoped<ILearningPathCatalogService, LearningPathCatalogService>();
builder.Services.AddScoped<IQuizService, QuizService>();
builder.Services.AddScoped<OumezzineAcademy.Application.Abstractions.IQuizSubmissionHandler, QuizSubmissionHandler>();
builder.Services.AddScoped<AdminCategoryService>();
builder.Services.AddScoped<AdminCourseService>();
builder.Services.AddScoped<AdminChapterService>();
builder.Services.AddScoped<AdminLessonService>();
builder.Services.AddScoped<AdminQuizService>();
builder.Services.AddScoped<AdminQuestionService>();
builder.Services.AddScoped<AdminLearningPathService>();
builder.Services.AddScoped<AdminLearningPathCategoryService>();
builder.Services.AddScoped<CoursePrerequisiteValidationService>();

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "text/css", "application/javascript", "image/svg+xml" });
});
builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Optimal;
});

builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
    options.Preload = true;
});

var app = builder.Build();

//}

if (app.Environment.IsDevelopment())
    //{
    //    using var seedScope = app.Services.CreateScope();
    //    await seedScope.ServiceProvider.GetRequiredService<IStudyLmsSeeder>().SeedAsync();
    //}

    if (!app.Environment.IsEnvironment("DesignTime"))
    {
        using (var scope = app.Services.CreateScope())
        {
            var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            if (!await roles.RoleExistsAsync("Admin"))
                await roles.CreateAsync(new IdentityRole("Admin"));

            var email = app.Configuration["Admin:Email"] ?? Environment.GetEnvironmentVariable("LEARNWEBAPP_ADMIN_EMAIL");
            var password = app.Configuration["Admin:Password"] ?? Environment.GetEnvironmentVariable("LEARNWEBAPP_ADMIN_PASSWORD");
            if (!string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(password))
            {
                var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
                var user = await users.FindByEmailAsync(email);
                if (user == null)
                {
                    user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
                    var created = await users.CreateAsync(user, password);
                    if (!created.Succeeded) throw new InvalidOperationException(string.Join("; ", created.Errors.Select(error => error.Description)));
                }
                if (!await users.IsInRoleAsync(user, "Admin")) await users.AddToRoleAsync(user, "Admin");
            }
        }
    }

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseResponseCompression();

app.Use(async (context, next) =>
{
    var cspNonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
    context.Items["CspNonce"] = cspNonce;
    context.Response.Headers["Content-Security-Policy"] =
        "default-src 'self'; " +
        $"script-src 'self' 'nonce-{cspNonce}' https://cdn.jsdelivr.net; " +
        "style-src 'self' https://fonts.googleapis.com https://cdn.jsdelivr.net https://unpkg.com; " +
        "img-src 'self' https:; " +
        "media-src 'self' https:; " +
        "connect-src 'self'; " +
        "font-src 'self' https://fonts.gstatic.com https://unpkg.com https://cdn.jsdelivr.net; " +
        "object-src 'none'; form-action 'self'; " +
        "base-uri 'self'; frame-ancestors 'none';";
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    await next();
});

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        var headers = ctx.Context.Response.GetTypedHeaders();
        var path = ctx.File.PhysicalPath;
        if (path is null)
        {
            return;
        }

        if (path.EndsWith(".css", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".js", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".webp", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".svg", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".ico", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".woff", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".woff2", StringComparison.OrdinalIgnoreCase))
        {
            headers.CacheControl = new Microsoft.Net.Http.Headers.CacheControlHeaderValue
            {
                Public = true,
                MaxAge = TimeSpan.FromDays(365)
            };
            headers.Expires = DateTimeOffset.UtcNow.AddYears(1);
        }
    }
});

app.UseRouting();
app.UseRequestLocalization();
app.UseAuthentication();
app.UseAuthorization();
app.UseOutputCache();

app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value?.TrimEnd('/');
    if (string.IsNullOrEmpty(path))
    {
        context.Response.Redirect("/fr/", permanent: true);
        return;
    }

    var redirects = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["/courses"] = "/fr/cours",
        ["/categories"] = "/fr/categories",
        ["/paths"] = "/fr/parcours",
        ["/Home/About"] = "/fr/a-propos"
    };
    if (path != null && redirects.TryGetValue(path, out var target))
    {
        context.Response.Redirect(target + context.Request.QueryString, permanent: true);
        return;
    }

    if (path != null)
    {
        var legacyDetails = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["/courses/"] = "/fr/cours/",
            ["/categories/"] = "/fr/categories/",
            ["/paths/"] = "/fr/parcours/",
            ["/lessons/"] = "/fr/lecons/",
            ["/quiz/"] = "/fr/quiz/"
        };

        foreach (var (legacyPrefix, localizedPrefix) in legacyDetails)
        {
            if (path.StartsWith(legacyPrefix, StringComparison.OrdinalIgnoreCase) && path.Length > legacyPrefix.Length)
            {
                context.Response.Redirect(localizedPrefix + path[legacyPrefix.Length..] + context.Request.QueryString, permanent: true);
                return;
            }
        }
    }
    await next();
});

app.MapControllerRoute("admin", "admin/{controller=Dashboard}/{action=Index}/{id?}", new { area = "Admin" });
app.MapControllerRoute("fr-home", "fr/", new { controller = "Home", action = "Index", culture = "fr" });
app.MapControllerRoute("en-home", "en/", new { controller = "Home", action = "Index", culture = "en" });
app.MapControllerRoute("fr-course-detail", "fr/cours/{slug}", new { controller = "Course", action = "Details", culture = "fr" });
app.MapControllerRoute("en-course-detail", "en/courses/{slug}", new { controller = "Course", action = "Details", culture = "en" });
app.MapControllerRoute("fr-courses", "fr/cours", new { controller = "Course", action = "Index", culture = "fr" });
app.MapControllerRoute("en-courses", "en/courses", new { controller = "Course", action = "Index", culture = "en" });
app.MapControllerRoute("fr-category-detail", "fr/categories/{slug}", new { controller = "Category", action = "Details", culture = "fr" });
app.MapControllerRoute("en-category-detail", "en/categories/{slug}", new { controller = "Category", action = "Details", culture = "en" });
app.MapControllerRoute("fr-categories", "fr/categories", new { controller = "Category", action = "Index", culture = "fr" });
app.MapControllerRoute("en-categories", "en/categories", new { controller = "Category", action = "Index", culture = "en" });
app.MapControllerRoute("fr-lesson-detail", "fr/lecons/{slug}", new { controller = "Lesson", action = "Details", culture = "fr" });
app.MapControllerRoute("en-lesson-detail", "en/lessons/{slug}", new { controller = "Lesson", action = "Details", culture = "en" });
app.MapControllerRoute("fr-path-detail", "fr/parcours/{slug}", new { controller = "LearningPath", action = "Details", culture = "fr" });
app.MapControllerRoute("en-path-detail", "en/learning-paths/{slug}", new { controller = "LearningPath", action = "Details", culture = "en" });
app.MapControllerRoute("fr-paths", "fr/parcours", new { controller = "LearningPath", action = "Index", culture = "fr" });
app.MapControllerRoute("en-paths", "en/learning-paths", new { controller = "LearningPath", action = "Index", culture = "en" });
app.MapControllerRoute("fr-quiz-submit", "fr/quiz/submit", new { controller = "Quiz", action = "Submit", culture = "fr" });
app.MapControllerRoute("en-quiz-submit", "en/quiz/submit", new { controller = "Quiz", action = "Submit", culture = "en" });
app.MapControllerRoute("fr-quiz", "fr/quiz/{slug}", new { controller = "Quiz", action = "Attempt", culture = "fr" });
app.MapControllerRoute("en-quiz", "en/quiz/{slug}", new { controller = "Quiz", action = "Attempt", culture = "en" });
app.MapControllerRoute("fr-about", "fr/a-propos", new { controller = "Home", action = "About", culture = "fr" });
app.MapControllerRoute("en-about", "en/about", new { controller = "Home", action = "About", culture = "en" });
app.MapControllerRoute("course-detail", "courses/{slug}", new { controller = "Course", action = "Details" });
app.MapControllerRoute("courses", "courses", new { controller = "Course", action = "Index" });
app.MapControllerRoute("category-detail", "categories/{slug}", new { controller = "Category", action = "Details" });
app.MapControllerRoute("categories", "categories", new { controller = "Category", action = "Index" });
app.MapControllerRoute("lesson-detail", "lessons/{slug}", new { controller = "Lesson", action = "Details" });
app.MapControllerRoute("path-detail", "paths/{slug}", new { controller = "LearningPath", action = "Details" });
app.MapControllerRoute("paths", "paths", new { controller = "LearningPath", action = "Index" });
app.MapControllerRoute("quiz", "quiz/{slug}", new { controller = "Quiz", action = "Attempt" });
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();
