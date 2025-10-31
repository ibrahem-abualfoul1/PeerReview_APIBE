using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using PeerReview.Domain.Entities;
using PeerReview.Infrastructure.Persistence;

namespace PeerReview.Infrastructure.Seed;
public static class SeedData
{
    public static async Task Run(AppDbContext db)
    {
        if (!await db.Roles.AnyAsync())
        {
            db.Roles.AddRange(
                new Role { Name = "Admin", CanSeeAllUsers = true, CanSeeSystemStats = true, CanSeeAssignmentsAll = true, CanSeeAnswersAll = true },
                new Role { Name = "Client" },
                new Role { Name = "Customer" },
                new Role { Name = "User" }
            );
            await db.SaveChangesAsync();
        }
        if (!await db.Users.AnyAsync())
        {
            var adminRole = await db.Roles.FirstAsync(r => r.Name == "Admin");
            db.Users.Add(new User {
                UserName = "admin",
                FullName = "Administrator",
                Email = "admin@example.com",
                RoleId = adminRole.Id,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123")
            });
            await db.SaveChangesAsync();
        }
    }
}
