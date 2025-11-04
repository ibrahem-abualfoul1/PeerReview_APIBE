using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PeerReview.Application.DTOs;
using PeerReview.Domain.Entities;
using PeerReview.Domain.Enums;
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
    public async Task<ActionResult<IEnumerable<QuestionDto>>> GetAll(CancellationToken ct)
    {
        var items = await _db.Questions
            .AsNoTracking()
            .Select(q => new QuestionDto(
                q.Id,
                q.TitleAr,
                q.DescriptionAr,
                q.TitleEn,
                q.DescriptionEn,
                q.CategoryId,
                q.Category != null ? q.Category.NameEn : null,
                q.Items
                    .OrderBy(i => i.Id) // أو Order لو ضفته
                    .Select(i => new QuestionItemDto(
                        i.Id,
                        i.TextAr,
                         i.TextEn,
                        i.Type,
                        i.IsRequired,
                        i.OptionsCsvAr ,i.OptionsCsvEn
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
                q.Id,
                q.TitleAr,
                q.DescriptionAr,
                 q.TitleEn,
                q.DescriptionEn,
                q.CategoryId,
                q.Category != null ? q.Category.NameEn : null,  // <-- CategoryName
                q.Items
                    .OrderBy(i => i.Id) // أو Order إن أضفته لاحقًا
                    .Select(i => new QuestionItemDto(
                        i.Id,
                        i.TextAr, i.TextEn,

                        i.Type,
                        i.IsRequired,
                        i.OptionsCsvAr, i.OptionsCsvEn
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

        var q = new Question
        {
            TitleAr = dto.TitleAr,
            DescriptionAr = dto.DescriptionAr ?? "",
            TitleEn = dto.TitleEn,
            DescriptionEn = dto.DescriptionEn ?? "",
            CategoryId = dto.CategoryId
        };

        foreach (var it in dto.Items)
        {
            q.Items.Add(new QuestionItem
            {
                TextAr = it.TextAr,
                TextEn = it.TextEn,

                Type = it.Type,
                IsRequired = it.IsRequired,
                OptionsCsvAr = it.OptionsCsvAr,
                OptionsCsvEn = it.OptionsCsvEn
            });
        }

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

        var existing = q.Items.Where(i => !i.IsDeleted).ToDictionary(i => i.Id);

        var seenIds = new HashSet<int>();
        foreach (var it in dto.Items)
        {
            if (it.Id is null or 0)
            {
                q.Items.Add(new QuestionItem
                {
                    TextAr = it.TextAr,
                    TextEn = it.TextEn,

                    Type = it.Type,
                    IsRequired = it.IsRequired,
                    OptionsCsvAr = it.OptionsCsvAr,
                    OptionsCsvEn = it.OptionsCsvEn
                });
            }
            else if (existing.TryGetValue(it.Id.Value, out var entity))
            {
                entity.TextAr = it.TextAr;
                entity.TextEn = it.TextEn;

                entity.Type = it.Type;
                entity.IsRequired = it.IsRequired;
                entity.OptionsCsvEn = it.OptionsCsvEn;
                entity.OptionsCsvAr = it.OptionsCsvAr;

                seenIds.Add(entity.Id);
            }
            else
            {
                return BadRequest($"Item Id {it.Id} غير موجود تحت هذا السؤال.");
            }
        }

        foreach (var e in existing.Values.Where(x => !seenIds.Contains(x.Id)))
        {
            e.IsDeleted = true;
            e.DeletedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }



    [HttpDelete("{id:int}")]
    public async Task<ActionResult> SoftDelete(int id, CancellationToken ct)
    {
        var q = await _db.Questions
            .Include(x => x.Items) 
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (q is null)
            return NotFound();

        
        q.IsDeleted = true;
        q.DeletedAt = DateTime.UtcNow;

       
        foreach (var item in q.Items.Where(i => !i.IsDeleted))
        {
            item.IsDeleted = true;
            item.DeletedAt = DateTime.UtcNow;
        }

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



}
