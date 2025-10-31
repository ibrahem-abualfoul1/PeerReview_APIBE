using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PeerReview.Application.Abstractions;
using PeerReview.Application.DTOs;
using PeerReview.Domain.Entities;
using PeerReview.Infrastructure.Persistence;
using BCrypt.Net;

namespace PeerReview.Api.Controllers;
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _cfg;
    private readonly IJwtTokenService _jwt;
    public AuthController(AppDbContext db, IConfiguration cfg, IJwtTokenService jwt) { _db = db; _cfg = cfg; _jwt = jwt; }

    [HttpPost("register")]
    public async Task<ActionResult> Register(RegisterRequest req)
    {
        if (await _db.Users.AnyAsync(u => u.UserName == req.UserName)) return BadRequest("Username already exists");

        var role = await _db.Roles.FindAsync(req.RoleId ?? _db.Roles.Where(r => r.Name=="User").Select(r=>r.Id).First());
        if (role == null || role.Name == "Admin") role = await _db.Roles.FirstAsync(r => r.Name == "User");

        var user = new User
        {
            UserName = req.UserName, FullName = req.FullName, Email = req.Email,
            RoleId = role.Id, PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password)
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return Ok(new { user.Id, user.UserName, Role = role.Name });
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest req)
    {
        var user = await _db.Users.Include(u=>u.Role).FirstOrDefaultAsync(u => u.UserName == req.UserName && u.IsActive);
        if (user == null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash)) return Unauthorized();
        var token = _jwt.CreateToken(user, user.Role?.Name ?? "User", TimeSpan.FromMinutes(int.Parse(_cfg["Jwt:ExpiresMinutes"]!)));
        return new LoginResponse(token, user.UserName, user.Role?.Name ?? "User");
    }
}
