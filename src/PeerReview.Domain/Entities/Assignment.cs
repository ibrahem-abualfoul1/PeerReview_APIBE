using PeerReview.Domain.Common;
namespace PeerReview.Domain.Entities;
public class Assignment : EntityBase
{
    public int QuestionId { get; set; }
    public int UserId { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}
