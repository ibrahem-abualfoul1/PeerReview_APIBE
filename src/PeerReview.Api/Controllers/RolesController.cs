using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PeerReview.Domain.Entities;
using PeerReview.Infrastructure.Persistence;

namespace PeerReview.Api.Controllers;
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class RolesController : ControllerBase
{
    private readonly AppDbContext _db;
    public RolesController(AppDbContext db) => _db = db;

    [HttpGet] public async Task<ActionResult<IEnumerable<Role>>> GetAll() => await _db.Roles.ToListAsync();
    [HttpGet("{id:int}")] public async Task<ActionResult<Role>> GetById(int id) => await _db.Roles.FindAsync(id) is Role r ? r : NotFound();
    [HttpPost] public async Task<ActionResult> Create(Role r){ _db.Roles.Add(r); await _db.SaveChangesAsync(); return CreatedAtAction(nameof(GetById), new { id=r.Id }, r); }
    [HttpPut("{id:int}")] public async Task<ActionResult> Update(int id, Role r){ var e=await _db.Roles.FindAsync(id); if(e==null) return NotFound(); e.Name=r.Name; e.CanSeeAllUsers=r.CanSeeAllUsers; e.CanSeeSystemStats=r.CanSeeSystemStats; e.CanSeeAssignmentsAll=r.CanSeeAssignmentsAll; e.CanSeeAnswersAll=r.CanSeeAnswersAll; await _db.SaveChangesAsync(); return NoContent(); }
}
