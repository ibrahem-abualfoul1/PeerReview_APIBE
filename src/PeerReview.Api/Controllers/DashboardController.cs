using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using PeerReview.Application.DTOs;
using PeerReview.Infrastructure.Persistence;

namespace PeerReview.Api.Controllers;
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _db;
    public DashboardController(AppDbContext db) => _db = db;
    int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<ActionResult<DashboardDto>> Get()
    {
        var me = await _db.Users
            .Include(x => x.Role)
            .FirstAsync(x => x.Id == CurrentUserId);

        var dto = new DashboardDto();

        // ============================
        // 1) USER BASIC METRICS
        // ============================
        var assignedToMe = await _db.Assignments
            .CountAsync(a => a.UserId == me.Id && a.IsActive);

        var answeredByMe = await _db.Answers
            .Where(a => a.UserId == me.Id)
            .Select(a => a.QuestionId)
            .Distinct()
            .CountAsync();

        var myPending = assignedToMe - answeredByMe;
        if (myPending < 0) myPending = 0;

        dto.Metrics["AssignedToMe"] = assignedToMe;
        dto.Metrics["AnsweredByMe"] = answeredByMe;
        dto.Metrics["MyPending"] = myPending;

        // نسبة الإنجاز
        dto.Metrics["MyProgressPercent"] = assignedToMe == 0
            ? 0
            : Math.Round((double)answeredByMe / assignedToMe * 100, 2);

        // ============================
        // 2) USER EXTRA (Latest Activity)
        // ============================
        dto.LatestAnswered = await _db.Answers
            .Where(a => a.UserId == me.Id)
            .OrderByDescending(a => a.SubmittedAt)
            .Take(5)
            .Select(a => new SimpleItemDto
            {
                Id = a.Id,
                Title = a.Question.TitleEn,
                Date = a.SubmittedAt
            })
            .ToListAsync();

        dto.LatestFiles = await _db.AnswerFiles
            .Include(f => f.File)
            .Include(f => f.Answer)
            .Where(f => f.Answer.UserId == me.Id)
            .OrderByDescending(f => f.File.CreatedAt)
            .Take(5)
            .Select(f => new SimpleFileDto
            {
                FileName = f.File.FileName,
                Length = f.File.Length,
                Date = f.File.CreatedAt
            })
            .ToListAsync();

        // إجمالي السكور والمتوسط
        var myScores = await _db.AnswerScores
     .Where(s => s.RevieweeUserId == me.Id)
     .ToListAsync();

        dto.Metrics["MyTotalScore"] = myScores.Sum(s => s.Score);
        dto.Metrics["MyAverageScore"] = myScores.Count == 0
            ? 0
            : Math.Round(myScores.Average(s => s.Score), 2);

        // ============================
        // 3) ADMIN METRICS
        // ============================
        if (me.Role.CanSeeSystemStats)
        {
            dto.Metrics["TotalUsers"] = await _db.Users.CountAsync();
            dto.Metrics["TotalQuestions"] = await _db.Questions.CountAsync();
            dto.Metrics["TotalAssignments"] = await _db.Assignments.CountAsync();
            dto.Metrics["TotalAnswers"] = await _db.Answers.CountAsync();

            // أسئلة جاوب عليها البعض
            dto.Metrics["QuestionsAnswered"] = await _db.Answers
                .Select(a => a.QuestionId)
                .Distinct()
                .CountAsync();

            // أسئلة ولا حدا جاوبها
            var totalQ = await _db.Questions.CountAsync();
            dto.Metrics["QuestionsNotAnswered"] =
                totalQ - Convert.ToInt32(dto.Metrics["QuestionsAnswered"]);
        }

        // ============================
        // 4) ADMIN EXTRA (Analytics)
        // ============================
        if (me.Role.CanSeeSystemStats)
        {
            // Top 5 Answering Users
            dto.TopUsers = await _db.Answers
                .GroupBy(a => new { a.User.Id, a.User.FullName })
                .Select(g => new RankItemDto
                {
                    Name = g.Key.FullName,
                    Count = g.Select(x => x.QuestionId).Distinct().Count()
                })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToListAsync();

            // Top 5 Reviewers by score
            dto.TopReviewers = await _db.AnswerScores
                .GroupBy(s => new { s.Reviewer.Id, s.Reviewer.FullName })
                .Select(g => new RankItemDto
                {
                    Name = g.Key.FullName,
                    Count = g.Sum(x => x.Score)
                })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToListAsync();

            // Top 5 Most Answered Questions
            dto.TopQuestions = await _db.Answers
                .GroupBy(a => new { a.Question.Id, a.Question.TitleEn })
                .Select(g => new RankItemDto
                {
                    Name = g.Key.TitleEn,
                    Count = g.Select(x => x.UserId).Distinct().Count()
                })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToListAsync();
        }

        return dto;
    }

}
