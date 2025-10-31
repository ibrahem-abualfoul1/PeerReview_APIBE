using PeerReview.Domain.Common;
namespace PeerReview.Domain.Entities;
public class Role : EntityBase
{
    public string Name { get; set; } = "";
    public bool CanSeeAllUsers { get; set; }
    public bool CanSeeSystemStats { get; set; }
    public bool CanSeeAssignmentsAll { get; set; }
    public bool CanSeeAnswersAll { get; set; }
    public List<User> Users { get; set; } = new();
}
