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

    // ================== DTOs ==================

    // لإرسال السكور لكل سؤال
    public record QuestionScoreItemDto(int QuestionId, decimal Score, string? Notes);
    public record QuestionScoreUpsertDto(int RevieweeUserId, List<QuestionScoreItemDto> Items);

    // لعرض الـ Items مع الإجابات لكل سؤال
    public class QuestionItemAnswerDto
    {
        public int QuestionItemId { get; set; }
        public string ItemTextEn { get; set; } = "";
        public string? AnswerValue { get; set; }

        public List<object> Files { get; set; } = new(); // عدّل النوع لو عندك DTO للملف
    }

    public class QuestionReviewDto
    {
        public int QuestionId { get; set; }
        public string QuestionTitleEn { get; set; } = "";
        public string QuestionDescriptionEn { get; set; } = "";

        // عناصر السؤال + إجاباتها
        public List<QuestionItemAnswerDto> Items { get; set; } = new();

        // للعرض فقط (إن وجد سكور قديم)
        public decimal? ExistingScore { get; set; }
        public string? ExistingNotes { get; set; }
    }

    public class WithScoredAnswersDto
    {
        public int UserId { get; set; }
        public string? UserName { get; set; }
        public string? FullName { get; set; }
        public int ScoredCount { get; set; }       // عدد الأسئلة المقيَّمة
        public double AvgScore { get; set; }
        public DateTime? LastScoredAt { get; set; }
    }

    public class UserScoreStatusDto
    {
        public int UserId { get; set; }
        public string? UserName { get; set; }
        public string? FullName { get; set; }

        public int TotalQuestions { get; set; }    // عدد الأسئلة التي أجاب عنها المستخدم
        public int ScoredCount { get; set; }       // عدد الأسئلة التي تقيّمت
        public int UnscoredCount { get; set; }     // عدد الأسئلة غير المقيّمة

        public bool HasScored { get; set; }
        public bool HasUnscored { get; set; }

        public DateTime? LastScoredAt { get; set; }
        public decimal TotalScore { get; set; }
    }

    public class UsersScoredStatusVm
    {
        public List<UserScoreStatusDto> Scored { get; set; } = new();
        public List<UserScoreStatusDto> Unscored { get; set; } = new();
    }
    public class ScoreDto
    {
        public int QuestionId { get; set; }
        public decimal? Score { get; set; }
        public string? Notes { get; set; }
    }

    public class ScoreUpdateDto
    {
        public int UserId { get; set; }
        public List<ScoreDto> Items { get; set; } = new();
    }


    // ================== Helpers ==================

    private int GetCurrentReviewerId()
    {
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? User.FindFirstValue("sub")
                   ?? User.FindFirstValue("uid");

        return int.TryParse(idStr, out var id) ? id : 1;
    }

    // ================== 1) جلب أسئلة مستخدم لم تُقيَّم بعد ==================
    // GET: api/AnswerScoring/by-user-unscored?userId=5
    // يرجع List<QuestionReviewDto> : كل سؤال + عناصره + إجابات المستخدم
    [HttpGet("by-user-unscored")]
    public async Task<IActionResult> GetUnscoredQuestionsByUser(
        [FromQuery] int userId,
        CancellationToken ct)
    {
        // كل الإجابات لهذا المستخدم
        var answers = await _db.Answers
            .Include(a => a.Question)
            .Include(a => a.QuestionItem)
            .Include(a => a.Files).ThenInclude(f => f.File)
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .ToListAsync(ct);

        if (!answers.Any())
            return Ok(new List<QuestionReviewDto>());

        var questionIds = answers.Select(a => a.QuestionId).Distinct().ToList();

        // الأسئلة التي تم تقييمها لهذا المستخدم مسبقاً
        var scoredQuestionIds = await _db.AnswerScores
            .Where(s => s.RevieweeUserId == userId && questionIds.Contains(s.QuestionId))
            .Select(s => s.QuestionId)
            .Distinct()
            .ToListAsync(ct);

        var scoredSet = scoredQuestionIds.ToHashSet();

        // فقط الأسئلة غير المقيّمة
        var unscoredGroups = answers
            .Where(a => !scoredSet.Contains(a.QuestionId))
            .GroupBy(a => a.QuestionId)
            .ToList();

        var result = new List<QuestionReviewDto>();

        foreach (var g in unscoredGroups)
        {
            var any = g.First();
            var dto = new QuestionReviewDto
            {
                QuestionId = g.Key,
                QuestionTitleEn = any.Question.TitleEn,
                QuestionDescriptionEn = any.Question.DescriptionEn,
                Items = g
                    .OrderBy(a => a.QuestionItemId)
                    .Select(a => new QuestionItemAnswerDto
                    {
                        QuestionItemId = a.QuestionItemId ?? 0,
                        ItemTextEn = a.QuestionItem?.TextEn ?? "",
                        AnswerValue = a.Value,
                        Files = a.Files
                            .Select(f => new
                            {

                                FileId = f.FileId,
                                FileName = f.File.FileName,
                                Size = f.File.Length,
                                Url = f.File.Path
                            } as object)
                            .ToList()
                    })
                    .ToList()
            };

            result.Add(dto);
        }

        return Ok(result);
    }

    // ================== 2) إضافة / تعديل سكورات الأسئلة لمستخدم ==================
    // POST: api/AnswerScoring/add-question-scores
    /*
        {
          "revieweeUserId": 5,
          "items": [
            { "questionId": 3, "score": 4.5, "notes": "..." },
            { "questionId": 7, "score": 2.0, "notes": null }
          ]
        }
    */
    [HttpPost("add-question-scores")]
    public async Task<IActionResult> AddQuestionScores(
        [FromBody] QuestionScoreUpsertDto payload,
        CancellationToken ct)
    {
        if (payload == null || payload.Items == null || payload.Items.Count == 0)
            return BadRequest("لا توجد عناصر للتصحيح.");

        var reviewerId = GetCurrentReviewerId();
        var revieweeId = payload.RevieweeUserId;

        var questionIds = payload.Items.Select(i => i.QuestionId).Distinct().ToList();

        // تأكيد وجود الأسئلة
        var existingQuestions = await _db.Questions
            .Where(q => questionIds.Contains(q.Id))
            .Select(q => q.Id)
            .ToListAsync(ct);

        if (existingQuestions.Count != questionIds.Count)
        {
            var found = existingQuestions.ToHashSet();
            var missing = questionIds.Where(id => !found.Contains(id));
            return BadRequest("أسئلة غير موجودة: " + string.Join(", ", missing));
        }

        // جلب السكورات السابقة لنفس (المستخدم المُقيَّم + المقيِّم + الأسئلة)
        var existingScores = await _db.AnswerScores
            .Where(s =>
                s.RevieweeUserId == revieweeId &&
                s.ReviewerUserId == reviewerId &&
                questionIds.Contains(s.QuestionId))
            .ToListAsync(ct);

        var byQuestionId = existingScores.ToDictionary(s => s.QuestionId);
        var now = DateTime.UtcNow;

        foreach (var item in payload.Items)
        {
            if (byQuestionId.TryGetValue(item.QuestionId, out var s))
            {
                // UPDATE
                s.Score = item.Score;
                s.Notes = item.Notes;
                s.ScoredAt = now;
            }
            else
            {
                // INSERT
                _db.AnswerScores.Add(new Domain.Entities.AnswerScore
                {
                    QuestionId = item.QuestionId,
                    RevieweeUserId = revieweeId,
                    ReviewerUserId = reviewerId,
                    Score = item.Score,
                    Notes = item.Notes,
                    ScoredAt = now
                });
            }
        }

        await _db.SaveChangesAsync(ct);

        // يرجّع ملخّص إجمالي لكل المستخدمين بعد التعديل
        return await GetAllUserTotalScores(ct);
    }

    // ================== 3) ملخص للمقيمين (كم سؤال قيّموا) ==================
    // GET: api/AnswerScoring/reviewers-summary
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
                ReviewedQuestionsCount = g.Count(),
                LastReviewedAt = g.Max(s => s.ScoredAt)
            })
            .OrderByDescending(x => x.ReviewedQuestionsCount)
            .ToListAsync(ct);

        return Ok(summary);
    }

    // ================== 4) إجمالي سكورات المستخدمين ==================
    // GET: api/AnswerScoring/all-scores
    [HttpGet("all-scores")]
    public async Task<IActionResult> GetAllUserTotalScores(CancellationToken ct)
    {
        var result = await _db.AnswerScores
            .Include(s => s.Reviewee)
            .GroupBy(s => new
            {
                s.RevieweeUserId,
                s.Reviewee.FullName,
                s.Reviewee.UserName
            })
            .Select(g => new
            {
                UserId = g.Key.RevieweeUserId,
                FullName = g.Key.FullName,
                UserName = g.Key.UserName,
                TotalScore = g.Sum(x => x.Score),
                QuestionsCount = g.Count()
            })
            .OrderByDescending(x => x.TotalScore)
            .ToListAsync(ct);

        return Ok(result);
    }

    // ================== 5) مستخدمون لديهم أسئلة غير مقيّمة ==================
    // GET: api/AnswerScoring/users-with-unscored-questions
    [HttpGet("users-with-unscored-questions")]
    public async Task<IActionResult> GetUsersWithUnscoredQuestions(
        CancellationToken ct = default)
    {
        // كل (UserId, QuestionId) التي لا يوجد لها AnswerScore
        var unscoredQuestions = await (
            from a in _db.Answers.AsNoTracking()
            where !(
                from s in _db.AnswerScores.AsNoTracking()
                where s.RevieweeUserId == a.UserId && s.QuestionId == a.QuestionId
                select 1
            ).Any()
            group a by new { a.UserId, a.QuestionId } into g
            select new
            {
                g.Key.UserId,
                g.Key.QuestionId
            })
            .ToListAsync(ct);

        var unscoredPerUser = unscoredQuestions
            .GroupBy(x => x.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                UnscoredQuestionsCount = g.Count()
            })
            .ToList();

        var userIds = unscoredPerUser.Select(x => x.UserId).ToList();

        var users = await _db.Users
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FullName, u.UserName, u.Email, u.IsActive })
            .ToListAsync(ct);

        var result = (
            from u in users
            join x in unscoredPerUser on u.Id equals x.UserId
            orderby x.UnscoredQuestionsCount descending, u.FullName
            select new
            {
                u.Id,
                u.FullName,
                u.UserName,
                u.Email,
                u.IsActive,
                x.UnscoredQuestionsCount
            }).ToList();

        return Ok(result);
    }

    // ================== 6) مستخدمون لديهم سكورات (للتقارير) ==================
    // GET: api/AnswerScoring/users-with-answers
    [HttpGet("users-with-answers")]
    public async Task<IActionResult> GetUsersWithAnswers(CancellationToken ct = default)
    {
        var query =
            from s in _db.AnswerScores
            join u in _db.Users on s.RevieweeUserId equals u.Id
            select new
            {
                u.Id,
                u.UserName,
                u.FullName,
                s.Score,
                s.ScoredAt,
                s.ReviewerUserId
            };

        var result = await query
            .GroupBy(x => new { x.Id, x.UserName, x.FullName })
            .Select(g => new WithScoredAnswersDto
            {
                UserId = g.Key.Id,
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

    // ================== 7) حالة التقييم لكل مستخدم (كم سؤال مقيَّم / غير مقيَّم) ==================
    // GET: api/AnswerScoring/users-scored-status
    [HttpGet("users-scored-status")]
    public async Task<IActionResult> GetUsersScoredStatus(
        CancellationToken ct = default)
    {
        // كل (UserId, QuestionId) التي تمت الإجابة عليها
        var perQuestion =
            from a in _db.Answers.AsNoTracking()
            group a by new { a.UserId, a.QuestionId } into g
            select new
            {
                g.Key.UserId,
                g.Key.QuestionId
            };

        // نربطها مع AnswerScores
        var perQuestionWithStatus =
            from q in perQuestion
            join s in _db.AnswerScores.AsNoTracking()
                on new { RevieweeUserId = q.UserId, q.QuestionId }
                equals new { s.RevieweeUserId, s.QuestionId } into gs
            from s in gs.DefaultIfEmpty()
            select new
            {
                q.UserId,
                HasScore = s != null,
                ScoredAt = s != null ? (DateTime?)s.ScoredAt : null,
                Score = s != null ? s.Score : 0m
            };

        var aggregated = await perQuestionWithStatus
            .GroupBy(x => x.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                TotalQuestions = g.Count(),
                ScoredCount = g.Count(x => x.HasScore),
                UnscoredCount = g.Count(x => !x.HasScore),
                LastScoredAt = g.Max(x => x.ScoredAt),
                TotalScore = g.Sum(x => x.Score)
            })
            .ToListAsync(ct);

        var userIds = aggregated.Select(x => x.UserId).ToList();

        var usersMap = await _db.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.UserName, u.FullName })
            .ToDictionaryAsync(u => u.Id, ct);

        var rows = aggregated.Select(x => new UserScoreStatusDto
        {
            UserId = x.UserId,
            UserName = usersMap.TryGetValue(x.UserId, out var u) ? u.UserName : null,
            FullName = usersMap.TryGetValue(x.UserId, out var u2) ? (u2.FullName ?? u2.UserName) : null,
            TotalQuestions = x.TotalQuestions,
            ScoredCount = x.ScoredCount,
            UnscoredCount = x.UnscoredCount,
            HasScored = x.ScoredCount > 0,
            HasUnscored = x.UnscoredCount > 0,
            LastScoredAt = x.LastScoredAt,
            TotalScore = x.TotalScore
        }).ToList();

        var result = new UsersScoredStatusVm
        {
            Scored = rows.Where(r => r.HasScored).OrderByDescending(r => r.LastScoredAt).ToList(),
            Unscored = rows.Where(r => !r.HasScored).OrderBy(r => r.FullName ?? r.UserName).ToList()
        };

        return Ok(result);
    }


    [HttpGet("by-user-scored")]
    public async Task<IActionResult> GetScoredQuestionsByUser(
    [FromQuery] int userId,
    CancellationToken ct)
    {
        // المقيّم الحالي (الـ Reviewer)
        var reviewerId = GetCurrentReviewerId();

        // كل الإجابات لهذا المستخدم
        var answers = await _db.Answers
            .Include(a => a.Question)
            .Include(a => a.QuestionItem)
            .Include(a => a.Files).ThenInclude(f => f.File)
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .ToListAsync(ct);

        if (!answers.Any())
            return Ok(new List<QuestionReviewDto>());

        var questionIds = answers
            .Select(a => a.QuestionId)
            .Distinct()
            .ToList();

        // السكورات لهذا المستخدم (المُقَيَّم) من نفس المقيّم الحالي
        var scores = await _db.AnswerScores
            .Where(s =>
                s.RevieweeUserId == userId &&
                s.ReviewerUserId == reviewerId &&
                questionIds.Contains(s.QuestionId))
            .ToListAsync(ct);

        if (!scores.Any())
            return Ok(new List<QuestionReviewDto>());

        // آخر سكور لكل QuestionId (لو انكتب أكثر من مرة)
        var scoreByQuestion = scores
            .GroupBy(s => s.QuestionId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(x => x.ScoredAt).First()
            );

        var scoredQuestionIds = scoreByQuestion.Keys.ToHashSet();

        // فقط الأسئلة التي لها سكور
        var scoredGroups = answers
            .Where(a => scoredQuestionIds.Contains(a.QuestionId))
            .GroupBy(a => a.QuestionId)
            .ToList();

        var result = new List<QuestionReviewDto>();

        foreach (var g in scoredGroups)
        {
            var any = g.First();
            var scoreRow = scoreByQuestion[g.Key];

            var dto = new QuestionReviewDto
            {
                QuestionId = g.Key,
                QuestionTitleEn = any.Question?.TitleEn ?? "",
                QuestionDescriptionEn = any.Question?.DescriptionEn ?? "",
                Items = g
                    .OrderBy(a => a.QuestionItemId)
                    .Select(a => new QuestionItemAnswerDto
                    {
                        QuestionItemId = a.QuestionItemId ?? 0,
                        ItemTextEn = a.QuestionItem?.TextEn ?? "",
                        AnswerValue = a.Value,
                        Files = a.Files
                            .Select(f => new
                            {
                                FileId = f.FileId,
                                FileName = f.File.FileName,
                                Size = f.File.Length,
                                Url = f.File.Path
                            } as object)
                            .ToList()
                    })
                    .ToList(),

                ExistingScore = scoreRow.Score,
                ExistingNotes = scoreRow.Notes
            };

            result.Add(dto);
        }

        return Ok(result);
    }
    // ================== 8) Batch Update للسكورات لمستخدم واحد (Scored Answers Form) ==================
    // PUT: api/AnswerScoring/by-user-scored/batch-update
    [HttpPut("by-user-scored/batch-update")]
    public async Task<IActionResult> BatchUpdateScoredQuestions(
        [FromBody] ScoreUpdateDto req,
        CancellationToken ct)
    {
        if (req.Items == null || req.Items.Count == 0)
            return BadRequest("لا يوجد درجات لتحديثها.");

        var reviewerId = GetCurrentReviewerId(); // المقيّم الحالي
        var revieweeId = req.UserId;             // المستخدم الذي يتم تقييمه

        var questionIds = req.Items
            .Where(x => x.QuestionId > 0)
            .Select(x => x.QuestionId)
            .Distinct()
            .ToList();

        if (!questionIds.Any())
            return BadRequest("لا توجد أسئلة صحيحة للتحديث.");

        // تأكيد وجود الأسئلة
        var existingQuestions = await _db.Questions
            .Where(q => questionIds.Contains(q.Id))
            .Select(q => q.Id)
            .ToListAsync(ct);

        if (existingQuestions.Count != questionIds.Count)
        {
            var found = existingQuestions.ToHashSet();
            var missing = questionIds.Where(id => !found.Contains(id));
            return BadRequest("أسئلة غير موجودة: " + string.Join(", ", missing));
        }

        // السكورات الحالية لنفس (المُقيَّم + المقيِّم + الأسئلة)
        var existingScores = await _db.AnswerScores
            .Where(s =>
                s.RevieweeUserId == revieweeId &&
                s.ReviewerUserId == reviewerId &&
                questionIds.Contains(s.QuestionId))
            .ToListAsync(ct);

        var byQuestionId = existingScores.ToDictionary(s => s.QuestionId);
        var now = DateTime.UtcNow;

        foreach (var item in req.Items)
        {
            // ممكن يكون null => نسمح بس بنحط 0 لو حاب
            var scoreValue = item.Score ?? 0m;

            if (byQuestionId.TryGetValue(item.QuestionId, out var s))
            {
                // UPDATE
                s.Score = scoreValue;
                s.Notes = item.Notes;
                s.ScoredAt = now;
            }
            else
            {
                // INSERT
                _db.AnswerScores.Add(new Domain.Entities.AnswerScore
                {
                    QuestionId = item.QuestionId,
                    RevieweeUserId = revieweeId,
                    ReviewerUserId = reviewerId,
                    Score = scoreValue,
                    Notes = item.Notes,
                    ScoredAt = now
                });
            }
        }

        await _db.SaveChangesAsync(ct);

        // يكفي OK بسيط لأن الـ MVC ما يقرأ البودي، فقط EnsureSuccessStatusCode
        return Ok(new { success = true });
    }

}
