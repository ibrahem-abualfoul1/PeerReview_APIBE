namespace PeerReview.Domain.Common;
public interface ISoftDelete { bool IsDeleted { get; set; } DateTime? DeletedAt { get; set; } }
public abstract class EntityBase : ISoftDelete
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
