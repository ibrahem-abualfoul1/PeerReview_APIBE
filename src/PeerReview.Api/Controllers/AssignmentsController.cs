using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PeerReview.Application.DTOs;
using PeerReview.Domain.Entities;
using PeerReview.Infrastructure.Persistence;

namespace PeerReview.Api.Controllers;
[ApiController]
[Route("api/[controller]")]
//[Authorize(Roles = "Admin")]
public class AssignmentsController : ControllerBase
{
    private readonly AppDbContext _db;
    public AssignmentsController(AppDbContext db) => _db = db;

    [HttpPost("bulk")]
    public async Task<ActionResult> BulkAssign(AssignRequest req)
    {
        foreach (var qId in req.QuestionIds.Distinct())
        {
            foreach (var uId in req.UserIds.Distinct())
            {
                // ابحث عن الـ Assignment الموجود
                var existing = await _db.Assignments
                    .FirstOrDefaultAsync(a => a.QuestionId == qId && a.UserId == uId);

                if (existing != null)
                {
                    // لو موجود وكان Active → خلّيه بحاله
                    if (existing.IsActive)
                        continue;

                    // لو موجود لكنه Deactivated → فعّله
                    existing.IsActive = true;
                    existing.AssignedAt = DateTime.UtcNow;
                }
                else
                {
                    // لو مش موجود → أضفه
                    _db.Assignments.Add(new Assignment
                    {
                        QuestionId = qId,
                        UserId = uId,
                        IsActive = true
                    });
                }
            }
        }

        await _db.SaveChangesAsync();
        return Ok();
    }


    [HttpGet("by-user/{userId:int}")]
    public async Task<ActionResult<IEnumerable<object>>> ByUser(int userId)
    {
        var list = await _db.Assignments
            .Where(a => a.UserId == userId && a.IsActive)

            // فقط الأسئلة التي تحتوي Item واحد على الأقل غير مُجاب
            .Where(a =>
                a.Question.Items.Any(qi =>
                    !_db.Answers.Any(ans =>
                        ans.UserId == userId &&
                        ans.QuestionId == a.QuestionId &&
                        ans.QuestionItemId == qi.Id
                    )
                )
            )

            .Select(a => new
            {
                assignmentId = a.Id,
                questionId = a.QuestionId,
                userId = a.UserId,
                assignedAt = a.AssignedAt,
                isActive = a.IsActive,

                question = new
                {
                    id = a.Question.Id,
                    titleEn = a.Question.TitleEn,
                    descriptionEn = a.Question.DescriptionEn,

                    categoryId = a.Question.CategoryId,
                    categoryName = a.Question.Category.NameEn,

                    subCategoryId = a.Question.SubCategoryId,
                    subCategoryName = a.Question.SubCategory != null
                        ? a.Question.SubCategory.NameEn
                        : null,

                    // 🟡 أهم جزء: فقط الـ Items غير المجاوَبة
                    items = a.Question.Items
                        .Where(qi =>
                            !_db.Answers.Any(ans =>
                                ans.UserId == userId &&
                                ans.QuestionId == a.QuestionId &&
                                ans.QuestionItemId == qi.Id
                            )
                        )
                        .Select(qi => new
                        {
                            id = qi.Id,
                            textEn = qi.TextEn,
                            type = (int)qi.Type,
                            isRequired = qi.IsRequired,
                            optionsCsvEn = qi.OptionsCsvEn
                        })
                        .ToList()
                }
            })

            .AsNoTracking()
            .ToListAsync();

        return Ok(list);
    }


    [HttpGet("by-question/{questionId:int}")]
    public async Task<ActionResult<object>> ByQuestion(int questionId)
    {
        var cnt = await _db.Assignments.CountAsync(a => a.QuestionId == questionId && a.IsActive);
        var users = await _db.Assignments.Where(a => a.QuestionId == questionId && a.IsActive)
            .Join(_db.Users, a => a.UserId, u => u.Id, (a, u) => new { u.Id, u.UserName, u.FullName }).ToListAsync();
        return Ok(new { Count = cnt, Users = users });
    }

    [HttpPost("{id:int}/deactivate")] public async Task<ActionResult> Deactivate(int id) { var a = await _db.Assignments.FindAsync(id); if (a == null) return NotFound(); a.IsActive = false; await _db.SaveChangesAsync(); return NoContent(); }
    [HttpPost("{id:int}/activate")] public async Task<ActionResult> Activate(int id) { var a = await _db.Assignments.FindAsync(id); if (a == null) return NotFound(); a.IsActive = true; await _db.SaveChangesAsync(); return NoContent(); }

    [HttpGet("GetAll")]
    //[Authorize]
    public async Task<ActionResult<IEnumerable<object>>> GetAll()
    {
        var list = await _db.Assignments
                .Include(a => a.User)

    .Include(a => a.Question)
        .ThenInclude(q => q.Category)
      .Include(a => a.Question)
        .ThenInclude(q => q.Items)
     .Include(a => a.Question)
        .ThenInclude(q => q.SubCategory)
    .AsNoTracking()
    .ToListAsync();
        return Ok(list);
    }


}
