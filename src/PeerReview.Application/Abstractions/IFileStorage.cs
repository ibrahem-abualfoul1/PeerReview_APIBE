namespace PeerReview.Application.Abstractions;
public interface IFileStorage
{
    Task<(string relativePath, long length, string contentType)> SaveAsync(string fileName, Stream stream, string? contentType);
}
