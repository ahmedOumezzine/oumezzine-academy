using AhmedOumezzine.EFCore.Repository.Extensions;
using AhmedOumezzine.EFCore.Repository.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OumezzineAcademy.Infrastructure.Persistence;
using OumezzineAcademy.Web.Services;
using System.Globalization;
using Xunit;

namespace OumezzineAcademy.Tests;

public sealed class CurrentLanguageServiceTests
{
    private static IRepository CreateRepository(OumezzineAcademy.Infrastructure.Data.ApplicationDbContext db)
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => db);
        services.AddGenericRepository<OumezzineAcademy.Infrastructure.Data.ApplicationDbContext>();
        return services.BuildServiceProvider().GetRequiredService<IRepository>();
    }

    [Theory]
    [InlineData("fr-FR", "fr")]
    [InlineData("en-US", "en")]
    [InlineData("nl-NL", "fr")]
    public void LanguageCode_UsesOnlySupportedLanguages(string cultureName, string expected)
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);
            Assert.Equal(expected, new CurrentLanguageService().LanguageCode);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Theory]
    [InlineData("fr-FR", "/fr/", "/fr/cours", "/fr/categories", "/fr/parcours", "/fr/a-propos")]
    [InlineData("en-US", "/en/", "/en/courses", "/en/categories", "/en/learning-paths", "/en/about")]
    public void PublicNavigationUsesTheCurrentLanguage(string cultureName, string home, string courses, string categories, string paths, string about)
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);
            using var db = new OumezzineAcademy.Infrastructure.Data.ApplicationDbContext(new DbContextOptionsBuilder<OumezzineAcademy.Infrastructure.Data.ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
            var navigation = new LocalizedUrlService(new EfLocalizedSlugQueries(CreateRepository(db))).GetPublicNavigation();
            Assert.Equal(home, navigation.Home);
            Assert.Equal(courses, navigation.Courses);
            Assert.Equal(categories, navigation.Categories);
            Assert.Equal(paths, navigation.LearningPaths);
            Assert.Equal(about, navigation.About);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }
}
