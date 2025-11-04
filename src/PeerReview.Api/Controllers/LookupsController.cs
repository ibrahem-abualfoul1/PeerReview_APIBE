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
public class LookupsController : ControllerBase
{
    private readonly AppDbContext _db;
    public LookupsController(AppDbContext db) => _db = db;

    // GET: api/Lookups
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Lookup>>> GetAll(CancellationToken ct)
    {
        var list = await _db.Lookups
            .AsNoTracking()
            .Include(x => x.SubLookups)
            .ToListAsync(ct);

        return Ok(list);
    }

    // GET: api/Lookups/{code}
    [HttpGet("{code}")]
    public async Task<ActionResult<object>> GetById(string code, CancellationToken ct)
    {
        var l = await _db.Lookups
            .AsNoTracking()
            .Include(x => x.SubLookups)
            .FirstOrDefaultAsync(x => x.Code == code, ct);

        if (l is null) return NotFound();

        return Ok(new
        {
            l.Id,
            l.NameAr,
            l.NameEn,

            l.TypeAr,
            l.TypeEn,

            l.Code,
            SubLookups = l.SubLookups.Select(s => new { s.Id, s.NameEn, s.NameAr })
        });
    }

    // POST: api/Lookups
    [HttpPost]
    public async Task<ActionResult> Create([FromBody] LookupCreateDto dto, CancellationToken ct)
    {
       

        var l = new Lookup
        {
            NameAr = dto.NameAr,
            TypeEn = dto.TypeEn,
            NameEn = dto.NameEn,
            TypeAr = dto.TypeAr,
            Code = dto.Code
        };

        _db.Lookups.Add(l);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { code = l.Code }, new { l.Id, l.NameAr, l.TypeAr, l.NameEn, l.TypeEn, l.Code });
    }

    // PUT: api/Lookups/{code}
    [HttpPut("{code}")]
    public async Task<ActionResult> Update(string code, [FromBody] LookupUpdateDto dto, CancellationToken ct)
    {
        var l = await _db.Lookups.FirstOrDefaultAsync(x => x.Code == code, ct);
        if (l is null) return NotFound();



        l.NameAr = dto.NameAr;
        l.TypeAr = dto.TypeAr;

        l.NameEn = dto.NameEn;
        l.TypeEn = dto.TypeAr;

        l.Code = dto.Code; // اسمح بتحديث الكود (أو احذفه لو بدك تثبّت الكود)

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // DELETE: api/Lookups/{code}
    [HttpDelete("{code}")]
    public async Task<ActionResult> Delete(string code, CancellationToken ct)
    {
        var l = await _db.Lookups
            .Include(x => x.SubLookups)
            .FirstOrDefaultAsync(x => x.Code == code, ct);

        if (l is null) return NotFound();

        if (l.SubLookups.Any())
            return BadRequest("Cannot delete lookup that has SubLookups.");

        l.IsDeleted = true;
        l.DeletedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // POST: api/Lookups/{code}/sub
    [HttpPost("{code}/sub")]
    public async Task<ActionResult> CreateSub(string code, [FromBody] SubLookupCreateDto dto, CancellationToken ct)
    {
        // dto.Code هو كود الـ Lookup الأب حسب تصميمك
        if (!string.Equals(code, dto.Code, StringComparison.OrdinalIgnoreCase))
            return BadRequest("Lookup code mismatch.");

        var lookup = await _db.Lookups.FirstOrDefaultAsync(l => l.Code == code, ct);
        if (lookup is null) return NotFound("Parent lookup not found.");

        var s = new SubLookup { LookupId = lookup.Id, NameAr = dto.NameAr , NameEn = dto.NameEn };
        _db.SubLookups.Add(s);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetSubById), new { id = s.Id }, new { s.Id, s.NameEn, s.NameAr, s.LookupId });
    }

    [HttpPut("sub/{id:int}")]
    public async Task<ActionResult> UpdateSub(int id, [FromBody] SubLookupUpdateDto dto, CancellationToken ct)
    {
        var s = await _db.SubLookups.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (s is null) return NotFound();

        var lookup = await _db.Lookups.FirstOrDefaultAsync(l => l.Code == dto.Code, ct);
        if (lookup is null) return NotFound("Target parent lookup not found.");

        s.NameAr = dto.NameAr;
        s.NameEn = dto.NameEn;

        s.LookupId = lookup.Id;

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("sub/{id:int}")]
    public async Task<ActionResult> DeleteSub(int id, CancellationToken ct)
    {
        var s = await _db.SubLookups.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (s is null) return NotFound();

        s.IsDeleted = true;
        s.DeletedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpGet("{id:int}/sub")]
    public async Task<ActionResult<IEnumerable<SubLookup>>> GetSubAll(int id, CancellationToken ct)
    {
        var list = await _db.SubLookups
            .AsNoTracking()
            .Where(x => x.LookupId == id)
            .ToListAsync(ct);

        return Ok(list);
    }

    [HttpGet("{id:int}/getsub")]
    public async Task<ActionResult<object>> GetSubById(int id, CancellationToken ct)
    {
        var l = await _db.SubLookups
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (l is null) return NotFound();
        return Ok(new { l.Id, l.NameAr, l.NameEn, l.LookupId });
    }
}
