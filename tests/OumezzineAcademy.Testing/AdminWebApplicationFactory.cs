using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Domain.Catalog;
using OumezzineAcademy.Infrastructure.Data;
using OumezzineAcademy.Infrastructure.Media;
using System.Security.Claims;

namespace OumezzineAcademy.Tests;

public enum AdminTestProfile
{ Anonymous, User, Admin }

public sealed class AdminWebApplicationFactory : WebApplicationFactory<Program>
{
    public static readonly Guid CourseId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    public static readonly Guid ChapterId = Guid.Parse("20000000-0000-0000-0000-000000000001");
    public static readonly Guid QuizId = Guid.Parse("30000000-0000-0000-0000-000000000001");
    private readonly AdminTestProfile _profile;
    private readonly string? _mediaRootPath;
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public AdminWebApplicationFactory(AdminTestProfile profile, string? mediaRootPath = null)
    {
        _profile = profile;
        _mediaRootPath = mediaRootPath;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\MSSQLLocalDB;Database=OumezzineAcademyTests;Trusted_Connection=True;"
            }));
        builder.ConfigureServices(services =>
        {
            _connection.Open();
            services.AddDataProtection().UseEphemeralDataProtectionProvider();
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(_connection));
            if (_mediaRootPath is not null)
            {
                services.RemoveAll<IMediaStorage>();
                services.AddSingleton<IMediaStorage>(new FileSystemMediaStorage(_mediaRootPath));
            }
            using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_connection).Options);
            db.Database.EnsureCreated();
            SeedFixture(db);

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.TestScheme;
                options.DefaultChallengeScheme = TestAuthHandler.TestScheme;
                options.DefaultForbidScheme = TestAuthHandler.TestScheme;
            }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.TestScheme, options => options.ClaimsIssuer = _profile.ToString());
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _connection.Dispose();
        base.Dispose(disposing);
    }

    private static void SeedFixture(ApplicationDbContext db)
    {
        var category = new CourseCategory
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            CreatedOnUtc = DateTime.UtcNow,
            Title = "Test category",
            Slug = "test-category",
            Status = StudyStatus.Published
        };
        var course = new Course
        {
            Id = CourseId,
            CreatedOnUtc = DateTime.UtcNow,
            Title = "Test course",
            Slug = "test-course",
            CourseCategoryId = category.Id,
            Level = StudyLevel.Beginner,
            Status = StudyStatus.Published
        };
        var chapter = new CourseContent
        {
            Id = ChapterId,
            CreatedOnUtc = DateTime.UtcNow,
            Title = "Test chapter",
            Slug = "test-chapter",
            CourseId = CourseId,
            Order = 1,
            Status = StudyStatus.Published
        };
        var quiz = new CourseQuiz
        {
            Id = QuizId,
            CreatedOnUtc = DateTime.UtcNow,
            Title = "Test quiz",
            Slug = "test-quiz",
            CourseContentId = ChapterId,
            Order = 1,
            Status = StudyStatus.Published
        };
        var question = new QuizQuestion
        {
            Id = Guid.Parse("40000000-0000-0000-0000-000000000001"),
            CreatedOnUtc = DateTime.UtcNow,
            Text = "Test question",
            Order = 1,
            CourseQuizId = QuizId
        };
        db.AddRange(category, course, chapter, quiz, question);
        db.Add(new CourseLesson
        {
            Id = Guid.Parse("50000000-0000-0000-0000-000000000001"),
            CreatedOnUtc = DateTime.UtcNow,
            Title = "Test lesson",
            Slug = "test-lesson",
            CourseContentId = ChapterId,
            Order = 1,
            Status = StudyStatus.Published
        });
        db.Add(new QuizAnswer
        {
            Id = Guid.Parse("60000000-0000-0000-0000-000000000001"),
            CreatedOnUtc = DateTime.UtcNow,
            Text = "Test answer",
            IsCorrect = true,
            QuizQuestionId = question.Id
        });
        db.SaveChanges();
    }
}

public sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string TestScheme = "AdminTest";

    public TestAuthHandler(Microsoft.Extensions.Options.IOptionsMonitor<AuthenticationSchemeOptions> options, Microsoft.Extensions.Logging.ILoggerFactory logger, System.Text.Encodings.Web.UrlEncoder encoder) : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (Options.ClaimsIssuer == AdminTestProfile.Anonymous.ToString()) return Task.FromResult(AuthenticateResult.NoResult());
        var profile = Options.ClaimsIssuer ?? AdminTestProfile.User.ToString();
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, profile), new(ClaimTypes.Name, profile) };
        if (profile == AdminTestProfile.Admin.ToString()) claims.Add(new(ClaimTypes.Role, "Admin"));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(claims, TestScheme)), TestScheme)));
    }
}
