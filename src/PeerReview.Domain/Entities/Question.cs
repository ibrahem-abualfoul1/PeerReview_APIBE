using PeerReview.Domain.Common;
using PeerReview.Domain.Enums;
namespace PeerReview.Domain.Entities;
public class Question : EntityBase
{
    public string TitleAr { get; set; } = "";
    public string TitleEn { get; set; } = "";
    public string DescriptionAr { get; set; } = "";
    public string DescriptionEn { get; set; } = "";

    public Lookup Category { get; set; }
    public int CategoryId { get; set; }
    public ICollection<QuestionItem> Items { get; set; } = new List<QuestionItem>();


}
public class QuestionItem : EntityBase
{
    public string TextAr { get; set; } = "";
    public string TextEn { get; set; } = "";

    public QuestionType Type { get; set; }
    public bool IsRequired { get; set; }
    public string? OptionsCsvAr { get; set; }
    public string? OptionsCsvEn { get; set; }

    public int QuestionId { get; set; }
    public Question Question
    {
        get; set;
    }
}
