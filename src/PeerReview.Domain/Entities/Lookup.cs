using PeerReview.Domain.Common;
namespace PeerReview.Domain.Entities;
public class Lookup : EntityBase
{
    public string NameAr { get; set; } = "";
    public string NameEn { get; set; } = "";

    public string TypeAr { get; set; } = "";
    public string TypeEn { get; set; } = "";

    public string Code { get; set; }
    public List<SubLookup> SubLookups { get; set; } = new();
}
public class SubLookup : EntityBase
{
    public int LookupId { get; set; }
    public Lookup? Lookup { get; set; }
    public string NameAr { get; set; } = "";
    public string NameEn { get; set; } = "";

}
