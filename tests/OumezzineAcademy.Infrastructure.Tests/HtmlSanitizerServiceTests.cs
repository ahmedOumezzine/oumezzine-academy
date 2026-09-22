using OumezzineAcademy.Infrastructure.Sanitization;
using Xunit;

namespace OumezzineAcademy.Tests;

public sealed class HtmlSanitizerServiceTests
{
    [Fact]
    public void Removes_scripts_and_keeps_allowed_markup()
    {
        var result = new HtmlSanitizerService().Sanitize("<p>Hello</p><script>alert(1)</script><a href=\"https://example.com\">link</a>");
        Assert.Contains("<p>Hello</p>", result);
        Assert.Contains("href=\"https://example.com\"", result);
        Assert.DoesNotContain("script", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Rejects_unsafe_uri_schemes()
    {
        var result = new HtmlSanitizerService().Sanitize("<a href=\"javascript:alert(1)\">bad</a>");
        Assert.DoesNotContain("javascript:", result, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Preserves_null_or_whitespace(string? value)
        => Assert.Equal(value, new HtmlSanitizerService().Sanitize(value));
}