using System.Xml.Linq;
using Xunit;

namespace OumezzineAcademy.Tests;

public sealed class ResourceConsistencyTests
{
    [Fact]
    public void FrenchAndEnglishSharedResourcesHaveTheSameKeys()
    {
        var resources = TestProjectFiles.FindOumezzineAcademyFile("Resources");
        var french = Keys(Path.Combine(resources, "SharedResource.fr.resx"));
        var english = Keys(Path.Combine(resources, "SharedResource.en.resx"));

        Assert.Empty(french.Except(english));
        Assert.Empty(english.Except(french));
        Assert.All(XDocument.Load(Path.Combine(resources, "SharedResource.fr.resx")).Descendants("data"), item =>
            Assert.False(string.IsNullOrWhiteSpace((string?)item.Attribute("name")) || string.IsNullOrWhiteSpace(item.Element("value")?.Value)));
        Assert.All(XDocument.Load(Path.Combine(resources, "SharedResource.en.resx")).Descendants("data"), item =>
            Assert.False(string.IsNullOrWhiteSpace((string?)item.Attribute("name")) || string.IsNullOrWhiteSpace(item.Element("value")?.Value)));
    }

    private static HashSet<string> Keys(string path) => XDocument.Load(path).Descendants("data").Select(x => (string?)x.Attribute("name")).Where(x => x is not null).Cast<string>().ToHashSet(StringComparer.Ordinal);

    [Fact]
    public void FrenchAndEnglishSharedResourcesHaveNoDuplicateKeys()
    {
        var resources = TestProjectFiles.FindOumezzineAcademyFile("Resources");
        AssertNoDuplicateKeys(Path.Combine(resources, "SharedResource.fr.resx"));
        AssertNoDuplicateKeys(Path.Combine(resources, "SharedResource.en.resx"));
    }

    private static void AssertNoDuplicateKeys(string path)
    {
        var keys = XDocument.Load(path).Descendants("data").Select(x => (string?)x.Attribute("name")).Where(x => x is not null).Cast<string>().ToArray();
        Assert.Equal(keys.Length, keys.Distinct(StringComparer.Ordinal).Count());
    }
}

