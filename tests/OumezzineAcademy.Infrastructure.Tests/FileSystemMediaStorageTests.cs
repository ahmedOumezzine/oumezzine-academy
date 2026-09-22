using Xunit;
using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Infrastructure.Media;

namespace OumezzineAcademy.Tests;

public sealed class FileSystemMediaStorageTests
{
    [Fact]
    public async Task Saves_valid_png_and_returns_safe_public_path()
    {
        var root = Directory.CreateTempSubdirectory().FullName;
        try
        {
            var id = Guid.NewGuid();
            var storage = new FileSystemMediaStorage(root);
            await using var stream = new MemoryStream([0x89, 0x50, 0x4E, 0x47, 0, 0, 0, 0, 0, 0, 0, 0, 1]);
            var path = await storage.SaveImageAsync(new("cover.PNG", "image/png", stream.Length, stream), "courses", id);

            Assert.StartsWith($"/uploads/courses/{id:D}/", path);
            Assert.True(storage.IsSafeImagePath(path, "courses", id));
            Assert.True(File.Exists(Path.Combine(root, path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar))));
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public async Task Rejects_invalid_signature_and_traversal()
    {
        var root = Directory.CreateTempSubdirectory().FullName;
        try
        {
            var storage = new FileSystemMediaStorage(root);
            await using var stream = new MemoryStream([1, 2, 3, 4, 5]);
            await Assert.ThrowsAsync<InvalidDataException>(() => storage.SaveImageAsync(new("x.png", "image/png", stream.Length, stream), "courses", Guid.NewGuid()));
            Assert.False(storage.IsSafeImagePath("/uploads/other/file.png", "courses", Guid.NewGuid()));
            Assert.False(storage.IsSafeImagePath("/uploads/courses/../other/file.png", "courses", Guid.NewGuid()));
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public void Deletes_only_safe_existing_file()
    {
        var root = Directory.CreateTempSubdirectory().FullName;
        try
        {
            var id = Guid.NewGuid();
            var folder = Path.Combine(root, "uploads", "lessons", id.ToString("D"));
            Directory.CreateDirectory(folder);
            var file = Path.Combine(folder, "x.jpg");
            File.WriteAllBytes(file, [1]);
            var storage = new FileSystemMediaStorage(root);
            storage.DeleteIfSafe($"/uploads/lessons/{id:D}/x.jpg", "lessons", id);
            Assert.False(File.Exists(file));
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public async Task Rejects_unsupported_area_and_oversized_upload()
    {
        var root = Directory.CreateTempSubdirectory().FullName;
        try
        {
            var storage = new FileSystemMediaStorage(root);
            await using var stream = new MemoryStream([0x89, 0x50, 0x4E, 0x47, 0, 0, 0, 0]);
            await Assert.ThrowsAsync<ArgumentException>(() => storage.SaveImageAsync(new("x.png", "image/png", stream.Length, stream), "avatars", Guid.NewGuid()));
            await Assert.ThrowsAsync<InvalidDataException>(() => storage.SaveImageAsync(new("x.png", "image/png", 5 * 1024 * 1024 + 1, stream), "courses", Guid.NewGuid()));
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public void Invalid_delete_path_is_ignored()
    {
        var root = Directory.CreateTempSubdirectory().FullName;
        try { new FileSystemMediaStorage(root).DeleteIfSafe("/uploads/courses/other/file.png", "courses", Guid.NewGuid()); }
        finally { Directory.Delete(root, true); }
    }
}
