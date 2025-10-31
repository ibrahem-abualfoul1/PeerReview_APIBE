using Microsoft.EntityFrameworkCore;
using PeerReview.Domain.Entities;

namespace PeerReview.Infrastructure.Persistence;
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<QuestionItem> QuestionItems => Set<QuestionItem>();
    public DbSet<Answer> Answers => Set<Answer>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<Lookup> Lookups => Set<Lookup>();
    public DbSet<SubLookup> SubLookups => Set<SubLookup>();
    public DbSet<FileEntry> FileEntries => Set<FileEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasIndex(u => u.UserName).IsUnique();
        modelBuilder.Entity<QuestionItem>().HasOne(qi => qi.Question).WithMany(q => q.Items).HasForeignKey(qi => qi.QuestionId);
        modelBuilder.Entity<Assignment>().HasIndex(a => new { a.QuestionId, a.UserId }).IsUnique();

        // Global soft-delete filters
        modelBuilder.Entity<User>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Question>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<QuestionItem>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Answer>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Lookup>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<SubLookup>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<FileEntry>().HasQueryFilter(e => !e.IsDeleted);

        base.OnModelCreating(modelBuilder);
    }
}
