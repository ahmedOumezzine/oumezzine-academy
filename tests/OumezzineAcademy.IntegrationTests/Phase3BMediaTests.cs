using Microsoft.AspNetCore.Http;
using OumezzineAcademy.Application.Abstractions;
using OumezzineAcademy.Infrastructure.Media;
using OumezzineAcademy.Infrastructure.Sanitization;
using Xunit;

namespace OumezzineAcademy.Tests;

public sealed class Phase3BMediaTests
{
    [Fact]
    public void Sanitizer_keeps_learning_html_and_removes_active_content()
    {
        var sanitizer = new HtmlSanitizerService();

        var result = sanitizer.Sanitize("<h2>Title</h2><h4>Subtitle</h4><p onclick=\"alert(1)\"><strong>Safe</strong><u>underlined</u><img src=\"/uploads/lessons/a/image.png\" alt=\"Diagram\" onerror=\"alert(1)\"></p><table><thead><tr><th>Header</th></tr></thead><tbody><tr><td>Cell</td></tr></tbody></table><script>alert(1)</script><a href=\"javascript:alert(1)\">bad</a>");

        Assert.Contains("<h2>Title</h2>", result);
        Assert.Contains("<strong>Safe</strong>", result);
        Assert.Contains("<h4>Subtitle</h4>", result);
        Assert.Contains("<u>underlined</u>", result);
        Assert.Contains("<table>", result);
        Assert.Contains("<th>Header</th>", result);
        Assert.Contains("src=\"/uploads/lessons/a/image.png\"", result);
        Assert.Contains("alt=\"Diagram\"", result);
        Assert.DoesNotContain("script", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onclick", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onerror", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("javascript:", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Storage_accepts_signed_images_and_scopes_them_to_entity()
    {
        var root = Path.Combine(Path.GetTempPath(), "OumezzineAcademy-Phase3B", Guid.NewGuid().ToString("N"));
        try
        {
            var storage = new FileSystemMediaStorage(root);
            var entityId = Guid.NewGuid();
            var file = CreateFile("diagram.png", [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A]);

            var url = await SaveAsync(storage, file, "lessons", entityId);

            Assert.StartsWith($"/uploads/lessons/{entityId:D}/", url);
            Assert.True(storage.IsSafeImagePath(url, "lessons", entityId));
            Assert.False(storage.IsSafeImagePath(url, "lessons", Guid.NewGuid()));
            Assert.True(File.Exists(Path.Combine(root, url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar))));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Theory]
    [InlineData("photo.jpg", new byte[] { 0xFF, 0xD8, 0xFF })]
    [InlineData("photo.jpeg", new byte[] { 0xFF, 0xD8, 0xFF })]
    [InlineData("photo.webp", new byte[] { 0x52, 0x49, 0x46, 0x46, 0x00, 0x00, 0x00, 0x00, 0x57, 0x45, 0x42, 0x50 })]
    public async Task Storage_accepts_supported_signed_formats(string name, byte[] content)
    {
        var root = Path.Combine(Path.GetTempPath(), "OumezzineAcademy-Phase3B", Guid.NewGuid().ToString("N"));
        try
        {
            var storage = new FileSystemMediaStorage(root);
            var url = await SaveAsync(storage, CreateFile(name, content), "courses", Guid.NewGuid());
            Assert.EndsWith(Path.GetExtension(name), url, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Theory]
    [InlineData(".exe", new byte[] { 0x4D, 0x5A })]
    [InlineData(".html", new byte[] { 0x3C, 0x68, 0x74, 0x6D, 0x6C })]
    [InlineData(".js", new byte[] { 0x61, 0x6C, 0x65, 0x72, 0x74 })]
    [InlineData(".svg", new byte[] { 0x3C, 0x73, 0x76, 0x67, 0x3E })]
    [InlineData(".png", new byte[] { 0x4D, 0x5A })]
    public async Task Storage_rejects_invalid_image_uploads(string extension, byte[] content)
    {
        var root = Path.Combine(Path.GetTempPath(), "OumezzineAcademy-Phase3B", Guid.NewGuid().ToString("N"));
        try
        {
            var storage = new FileSystemMediaStorage(root);
            await Assert.ThrowsAsync<InvalidDataException>(() => SaveAsync(storage, CreateFile($"upload{extension}", content), "courses", Guid.NewGuid()));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Storage_rejects_oversized_files_and_path_escape()
    {
        var root = Path.Combine(Path.GetTempPath(), "OumezzineAcademy-Phase3B", Guid.NewGuid().ToString("N"));
        try
        {
            var storage = new FileSystemMediaStorage(root);
            var oversized = new byte[5 * 1024 * 1024 + 1];
            oversized[0] = 0x89;
            oversized[1] = 0x50;
            oversized[2] = 0x4E;
            oversized[3] = 0x47;
            await Assert.ThrowsAsync<InvalidDataException>(() => SaveAsync(storage, CreateFile("large.png", oversized), "courses", Guid.NewGuid()));
            Assert.False(storage.IsSafeImagePath("/uploads/courses/../other/file.png", "courses", Guid.NewGuid()));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    private static FormFile CreateFile(string name, byte[] content)
    {
        var stream = new MemoryStream(content);
        var contentType = Path.GetExtension(name).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            _ => "image/png"
        };
        return new FormFile(stream, 0, stream.Length, "file", name) { Headers = new HeaderDictionary(), ContentType = contentType };
    }

    private static async Task<string> SaveAsync(IMediaStorage storage, IFormFile file, string area, Guid entityId)
    {
        await using var stream = file.OpenReadStream();
        return await storage.SaveImageAsync(new MediaUpload(file.FileName, file.ContentType, file.Length, stream), area, entityId);
    }
}