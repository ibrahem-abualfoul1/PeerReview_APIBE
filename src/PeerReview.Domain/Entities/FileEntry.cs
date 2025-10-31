using PeerReview.Domain.Common;
namespace PeerReview.Domain.Entities;
public class FileEntry : EntityBase
{
    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long Length { get; set; }
    public string Path { get; set; } = "";
    public int UploadedByUserId { get; set; }
}
