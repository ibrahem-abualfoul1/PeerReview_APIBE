using PeerReview.Domain.Common;

namespace PeerReview.Domain.Entities
{
    public class Answer : EntityBase
    {
        public int QuestionId { get; set; }             // Required FK -> Question
        public Question Question { get; set; } = null!; // Required nav

        public int? QuestionItemId { get; set; }        // Optional FK -> QuestionItem
        public QuestionItem? QuestionItem { get; set; } // Optional nav

        public int UserId { get; set; }                 // Required FK -> User
        public User User { get; set; } = null!;         // Required nav

        public string? Value { get; set; }

        

        public DateTime? SubmittedAt { get; set; }

        public ICollection<AnswerFile> Files { get; set; } = new List<AnswerFile>();

    }
}
