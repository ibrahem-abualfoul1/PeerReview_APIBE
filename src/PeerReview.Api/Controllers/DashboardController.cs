using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using PeerReview.Application.DTOs;
using PeerReview.Infrastructure.Persistence;

namespace PeerReview.Api.Controllers;
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _db;
    public DashboardController(AppDbContext db) => _db = db;
    int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<ActionResult<DashboardDto>> Get()
    {
        var me = await _db.Users.Include(u=>u.Role).FirstAsync(u=>u.Id==CurrentUserId);
        var dto = new DashboardDto();

        var assignedToMe = await _db.Assignments.CountAsync(a => a.UserId == me.Id && a.IsActive);
        var answeredByMe = await _db.Answers.CountAsync(a => a.UserId == me.Id);
        var myPending = assignedToMe - await _db.Answers.Where(a=>a.UserId==me.Id).Select(a=>a.QuestionId).Distinct().CountAsync();
        dto.Metrics["AssignedToMe"] = assignedToMe;
        dto.Metrics["AnsweredByMe"] = answeredByMe;
        dto.Metrics["MyPending"] = myPending < 0 ? 0 : myPending;

        if (me.Role!.CanSeeAllUsers) dto.Metrics["TotalUsers"] = await _db.Users.CountAsync();
        if (me.Role!.CanSeeSystemStats) dto.Metrics["TotalQuestions"] = await _db.Questions.CountAsync();
        if (me.Role!.CanSeeAssignmentsAll) dto.Metrics["TotalAssignments"] = await _db.Assignments.CountAsync();
        if (me.Role!.CanSeeAnswersAll) dto.Metrics["TotalAnswers"] = await _db.Answers.CountAsync();
        return dto;
    }
}
