namespace OumezzineAcademy.Tests;

public static class TestProjectFiles
{
    public static string FindOumezzineAcademyFile(string relativePath)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidates = new[] { Path.Combine(current.FullName, "src", "OumezzineAcademy.Web"), Path.Combine(current.FullName, "src", "OumezzineAcademy"), Path.Combine(current.FullName, "Apps", "OumezzineAcademy"), Path.Combine(current.FullName, "OumezzineAcademy") };
            foreach (var candidate in candidates)
            {
                var path = Path.Combine(candidate, relativePath.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(path) || Directory.Exists(path)) return path;
            }
            current = current.Parent;
        }
        throw new FileNotFoundException($"Could not locate Oumezzine Academy source file '{relativePath}'.");
    }
}