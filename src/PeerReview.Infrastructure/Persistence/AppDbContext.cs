using Microsoft.EntityFrameworkCore;
using PeerReview.Domain.Entities;

namespace PeerReview.Infrastructure.Persistence
{
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
        public DbSet<AnswerScore> AnswerScores => Set<AnswerScore>();
        public DbSet<AnswerFile> AnswerFiles => Set<AnswerFile>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ===== Users =====
            modelBuilder.Entity<User>(b =>
            {
                b.Property(x => x.UserName).IsRequired().HasMaxLength(64);
                b.Property(x => x.FullName).IsRequired().HasMaxLength(128);
                b.Property(x => x.Email).IsRequired().HasMaxLength(256);

                b.HasIndex(u => u.UserName)
                 .IsUnique()
                 .HasFilter("[IsDeleted] = 0");
            });

            // ===== QuestionItems =====
            modelBuilder.Entity<QuestionItem>(b =>
            {
                b.Property(x => x.TextEn)
                 .IsRequired()
                 .HasMaxLength(256)
                 .HasConversion(v => v == null ? null : v.Trim(), v => v);

                b.Property(x => x.OptionsCsvEn)
                 .HasMaxLength(1024)
                 .HasConversion(v => v == null ? null : v.Trim(), v => v);
            });

            // ===== Many-to-Many: Question ↔ QuestionItem =====
            modelBuilder.Entity<Question>()
                .HasMany(q => q.Items)
                .WithMany(i => i.Questions)
                .UsingEntity<Dictionary<string, object>>(
                    "QuestionQuestionItems",
                    right => right
                        .HasOne<QuestionItem>()
                        .WithMany()
                        .HasForeignKey("QuestionItemId")
                        .OnDelete(DeleteBehavior.Restrict),
                    left => left
                        .HasOne<Question>()
                        .WithMany()
                        .HasForeignKey("QuestionId")
                        .OnDelete(DeleteBehavior.Cascade),
                    join =>
                    {
                        join.ToTable("QuestionQuestionItems");
                        join.HasKey("QuestionId", "QuestionItemId");
                        join.HasIndex("QuestionId");
                        join.HasIndex("QuestionItemId");
                    }
                );

            // ===== Assignments =====
            modelBuilder.Entity<Assignment>(b =>
            {
                b.HasIndex(a => new { a.QuestionId, a.UserId })
                 .IsUnique()
                 .HasFilter("[IsDeleted] = 0");

                b.HasIndex(a => a.UserId);
                b.HasIndex(a => a.QuestionId);
            });

            // ===== Lookups =====
            modelBuilder.Entity<Lookup>(b =>
            {
                b.Property(l => l.NameEn)
                 .IsRequired()
                 .HasMaxLength(128)
                 .HasConversion(v => v == null ? null : v.Trim(), v => v);

                b.Property(l => l.TypeEn)
                 .IsRequired()
                 .HasMaxLength(64)
                 .HasConversion(v => v == null ? null : v.Trim(), v => v);

                b.Property(l => l.Code)
                 .IsRequired()
                 .HasMaxLength(64)
                 .HasConversion(
                     v => v == null ? null : v.Trim().ToUpperInvariant(),
                     v => v
                 );

                b.HasIndex(l => new { l.TypeEn, l.NameEn })
                 .IsUnique()
                 .HasFilter("[IsDeleted] = 0");

                b.HasIndex(l => l.Code)
                 .IsUnique()
                 .HasFilter("[IsDeleted] = 0");
            });

            // ===== SubLookups =====
            modelBuilder.Entity<SubLookup>(b =>
            {
                b.Property(s => s.NameEn)
                 .IsRequired()
                 .HasMaxLength(128)
                 .HasConversion(v => v == null ? null : v.Trim(), v => v);

                b.HasOne(s => s.Lookup)
                 .WithMany(l => l.SubLookups)
                 .HasForeignKey(s => s.LookupId)
                 .OnDelete(DeleteBehavior.Cascade);

                b.HasIndex(s => new { s.LookupId, s.NameEn })
                 .IsUnique()
                 .HasFilter("[IsDeleted] = 0");
            });

            // ===== FileEntries =====
            modelBuilder.Entity<FileEntry>(b =>
            {
                b.Property(f => f.FileName).IsRequired().HasMaxLength(256);
                b.Property(f => f.ContentType).IsRequired().HasMaxLength(128);
            });

            // ===== Answers =====
            modelBuilder.Entity<Answer>(b =>
            {
                // فهارس مساعدة
                b.HasIndex(a => a.QuestionId);
                b.HasIndex(a => a.UserId);
                b.HasIndex(a => a.QuestionItemId);

                // Answer ↔ Question (Required)
                b.HasOne(a => a.Question)
                 .WithMany()
                 .HasForeignKey(a => a.QuestionId)
                 .OnDelete(DeleteBehavior.Restrict);

                // Answer ↔ User (Required)
                b.HasOne(a => a.User)
                 .WithMany()
                 .HasForeignKey(a => a.UserId)
                 .OnDelete(DeleteBehavior.Restrict);

                // Answer ↔ QuestionItem (Optional)
                b.HasOne(a => a.QuestionItem)
                 .WithMany()
                 .HasForeignKey(a => a.QuestionItemId)
                 .OnDelete(DeleteBehavior.SetNull);
            });

            // ===== AnswerScores =====
            modelBuilder.Entity<AnswerScore>(b =>
            {
                b.HasIndex(x => x.AnswerId);
                b.HasIndex(x => new { x.AnswerId, x.ReviewerUserId })
                 .IsUnique()
                 .HasFilter("[IsDeleted] = 0");

                b.Property(x => x.Score).HasColumnType("decimal(10,2)");

                b.HasOne(x => x.Answer)
                 .WithMany()
                 .HasForeignKey(x => x.AnswerId)
                 .OnDelete(DeleteBehavior.Cascade);

                b.HasOne(x => x.Reviewer)
                 .WithMany()
                 .HasForeignKey(x => x.ReviewerUserId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // ===== AnswerFiles =====
            modelBuilder.Entity<AnswerFile>(b =>
            {
                b.HasOne(af => af.Answer)
                 .WithMany(a => a.Files)
                 .HasForeignKey(af => af.AnswerId)
                 .OnDelete(DeleteBehavior.Cascade);

                b.HasOne(af => af.File)
                 .WithMany()
                 .HasForeignKey(af => af.FileId)
                 .OnDelete(DeleteBehavior.Cascade);

                b.HasIndex(af => af.AnswerId);
                b.HasIndex(af => af.FileId);
            });

            // ===== Global soft-delete filters =====
            modelBuilder.Entity<User>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Question>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<QuestionItem>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Answer>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Lookup>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<SubLookup>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<FileEntry>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<AnswerFile>().HasQueryFilter(e => !e.IsDeleted);

            base.OnModelCreating(modelBuilder);
        }
    }
}
