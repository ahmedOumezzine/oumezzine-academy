using OumezzineAcademy.Application.Abstractions;

namespace OumezzineAcademy.Infrastructure.Media;

public sealed class FileSystemMediaStorage(string webRootPath) : IMediaStorage
{
    private const long MaxImageBytes = 5 * 1024 * 1024;
    private static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };

    private static readonly Dictionary<string, byte[]> Signatures = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = [0xFF, 0xD8, 0xFF],
        [".jpeg"] = [0xFF, 0xD8, 0xFF],
        [".png"] = [0x89, 0x50, 0x4E, 0x47],
        [".webp"] = [0x52, 0x49, 0x46, 0x46]
    };

    public async Task<string> SaveImageAsync(MediaUpload upload, string area, Guid entityId, CancellationToken cancellationToken = default)
    {
        if (upload is null || upload.Length <= 0 || upload.Length > MaxImageBytes) throw new InvalidDataException("Image size is invalid.");
        var extension = Path.GetExtension(upload.FileName);
        if (!Extensions.Contains(extension) || !Signatures.TryGetValue(extension, out var signature)) throw new InvalidDataException("Image format is not allowed.");
        var header = new byte[12];
        var read = await upload.Content.ReadAsync(header, cancellationToken);
        if (read < signature.Length || !header.AsSpan(0, signature.Length).SequenceEqual(signature) || (extension.Equals(".webp", StringComparison.OrdinalIgnoreCase) && !header.AsSpan(8, 4).SequenceEqual("WEBP"u8))) throw new InvalidDataException("Image content is invalid.");
        var folder = SafeFolder(area, entityId);
        Directory.CreateDirectory(folder);
        var name = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        await using var output = File.Create(Path.Combine(folder, name));
        if (!upload.Content.CanSeek) throw new InvalidDataException("Image stream must be seekable.");
        upload.Content.Position = 0;
        await upload.Content.CopyToAsync(output, cancellationToken);
        return $"/uploads/{area}/{entityId:D}/{name}";
    }

    public bool IsSafeImagePath(string? relativePath, string area, Guid entityId)
        => relativePath is not null && Path.GetFullPath(Path.Combine(webRootPath, relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar))).StartsWith(SafeFolder(area, entityId) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);

    public void DeleteIfSafe(string? relativePath, string area, Guid entityId)
    {
        if (!IsSafeImagePath(relativePath, area, entityId)) return;
        var path = Path.GetFullPath(Path.Combine(webRootPath, relativePath!.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)));
        if (File.Exists(path)) File.Delete(path);
    }

    private string SafeFolder(string area, Guid entityId)
    {
        if (area is not ("courses" or "lessons" or "chapters" or "learning-paths")) throw new ArgumentException("Unsupported media area.", nameof(area));
        return Path.Combine(webRootPath, "uploads", area, entityId.ToString("D"));
    }
}