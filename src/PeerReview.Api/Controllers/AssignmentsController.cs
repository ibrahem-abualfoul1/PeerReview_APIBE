using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PeerReview.Application.DTOs;
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
        foreach(var qId in req.QuestionIds.Distinct())
        foreach(var uId in req.UserIds.Distinct())
            if (!await _db.Assignments.AnyAsync(a=>a.QuestionId==qId && a.UserId==uId))
                _db.Assignments.Add(new Domain.Entities.Assignment{ QuestionId=qId, UserId=uId });
        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpGet("by-user/{userId:int}")]
    //[Authorize]
    public async Task<ActionResult<IEnumerable<object>>> ByUser(int userId)
    {
        var list = await _db.Assignments
    .Where(a => a.UserId == userId && a.IsActive)
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

    [HttpGet("by-question/{questionId:int}")]
    public async Task<ActionResult<object>> ByQuestion(int questionId)
    {
        var cnt = await _db.Assignments.CountAsync(a=>a.QuestionId==questionId && a.IsActive);
        var users = await _db.Assignments.Where(a=>a.QuestionId==questionId && a.IsActive)
            .Join(_db.Users, a=>a.UserId, u=>u.Id, (a,u)=> new { u.Id, u.UserName, u.FullName }).ToListAsync();
        return Ok(new { Count = cnt, Users = users });
    }

    [HttpPost("{id:int}/deactivate")] public async Task<ActionResult> Deactivate(int id){ var a=await _db.Assignments.FindAsync(id); if(a==null) return NotFound(); a.IsActive=false; await _db.SaveChangesAsync(); return NoContent(); }
    [HttpPost("{id:int}/activate")] public async Task<ActionResult> Activate(int id){ var a=await _db.Assignments.FindAsync(id); if(a==null) return NotFound(); a.IsActive=true; await _db.SaveChangesAsync(); return NoContent(); }

    [HttpGet("GetAll")]
    //[Authorize]
    public async Task<ActionResult<IEnumerable<object>>> GetAll()
    {
        var list = await _db.Assignments
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
