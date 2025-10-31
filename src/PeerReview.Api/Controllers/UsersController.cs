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
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;
    public UsersController(AppDbContext db) => _db = db;

    [HttpGet] public async Task<ActionResult<IEnumerable<object>>> GetAll() =>
        Ok(await _db.Users.Include(u=>u.Role).Select(u=> new { u.Id, u.UserName, u.FullName, u.Email, u.IsActive, Role = u.Role!.Name }).ToListAsync());

    [HttpGet("{id:int}")]
    public async Task<ActionResult<object>> GetById(int id)
    {
        var u = await _db.Users.Include(u=>u.Role).FirstOrDefaultAsync(u=>u.Id==id);
        if (u == null) return NotFound();
        return Ok(u);
    }

    [HttpPost]
    public async Task<ActionResult> Create(UserCreateDto dto)
    {
        var user = new User { UserName=dto.UserName, FullName=dto.FullName, Email=dto.Email, RoleId=dto.RoleId, PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password) };
        _db.Users.Add(user); await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, new { user.Id });
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> Update(int id, UserUpdateDto dto)
    {
        var u = await _db.Users.FindAsync(id); if (u==null) return NotFound();
        u.FullName = dto.FullName; u.Email = dto.Email; u.IsActive = dto.IsActive; u.RoleId = dto.RoleId;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id:int}/activate")] public async Task<ActionResult> Activate(int id){ var u=await _db.Users.FindAsync(id); if(u==null) return NotFound(); u.IsActive=true; await _db.SaveChangesAsync(); return NoContent(); }
    [HttpPost("{id:int}/deactivate")] public async Task<ActionResult> Deactivate(int id){ var u=await _db.Users.FindAsync(id); if(u==null) return NotFound(); u.IsActive=false; await _db.SaveChangesAsync(); return NoContent(); }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> SoftDelete(int id){ var u=await _db.Users.FindAsync(id); if(u==null) return NotFound(); u.IsDeleted=true; u.DeletedAt=DateTime.UtcNow; await _db.SaveChangesAsync(); return NoContent(); }
}
