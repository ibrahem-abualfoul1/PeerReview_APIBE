using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PeerReview.Application.DTOs;
using PeerReview.Domain.Entities;
using PeerReview.Infrastructure.Persistence;
using System.Linq;

namespace PeerReview.Api.Controllers;
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class LookupsController : ControllerBase
{
    private readonly AppDbContext _db;
    public LookupsController(AppDbContext db) => _db = db;

    [HttpGet] public async Task<ActionResult<IEnumerable<Lookup>>> GetAll() => await _db.Lookups.Include(x=>x.SubLookups).ToListAsync();

    [HttpGet("{id:int}")]
    public async Task<ActionResult<object>> GetById(int id)
    {
        var l = await _db.Lookups.Include(l=>l.SubLookups).FirstOrDefaultAsync(l=>l.Id==id);
        if (l==null) return NotFound();
        return Ok(new { l.Id, l.Name, l.Type, SubLookups = l.SubLookups.Select(s=> new { s.Id, s.Name }) });
    }

    [HttpPost] public async Task<ActionResult> Create(LookupCreateDto dto){ var l=new Lookup{ Name=dto.Name, Type=dto.Type }; _db.Lookups.Add(l); await _db.SaveChangesAsync(); return CreatedAtAction(nameof(GetById), new { id=l.Id }, new { l.Id }); }
    [HttpPut("{id:int}")] public async Task<ActionResult> Update(int id, LookupUpdateDto dto){ var l=await _db.Lookups.FindAsync(id); if(l==null) return NotFound(); l.Name=dto.Name; l.Type=dto.Type; await _db.SaveChangesAsync(); return NoContent(); }
    [HttpDelete("{id:int}")] public async Task<ActionResult> Delete(int id){ var l=await _db.Lookups.Include(x=>x.SubLookups).FirstOrDefaultAsync(x=>x.Id==id); if(l==null) return NotFound(); if (l.SubLookups.Any()) return BadRequest("Cannot delete lookup that has SubLookups"); l.IsDeleted=true; l.DeletedAt=DateTime.UtcNow; await _db.SaveChangesAsync(); return NoContent(); }

    [HttpPost("{lookupId:int}/sub")] public async Task<ActionResult> CreateSub(int lookupId, SubLookupCreateDto dto){ if (lookupId!=dto.LookupId) return BadRequest("LookupId mismatch"); var s=new SubLookup{ LookupId=dto.LookupId, Name=dto.Name }; _db.SubLookups.Add(s); await _db.SaveChangesAsync(); return Ok(new { s.Id }); }
    [HttpPut("sub/{id:int}")] public async Task<ActionResult> UpdateSub(int id, SubLookupUpdateDto dto){ var s=await _db.SubLookups.FindAsync(id); if(s==null) return NotFound(); s.Name=dto.Name; s.LookupId = dto.LookupId; await _db.SaveChangesAsync(); return NoContent(); }
    [HttpDelete("sub/{id:int}")] public async Task<ActionResult> DeleteSub(int id){ var s=await _db.SubLookups.FindAsync(id); if(s==null) return NotFound(); s.IsDeleted=true; s.DeletedAt=DateTime.UtcNow; await _db.SaveChangesAsync(); return NoContent(); }


    [HttpGet("{id:int}/sub")] public async Task<ActionResult<IEnumerable<SubLookup>>> GetSubAll(int id) => await _db.SubLookups.Where(x=>x.LookupId == id).ToListAsync();
    
    [HttpGet("{id:int}/getsub")]
    public async Task<ActionResult<object>> GetSubById(int id)
    {
        var l = await _db.SubLookups.FirstOrDefaultAsync(l => l.Id == id);
        if (l == null) return NotFound();
        return Ok(new { l.Id, l.Name, l.LookupId } );
    }
}
