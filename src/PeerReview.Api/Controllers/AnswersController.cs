using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PeerReview.Application.Abstractions;
using PeerReview.Application.DTOs;
using PeerReview.Domain.Entities;
using PeerReview.Infrastructure.Persistence;
using System.Security.Claims;

namespace PeerReview.Api.Controllers;
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AnswersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IFileStorage _files;
    public AnswersController(AppDbContext db, IFileStorage files) { _db = db; _files = files; }
    int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("mine")]
    public async Task<ActionResult<IEnumerable<Answer>>> Mine()
    {
        var list = await _db.Answers
            .Include(x => x.Question)
            .Include(x => x.QuestionItem)
            .Include(x => x.User)
            .Include(x => x.Files).ThenInclude(x=>x.File)
            .ToListAsync();

        return list;
    }

    [HttpPost]
    public async Task<ActionResult> Create(List<AnswerCreateDto> dtoList)
    {
        if (dtoList == null || !dtoList.Any())
            return BadRequest("لا توجد بيانات للإدخال.");

        foreach (var dto in dtoList)
        {
            var existing = await _db.Answers
                .FirstOrDefaultAsync(x =>
                    x.UserId == CurrentUserId &&
                    x.QuestionId == dto.QuestionId &&
                    x.QuestionItemId == dto.QuestionItemId);

            if (existing == null)
            {
                var newAnswer = new Answer
                {
                    UserId = CurrentUserId,
                    QuestionId = dto.QuestionId,
                    QuestionItemId = dto.QuestionItemId,
                    Value = dto.Value,
                    SubmittedAt = DateTime.UtcNow
                };
                _db.Answers.Add(newAnswer);
            }
            else
            {
                existing.Value = dto.Value;
                existing.SubmittedAt = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = "تم حفظ الإجابات بنجاح." });
    }


    [HttpPut("{id:int}")]
    public async Task<ActionResult> Update(int id, AnswerUpdateDto dto)
    {
        var a = await _db.Answers.FindAsync(id);
        if (a == null || a.UserId != CurrentUserId)
            return NotFound();
        a.Value = dto.Value;
        a.SubmittedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var a = await _db.Answers.FindAsync(id);
        if (a == null || a.UserId != CurrentUserId)
            return NotFound();
        a.IsDeleted = true;
        a.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("upload")]
    [RequestSizeLimit(20_000_000)]
    public async Task<ActionResult> Upload([FromForm] int questionId, [FromForm] int? questionItemId, IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest("Empty file");

            // 1) نجيب الـ Answer الحالي (لو موجود) مع الملفات المرتبطة
            var answer = await _db.Answers
                .Include(a => a.Files)
                    .ThenInclude(af => af.File)
                .FirstOrDefaultAsync(a =>
                    a.UserId == CurrentUserId &&
                    a.QuestionId == questionId &&
                    a.QuestionItemId == questionItemId
                );

            // لو ما في Answer نعمل واحد جديد
            if (answer == null)
            {
                answer = new Answer
                {
                    UserId = CurrentUserId,
                    QuestionId = questionId,
                    QuestionItemId = questionItemId,
                    SubmittedAt = DateTime.UtcNow
                };

                _db.Answers.Add(answer);
                await _db.SaveChangesAsync(); // عشان نضمن أن الـ Id اتولد
            }

            // ✅ **مهم**:
            // ما عاد نحذف أي ملفات قديمة
            // كل Upload راح يضيف ملف جديد فوق الموجودين

            // 2) حفظ الملف الجديد في التخزين الفيزيائي
            var (rel, length, contentType) = await _files.SaveAsync(
                file.FileName,
                file.OpenReadStream(),
                file.ContentType
            );

            // 3) إنشاء FileEntry جديد
            var fe = new FileEntry
            {
                FileName = file.FileName,
                ContentType = contentType,
                Length = length,
                Path = Path.Combine("uploads", rel).Replace("\\", "/"),
                UploadedByUserId = CurrentUserId
            };

            _db.FileEntries.Add(fe);
            await _db.SaveChangesAsync();

            // 4) ربطه بالـ Answer عن طريق AnswerFile (إضافة جديدة بدون حذف القديم)
            var answerFile = new AnswerFile
            {
                AnswerId = answer.Id,
                FileId = fe.Id
            };

            _db.AnswerFiles.Add(answerFile);

            // تحديث وقت الإرسال
            answer.SubmittedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return Ok(new
            {
                AnswerId = answer.Id,
                FileId = fe.Id,
                FileUrl = "/uploads/" + rel
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message, stack = ex.StackTrace });
        }
    }




    [HttpGet("getByUserId")]
    public async Task<ActionResult<IEnumerable<Answer>>> GetByUserId()
    {
        var list = await _db.Answers
            .Include(x => x.Question)
            .Include(x => x.QuestionItem)
            .Include(x => x.User)
            .Include(x => x.Files).ThenInclude(f => f.File)
            .Where(x => x.UserId == CurrentUserId)
            .ToListAsync();

        return list;
    }

}
