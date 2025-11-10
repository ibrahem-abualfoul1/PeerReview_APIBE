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
    public record BatchUserScoreUpdateDto(int UserId, List<BatchScoreItemDto> Items);

    public class WithScoredAnswersDto
    {
        public int UserId { get; set; }
        public string? UserName { get; set; }
        public string? FullName { get; set; }
        public int ScoredCount { get; set; }
        public double AvgScore { get; set; }
        public DateTime? LastScoredAt { get; set; }
    }

    public class UserScoreStatusDto
    {
        public int UserId { get; set; }
        public string? UserName { get; set; }
        public string? FullName { get; set; }

        public int TotalAnswers { get; set; }
        public int ScoredCount { get; set; }
        public int UnscoredCount { get; set; }

        public bool HasScored { get; set; }    // على الأقل إجابة واحدة مُقيّمة لهذا المقيِّم
        public bool HasUnscored { get; set; }  // لديه إجابات غير مُقيّمة لهذا المقيِّم

        public DateTime? LastScoredAt { get; set; }
    }

    public class UsersScoredStatusVm
    {
        public int ReviewerId { get; set; }
        public List<UserScoreStatusDto> Scored { get; set; } = new();
        public List<UserScoreStatusDto> Unscored { get; set; } = new();
    }


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

    // 2) مع باراميتر reviewerId اختياري (إن لم يُمرر = جميع المقيمين)
    [HttpGet("by-user-scored-all")]
    public async Task<IActionResult> GetScoredAnswersByUserAll(
        [FromQuery] int userId,
        CancellationToken ct)
    {
        var q =
            from s in _db.AnswerScores.AsNoTracking()
            join a in _db.Answers.AsNoTracking() on s.AnswerId equals a.Id
            join qi in _db.QuestionItems.AsNoTracking() on a.QuestionItemId equals qi.Id into _qi
            from qi in _qi.DefaultIfEmpty()
            join r in _db.Users.AsNoTracking() on s.ReviewerUserId equals r.Id
            where a.UserId == userId
            select new
            {
                AnswerScoreId = s.Id,
                AnswerId = a.Id,
                a.QuestionId,
                a.QuestionItemId,
                ItemTextAr = qi != null ? qi.TextAr : null,
                ItemTextEn = qi != null ? qi.TextEn : null,
                a.Value,
                s.Score,
                s.Notes,
                s.ScoredAt,
                ReviewerId = s.ReviewerUserId,
                ReviewerName = r.FullName ?? r.UserName
            };


        var data = await q
            .OrderBy(x => x.QuestionId)
            .ThenBy(x => x.QuestionItemId)
            .ToListAsync(ct);

        return Ok(data);
    }


    // GET /api/AnswerScoring/users-with-answers?reviewerId=123 (اختياري)
    [HttpGet("users-with-answers")]
    public async Task<IActionResult> GetUsersWithAnswers(CancellationToken ct = default)
    {
        // نربط AnswerScores -> Answers -> Users
        var query =
            from s in _db.AnswerScores
            join a in _db.Answers on s.AnswerId equals a.Id
            join u in _db.Users on a.UserId equals u.Id
            select new { a.UserId, u.UserName, u.FullName, s.Score, s.ScoredAt, s.ReviewerUserId };

        

        // تجميع حسب المستخدم
        var result = await query
            .GroupBy(x => new { x.UserId, x.UserName, x.FullName })
            .Select(g => new WithScoredAnswersDto
            {
                UserId = g.Key.UserId,
                UserName = g.Key.UserName,
                FullName = g.Key.FullName,
                ScoredCount = g.Count(),
                AvgScore = g.Average(x => (double?)x.Score) ?? 0,
                LastScoredAt = g.Max(x => x.ScoredAt)
            })
            .OrderByDescending(x => x.LastScoredAt)
            .ToListAsync(ct);

        return Ok(result);
    }
    // GET /api/AnswerScoring/users-scored-status?reviewerId=123  (اختياري)
    [HttpGet("users-scored-status")]
    public async Task<IActionResult> GetUsersScoredStatus( CancellationToken ct = default)
    {
        // لو بدك تربطها بالمقيِّم الحالي افتراضيًا:

        // لكل Answer نعرف هل عليه Score لهذا المقيّم أم لا
        var perAnswer =
            from a in _db.Answers.AsNoTracking()
            join s in _db.AnswerScores.AsNoTracking()
                on a.Id equals s.AnswerId into gs
            from s in gs.DefaultIfEmpty()
            select new
            {
                a.UserId,
                HasScore = s != null,
                ScoredAt = s != null ? s.ScoredAt : (DateTime?)null
            };

        // نجمع على مستوى المستخدم
        var aggregated = await perAnswer
            .GroupBy(x => x.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                TotalAnswers = g.Count(),
                ScoredCount = g.Count(x => x.HasScore),
                UnscoredCount = g.Count(x => !x.HasScore),
                LastScoredAt = g.Max(x => x.ScoredAt)
            })
            .ToListAsync(ct);

        // أسماء المستخدمين
        var userIds = aggregated.Select(x => x.UserId).ToList();
        var usersMap = await _db.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.UserName, u.FullName })
            .ToDictionaryAsync(u => u.Id, ct);

        // إسقاط إلى DTO
        var rows = aggregated.Select(x => new UserScoreStatusDto
        {
            UserId = x.UserId,
            UserName = usersMap.TryGetValue(x.UserId, out var u) ? u.UserName : null,
            FullName = usersMap.TryGetValue(x.UserId, out var u2) ? (u2.FullName ?? u2.UserName) : null,
            TotalAnswers = x.TotalAnswers,
            ScoredCount = x.ScoredCount,
            UnscoredCount = x.UnscoredCount,
            HasScored = x.ScoredCount > 0,
            HasUnscored = x.UnscoredCount > 0,
            LastScoredAt = x.LastScoredAt
        }).ToList();

        // تقسيم لقائمتين
        var result = new UsersScoredStatusVm
        {
            Scored = rows.Where(r => r.HasScored).OrderByDescending(r => r.LastScoredAt).ToList(),
            Unscored = rows.Where(r => !r.HasScored).OrderBy(r => r.FullName ?? r.UserName).ToList()
        };

        return Ok(result);
    }


    // DTO للتحديث الكامل حسب المستخدم

    // PUT /api/AnswerScoring/by-user-scored/batch-update
    [HttpPut("by-user-scored/batch-update")]
    public async Task<IActionResult> UpdateUserScoredBatch([FromBody] BatchUserScoreUpdateDto payload, CancellationToken ct)
    {
        if (payload?.Items == null || payload.Items.Count == 0)
            return BadRequest("لا توجد عناصر للتحديث.");

        var userId = payload.UserId;

        // 1) تأكيد أن كل الإجابات موجودة وتنتمي لهذا المستخدم
        var answerIds = payload.Items.Select(i => i.AnswerId).Distinct().ToList();

        var answers = await _db.Answers
            .Where(a => answerIds.Contains(a.Id))
            .Select(a => new { a.Id, a.UserId })
            .ToListAsync(ct);

        if (answers.Count != answerIds.Count)
        {
            var found = answers.Select(a => a.Id).ToHashSet();
            var missing = answerIds.Where(id => !found.Contains(id));
            return BadRequest("إجابات غير موجودة: " + string.Join(", ", missing));
        }

        var wrongOwner = answers.Where(a => a.UserId != userId).Select(a => a.Id).ToList();
        if (wrongOwner.Count > 0)
            return BadRequest("إجابات لا تعود لهذا المستخدم: " + string.Join(", ", wrongOwner));

        // 2) جلب سجلات التقييم الموجودة لنفس المقيّم (Update-only)
        var existingScores = await _db.AnswerScores
            .Where(s =>  answerIds.Contains(s.AnswerId))
            .ToListAsync(ct);

        var byAnswerId = existingScores.ToDictionary(s => s.AnswerId);
        var missingForReviewer = answerIds.Where(id => !byAnswerId.ContainsKey(id)).ToList();

        if (missingForReviewer.Count > 0)
            return BadRequest("لا يوجد تقييم سابق للتحديث لهذه الإجابات (لهذا المقيّم): " +
                              string.Join(", ", missingForReviewer));

        // 3) تنفيذ التحديث دفعة واحدة
        var nowUtc = DateTime.UtcNow;
        foreach (var item in payload.Items)
        {
            var s = byAnswerId[item.AnswerId];
            s.Score = item.Score;
            s.Notes = item.Notes;
            s.ScoredAt = nowUtc;
        }

        await _db.SaveChangesAsync(ct);

        // 4) (اختياري) رجّع Snapshot بعد التحديث — مفيد لإعادة تعبئة الفورم
        var updated = await (
            from s in _db.AnswerScores.AsNoTracking()
            join a in _db.Answers.AsNoTracking() on s.AnswerId equals a.Id
            join qi in _db.QuestionItems.AsNoTracking() on a.QuestionItemId equals qi.Id into _qi
            from qi in _qi.DefaultIfEmpty()
            where  a.UserId == userId && answerIds.Contains(s.AnswerId)
            select new
            {
                AnswerScoreId = s.Id,
                s.AnswerId,
                a.QuestionId,
                a.QuestionItemId,
                ItemTextAr = qi != null ? qi.TextAr : null,
                ItemTextEn = qi != null ? qi.TextEn : null,
                a.Value,
                s.Score,
                s.Notes,
                s.ScoredAt
            })
            .OrderBy(x => x.QuestionId)
            .ThenBy(x => x.QuestionItemId)
            .ToListAsync(ct);

        return Ok(new { Ok = true, UserId = userId, Updated = updated.Count, Items = updated });
    }

}
