using PeerReview.Domain.Common;
using PeerReview.Domain.Enums;
namespace PeerReview.Domain.Entities;
public class Question : EntityBase
{
    public string TitleEn { get; set; } = "";
    public string DescriptionEn { get; set; } = "";

    public int CategoryId { get; set; }
    public Lookup Category { get; set; } = null!;

    public int? SubCategoryId { get; set; }
    public SubLookup? SubCategory { get; set; }

    public ICollection<QuestionItem> Items { get; set; } = new List<QuestionItem>();

    public ICollection<AnswerScore> Scores { get; set; } = new List<AnswerScore>();
}


public class QuestionItem : EntityBase
{
    public string TextEn { get; set; } = "";

    public QuestionType Type { get; set; }
    public bool IsRequired { get; set; }
    public string? OptionsCsvEn { get; set; }

    public ICollection<Question> Questions { get; set; } = new List<Question>();

}
