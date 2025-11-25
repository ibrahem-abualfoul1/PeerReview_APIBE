using PeerReview.Domain.Common;

namespace PeerReview.Domain.Entities
{
    public class AnswerFile : EntityBase
    {
        public int AnswerId { get; set; }
        public int FileId { get; set; }
        public Answer Answer { get; set; } = null!;
        public FileEntry File { get; set; } = null!;
    }
}
