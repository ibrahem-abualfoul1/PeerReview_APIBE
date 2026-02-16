using PeerReview.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeerReview.Domain.Entities;

public class AnswerScore : EntityBase
{
    public int QuestionId { get; set; }
    public Question Question { get; set; } = null!;

    // الشخص الذي يتم تقييمه
    public int RevieweeUserId { get; set; }
    public User Reviewee { get; set; } = null!;

    // الشخص الذي يقوم بالتقييم
    public int ReviewerUserId { get; set; }
    public User Reviewer { get; set; } = null!;

    public decimal Score { get; set; }
    public string? Notes { get; set; }
    public DateTime ScoredAt { get; set; } = DateTime.UtcNow;
}
