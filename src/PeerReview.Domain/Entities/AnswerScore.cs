using PeerReview.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeerReview.Domain.Entities;

public class AnswerScore : EntityBase
{
    public int AnswerId { get; set; }
    public Answer Answer { get; set; } = null!;

    public int ReviewerUserId { get; set; }      
    public User Reviewer { get; set; } = null!;

    public decimal Score { get; set; }           
    public string? Notes { get; set; }           
    public DateTime ScoredAt { get; set; } = DateTime.UtcNow;
}