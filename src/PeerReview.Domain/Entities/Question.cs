using PeerReview.Domain.Common;
using PeerReview.Domain.Enums;
namespace PeerReview.Domain.Entities;
public class Question : EntityBase
{
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public int? CategoryId { get; set; }
    public Lookup? Category { get; set; }
    public List<QuestionItem> Items { get; set; } = new();
}
public class QuestionItem : EntityBase
{
    public int QuestionId { get; set; }
    public Question? Question { get; set; }
    public string Text { get; set; } = "";
    public QuestionType Type { get; set; }
    public bool IsRequired { get; set; }
    public string? OptionsCsv { get; set; }
    public int? ParentItemId { get; set; }
    public string? ShowWhenValue { get; set; }
}
