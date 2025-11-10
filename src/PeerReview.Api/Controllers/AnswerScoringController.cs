using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PeerReview.Infrastructure.Persistence;
using System.Security.Claims;

namespace PeerReview.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AnswerScoringController : ControllerBase
{
    private readonly AppDbContext _db;
    public AnswerScoringController(AppDbContext db) => _db = db;
    int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public record BatchScoreItemDto(int AnswerId, decimal Score, string? Notes);
    public record BatchScoreUpsertDto(List<BatchScoreItemDto> Items);

    [HttpGet("by-user-unscored")]
    public async Task<IActionResult> GetUnscoredAnswersByUser([FromQuery] int userId, CancellationToken ct)
    {
        var data = await _db.Answers
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .Where(a => !_db.AnswerScores.Any(s => s.AnswerId == a.Id))
            .Select(a => new
            {
                AnswerId = a.Id,
                a.QuestionId,
                a.QuestionItemId,
                ItemTextAr = a.QuestionItem != null ? a.QuestionItem.TextAr : null,
                ItemTextEn = a.QuestionItem != null ? a.QuestionItem.TextEn : null,
                a.Value,
                a.SubmittedAt
            })
            .OrderBy(x => x.QuestionId)
            .ThenBy(x => x.QuestionItemId)
            .ToListAsync(ct);

        return Ok(data);
    }

    [HttpGet("reviewers-summary")]
    public async Task<IActionResult> GetReviewerSummary(CancellationToken ct)
    {
        var summary = await _db.AnswerScores
            .Include(s => s.Reviewer)
            .GroupBy(s => new
            {
                s.ReviewerUserId,
                s.Reviewer.FullName,
                s.Reviewer.UserName
            })
            .Select(g => new
            {
                ReviewerUserId = g.Key.ReviewerUserId,
                ReviewerFullName = g.Key.FullName,
                ReviewerUserName = g.Key.UserName,
                ReviewedAnswersCount = g.Count(),
                LastReviewedAt = g.Max(s => s.ScoredAt)
            })
            .OrderByDescending(x => x.ReviewedAnswersCount)
            .ToListAsync(ct);

        return Ok(summary);
    }



    private int GetCurrentReviewerId()
    {
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? User.FindFirstValue("sub")
                   ?? User.FindFirstValue("uid");
        return int.TryParse(idStr, out var id) ? id : 1; // fallback
    }

    [HttpPost("add-score")]
    public async Task<IActionResult> AddAnswerScore([FromBody] BatchScoreUpsertDto payload, CancellationToken ct)
    {
        if (payload?.Items == null || payload.Items.Count == 0)
            return BadRequest("لا توجد عناصر للتصحيح.");

        var reviewerId = GetCurrentReviewerId();

        var answerIds = payload.Items.Select(i => i.AnswerId).Distinct().ToList();
        var existingAnswers = await _db.Answers
            .Where(a => answerIds.Contains(a.Id))
            .Select(a => a.Id)
            .ToListAsync(ct);
        if (existingAnswers.Count != answerIds.Count)
        {
            var found = existingAnswers.ToHashSet();
            var missing = answerIds.Where(id => !found.Contains(id));
            return BadRequest($"إجابات غير موجودة: {string.Join(", ", missing)}");
        }

        var existingScores = await _db.AnswerScores
            .Where(s => answerIds.Contains(s.AnswerId))
            .ToListAsync(ct);
        var byAnswerId = existingScores.ToDictionary(s => s.AnswerId);

        foreach (var item in payload.Items)
        {
            

            if (byAnswerId.TryGetValue(item.AnswerId, out var s))
            {
                // UPDATE
                s.Score = item.Score;
                s.Notes = item.Notes;
                s.ScoredAt = DateTime.UtcNow;
            }
            else
            {
                // INSERT
                _db.AnswerScores.Add(new Domain.Entities.AnswerScore
                {
                    AnswerId = item.AnswerId,
                    ReviewerUserId = reviewerId,
                    Score = item.Score,
                    Notes = item.Notes,
                    ScoredAt = DateTime.UtcNow
                });
            }
        }

        await _db.SaveChangesAsync(ct);

        return await GetAllUserTotalScores(ct);
    }


    [HttpGet("all-scores")]
    public async Task<IActionResult> GetAllUserTotalScores(CancellationToken ct)
    {
        var result = await _db.AnswerScores
            .Include(s => s.Answer)
            .ThenInclude(a => a.User)
            .GroupBy(s => new
            {
                s.Answer.UserId,
                s.Answer.User.FullName,
                s.Answer.User.UserName
            })
            .Select(g => new
            {
                UserId = g.Key.UserId,
                FullName = g.Key.FullName,
                UserName = g.Key.UserName,
                TotalScore = g.Sum(x => x.Score),
                AnswersCount = g.Count()
            })
            .OrderByDescending(x => x.TotalScore)
            .ToListAsync(ct);

        return Ok(result);
    }

    [HttpGet("users-with-unscored-answers")]
    public async Task<IActionResult> GetUsersWithUnscoredAnswers(
    CancellationToken ct = default)
    {

        var unscoredPerUser = await _db.Answers
            .AsNoTracking()
            .Where(a => !_db.AnswerScores
                .Any(s => s.AnswerId == a.Id))
            .GroupBy(a => a.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                UnscoredCount = g.Count()
            })
            .ToListAsync(ct);

        var userIds = unscoredPerUser.Select(x => x.UserId).ToList();

        var users = await _db.Users
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FullName, u.UserName, u.Email, u.IsActive })
            .ToListAsync(ct);

        var result = (from u in users
                      join x in unscoredPerUser on u.Id equals x.UserId
                      orderby x.UnscoredCount descending, u.FullName
                      select new
                      {
                          u.Id,
                          u.FullName,
                          u.UserName,
                          u.Email,
                          u.IsActive,
                          x.UnscoredCount
                      }).ToList();

        return Ok(result);
    }

}
