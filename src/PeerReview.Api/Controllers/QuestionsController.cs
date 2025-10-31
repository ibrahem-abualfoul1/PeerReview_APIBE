using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PeerReview.Application.DTOs;
using PeerReview.Domain.Entities;
using PeerReview.Infrastructure.Persistence;

namespace PeerReview.Api.Controllers;
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class QuestionsController : ControllerBase
{
    private readonly AppDbContext _db;
    public QuestionsController(AppDbContext db) => _db = db;

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<QuestionDto>>> GetAll()
    {
        var items = await _db.Questions.Include(q=>q.Items).Select(q => new QuestionDto(
            q.Id, q.Title, q.Description, q.CategoryId,
            q.Items.Select(i => new QuestionItemDto(i.Id, i.Text, i.Type, i.IsRequired, i.OptionsCsv, i.ParentItemId, i.ShowWhenValue)).ToList()
        )).ToListAsync();
        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<QuestionDto>> GetById(int id)
    {
        var q = await _db.Questions.Include(q=>q.Items).FirstOrDefaultAsync(q=>q.Id==id);
        if (q == null) return NotFound();
        return new QuestionDto(q.Id, q.Title, q.Description, q.CategoryId,
            q.Items.Select(i => new QuestionItemDto(i.Id, i.Text, i.Type, i.IsRequired, i.OptionsCsv, i.ParentItemId, i.ShowWhenValue)).ToList());
    }

    [HttpPost]
    public async Task<ActionResult> Create(QuestionCreateDto dto)
    {
        var q = new Question { Title = dto.Title, Description = dto.Description, CategoryId = dto.CategoryId };
        foreach(var it in dto.Items)
            q.Items.Add(new QuestionItem { Text = it.Text, Type = it.Type, IsRequired = it.IsRequired, OptionsCsv = it.OptionsCsv, ParentItemId = it.ParentItemId, ShowWhenValue = it.ShowWhenValue });
        _db.Questions.Add(q); await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = q.Id }, new { q.Id });
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> Update(int id, QuestionUpdateDto dto)
    {
        var q = await _db.Questions.Include(x=>x.Items).FirstOrDefaultAsync(x=>x.Id==id);
        if (q == null) return NotFound();
        q.Title = dto.Title; q.Description = dto.Description; q.CategoryId = dto.CategoryId;
        foreach(var it in q.Items){ it.IsDeleted = true; it.DeletedAt = DateTime.UtcNow; }
        foreach(var it in dto.Items)
            _db.QuestionItems.Add(new QuestionItem { QuestionId = q.Id, Text = it.Text, Type = it.Type, IsRequired = it.IsRequired, OptionsCsv = it.OptionsCsv, ParentItemId = it.ParentItemId, ShowWhenValue = it.ShowWhenValue });
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> SoftDelete(int id){ var q=await _db.Questions.FindAsync(id); if(q==null) return NotFound(); q.IsDeleted=true; q.DeletedAt=DateTime.UtcNow; await _db.SaveChangesAsync(); return NoContent(); }
}
