using PeerReview.Domain.Common;
namespace PeerReview.Domain.Entities;
public class Answer : EntityBase
{
    public int QuestionId { get; set; }
    public int? QuestionItemId { get; set; }
    public int UserId { get; set; }
    public string? Value { get; set; }
    public int? FileId { get; set; }
    public FileEntry? File { get; set; }
    public DateTime? SubmittedAt { get; set; }
}
