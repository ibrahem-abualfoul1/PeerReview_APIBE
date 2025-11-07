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
        return await _db.Answers.Include(x => x.Question).Include(x => x.QuestionItem).Include(x => x.User).ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult> Create(AnswerCreateDto dto)
    {
        var a = await _db.Answers.Include(x => x.Question).Include(x => x.QuestionItem).Include(x => x.User).FirstOrDefaultAsync(x => x.UserId == CurrentUserId && x.QuestionId == dto.QuestionId && x.QuestionItemId == dto.QuestionItemId);
        if (a == null) { a = new Answer { UserId = CurrentUserId, QuestionId = dto.QuestionId, QuestionItemId = dto.QuestionItemId, Value = dto.Value, SubmittedAt = DateTime.UtcNow }; _db.Answers.Add(a); }
        else { a.Value = dto.Value; a.SubmittedAt = DateTime.UtcNow; }
        await _db.SaveChangesAsync(); return Ok(new { a.Id });
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
        if (file == null || file.Length == 0) return BadRequest("Empty file");
        var (rel, length, contentType) = await _files.SaveAsync(file.FileName, file.OpenReadStream(), file.ContentType);
        var fe = new FileEntry { FileName = file.FileName, ContentType = contentType, Length = length, Path = System.IO.Path.Combine("uploads", rel).Replace("\\", "/"), UploadedByUserId = CurrentUserId };
        _db.FileEntries.Add(fe);
        await _db.SaveChangesAsync();

        var a = await _db.Answers.FirstOrDefaultAsync(x => x.UserId == CurrentUserId && x.QuestionId == questionId && x.QuestionItemId == questionItemId);
        if (a == null) { a = new Answer { UserId = CurrentUserId, QuestionId = questionId, QuestionItemId = questionItemId, FileId = fe.Id, SubmittedAt = DateTime.UtcNow }; _db.Answers.Add(a); }
        else { a.FileId = fe.Id; a.SubmittedAt = DateTime.UtcNow; }
        await _db.SaveChangesAsync();
        return Ok(new { a.Id, FileId = fe.Id, FileUrl = "/uploads/" + rel });
    }


    [HttpGet("getByUserId")]
    public async Task<ActionResult<IEnumerable<Answer>>> GetByUserId()
    {
        return await _db.Answers.Include(x => x.Question).Include(x => x.QuestionItem).Include(x => x.User).Where(x=>x.UserId == CurrentUserId).ToListAsync();
    }
}
