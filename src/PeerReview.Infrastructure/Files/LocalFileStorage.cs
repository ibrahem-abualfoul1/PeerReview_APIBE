using PeerReview.Application.Abstractions;

namespace PeerReview.Infrastructure.Files;
public class LocalFileStorage : IFileStorage
{
    private readonly string _root;
    public LocalFileStorage(string root)
    {
        _root = root;
        Directory.CreateDirectory(_root);
    }
    public async Task<(string relativePath, long length, string contentType)> SaveAsync(string fileName, Stream stream, string? contentType)
    {
        var safe = fileName.Replace("..", "").Replace("/", "_").Replace("\\", "_");
        var name = $"{Guid.NewGuid()}_{safe}";
        var full = Path.Combine(_root, name);
        using (var fs = File.Create(full))
        {
            await stream.CopyToAsync(fs);
        }
        
        var rel = name;
        return (rel, new FileInfo(full).Length, contentType ?? "application/octet-stream");
    }
}
