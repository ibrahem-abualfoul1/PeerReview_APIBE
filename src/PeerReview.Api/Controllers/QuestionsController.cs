using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PeerReview.Application.DTOs;
using PeerReview.Domain.Entities;
using PeerReview.Domain.Enums;
using PeerReview.Infrastructure.Persistence;
using System.Linq;

namespace PeerReview.Api.Controllers;
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class QuestionsController : ControllerBase
{
    private readonly AppDbContext _db;
    public QuestionsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<QuestionDto>>> GetAll(CancellationToken ct)
    {
        var items = await _db.Questions
            .AsNoTracking()
            .Select(q => new QuestionDto(
                q.Id, q.TitleAr, q.DescriptionAr, q.TitleEn, q.DescriptionEn,
                q.CategoryId, q.SubCategoryId,
                q.Category != null ? q.Category.NameEn : null,
                q.SubCategory != null ? q.SubCategory.NameEn : null,
                q.Items
                    .OrderBy(i => i.Id)
                    .Select(i => new QuestionItemDto(
                        i.Id, i.TextAr, i.TextEn, i.Type, i.IsRequired, i.OptionsCsvAr, i.OptionsCsvEn
                    ))
                    .ToList()
            ))
            .ToListAsync(ct);

        return Ok(items);
    }


    [HttpGet("{id:int}")]
    public async Task<ActionResult<QuestionDto>> GetById(int id, CancellationToken ct)
    {
        var dto = await _db.Questions
            .AsNoTracking()
            .Where(q => q.Id == id)
            .Select(q => new QuestionDto(
                q.Id, q.TitleAr, q.DescriptionAr, q.TitleEn, q.DescriptionEn,
                q.CategoryId, q.SubCategoryId,
                q.Category != null ? q.Category.NameEn : null,
                q.SubCategory != null ? q.SubCategory.NameEn : null,
                q.Items
                    .OrderBy(i => i.Id)
                    .Select(i => new QuestionItemDto(
                        i.Id, i.TextAr, i.TextEn, i.Type, i.IsRequired, i.OptionsCsvAr, i.OptionsCsvEn
                    ))
                    .ToList()
            ))
            .FirstOrDefaultAsync(ct);

        if (dto is null) return NotFound();
        return Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] QuestionCreateDto dto, CancellationToken ct)
    {
        if (dto.Items is null || dto.Items.Count == 0)
            return BadRequest("يجب إضافة عنصر واحد على الأقل للسؤال.");

        var categoryExists = await _db.Lookups.AnyAsync(l => l.Id == dto.CategoryId, ct);
        if (!categoryExists) return BadRequest("CategoryId غير صالح.");

        var q = new Question
        {
            TitleAr = dto.TitleAr,
            DescriptionAr = dto.DescriptionAr ?? "",
            TitleEn = dto.TitleEn,
            DescriptionEn = dto.DescriptionEn ?? "",
            CategoryId = dto.CategoryId,
            SubCategoryId = dto.SubCategoryId
        };

        var itemIds = dto.Items.Distinct().ToList();
        var items = await _db.QuestionItems.Where(i => itemIds.Contains(i.Id)).ToListAsync(ct);
        if (items.Count != itemIds.Count)
        {
            var found = items.Select(i => i.Id).ToHashSet();
            var missing = itemIds.Where(id => !found.Contains(id));
            return BadRequest($"عناصر غير موجودة: {string.Join(", ", missing)}");
        }

        foreach (var it in items) q.Items.Add(it);

        _db.Questions.Add(q);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = q.Id }, new { q.Id });
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> Update(int id, [FromBody] QuestionUpdateDto dto, CancellationToken ct)
    {
        var q = await _db.Questions
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (q is null) return NotFound();

        var categoryExists = await _db.Lookups.AnyAsync(l => l.Id == dto.CategoryId, ct);
        if (!categoryExists) return BadRequest("CategoryId غير صالح.");

        q.TitleAr = dto.TitleAr;
        q.DescriptionAr = dto.DescriptionAr ?? "";
        q.TitleEn = dto.TitleEn;
        q.DescriptionEn = dto.DescriptionEn ?? "";
        q.CategoryId = dto.CategoryId;
        q.SubCategoryId = dto.SubCategoryId; // ← التصحيح

        var incomingIds = (dto.Items ?? new List<int>()).Distinct().ToHashSet();
        var currentIds = q.Items.Select(i => i.Id).ToHashSet();

        // to add
        var toAddIds = incomingIds.Except(currentIds).ToList();
        if (toAddIds.Count > 0)
        {
            var toAdd = await _db.QuestionItems.Where(i => toAddIds.Contains(i.Id)).ToListAsync(ct);
            if (toAdd.Count != toAddIds.Count)
            {
                var found = toAdd.Select(i => i.Id).ToHashSet();
                var missing = toAddIds.Where(id => !found.Contains(id));
                return BadRequest($"عناصر غير موجودة: {string.Join(", ", missing)}");
            }
            foreach (var it in toAdd) q.Items.Add(it);
        }

        // to remove
        var toRemove = q.Items.Where(i => !incomingIds.Contains(i.Id)).ToList();
        foreach (var it in toRemove) q.Items.Remove(it);

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }




    [HttpDelete("{id:int}")]
    public async Task<ActionResult> SoftDelete(int id, CancellationToken ct)
    {
        var q = await _db.Questions.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (q is null) return NotFound();

        q.IsDeleted = true;
        q.DeletedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }


    [HttpGet("QuestionType")]
    public async Task<ActionResult> GetQuestionType()
    {
        var list = Enum.GetValues(typeof(QuestionType))
            .Cast<QuestionType>()
            .Select(e => new
            {
                Id = (int)e,
                Name = e.ToString()
            });

        return Ok(list);
    }

    [HttpGet("GetQuestionCategories")]
    public async Task<ActionResult> GetQuestionCategories(CancellationToken ct)
    {
        var categories = await _db.QuestionItems
            .AsNoTracking()
            .ToListAsync(ct);

        return Ok(categories);
    }


}
