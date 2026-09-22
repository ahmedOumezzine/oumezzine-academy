using Xunit;
using System.Globalization;
using OumezzineAcademy.Domain.Catalog;
using OumezzineAcademy.Web.Services;

namespace OumezzineAcademy.Tests;

public sealed class PolicyAndLanguageTests
{
    [Theory]
    [InlineData("fr", "fr")]
    [InlineData("en-US", "en")]
    [InlineData("de", "fr")]
    public void Normalizes_supported_and_unknown_languages(string culture, string expected)
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
            Assert.Equal(expected, new CurrentLanguageService().LanguageCode);
        }
        finally { CultureInfo.CurrentUICulture = previous; }
    }

    [Fact]
    public void Published_course_and_translation_are_visible()
    {
        var result = CourseVisibilityPolicy.Evaluate(StudyStatus.Published, StudyStatus.Published, "en");
        Assert.True(result.IsVisible);
        Assert.Empty(result.Reasons);
    }

    [Theory]
    [InlineData(StudyStatus.Draft, null)]
    [InlineData(StudyStatus.Archived, StudyStatus.Draft)]
    [InlineData(StudyStatus.Published, StudyStatus.Archived)]
    public void Unpublished_statuses_return_explanations(StudyStatus course, StudyStatus? translation)
    {
        var result = CourseVisibilityPolicy.Evaluate(course, translation, "en");
        Assert.False(result.IsVisible);
        Assert.NotEmpty(result.Reasons);
    }
}
