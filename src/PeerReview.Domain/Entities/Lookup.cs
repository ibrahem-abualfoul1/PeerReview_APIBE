using PeerReview.Domain.Common;
namespace PeerReview.Domain.Entities;
public class Lookup : EntityBase
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public List<SubLookup> SubLookups { get; set; } = new();
}
public class SubLookup : EntityBase
{
    public int LookupId { get; set; }
    public Lookup? Lookup { get; set; }
    public string Name { get; set; } = "";
}
