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
                

                b.Property(x => x.TextEn).IsRequired().HasMaxLength(256)
                 .HasConversion(v => v == null ? null : v.Trim(), v => v);

                

                b.Property(x => x.OptionsCsvEn).HasMaxLength(1024)
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
                        .OnDelete(DeleteBehavior.Restrict),   // لا تحذف الـ Item عند حذف سؤال
                    left => left
                        .HasOne<Question>()
                        .WithMany()
                        .HasForeignKey("QuestionId")
                        .OnDelete(DeleteBehavior.Cascade),    // احذف روابط السؤال عند حذفه
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
                b.HasIndex(a => a.FileId);

                // Answer ↔ Question (Required)
                b.HasOne(a => a.Question)
                 .WithMany() // إن كان عندك ICollection<Answer> Answers في Question يمكنك استبدالها بـ .WithMany(q => q.Answers)
                 .HasForeignKey(a => a.QuestionId)
                 .OnDelete(DeleteBehavior.Restrict);

                // Answer ↔ User (Required)
                b.HasOne(a => a.User)
                 .WithMany() // إن كان عندك ICollection<Answer> Answers في User يمكنك استبدالها بـ .WithMany(u => u.Answers)
                 .HasForeignKey(a => a.UserId)
                 .OnDelete(DeleteBehavior.Restrict);

                // Answer ↔ QuestionItem (Optional)
                b.HasOne(a => a.QuestionItem)
                 .WithMany() // إن أضفت ICollection<Answer> Answers في QuestionItem استبدلها بـ .WithMany(qi => qi.Answers)
                 .HasForeignKey(a => a.QuestionItemId)
                 .OnDelete(DeleteBehavior.SetNull);

                // Answer ↔ FileEntry (Optional)
                b.HasOne(a => a.File)
                 .WithMany()
                 .HasForeignKey(a => a.FileId)
                 .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<AnswerScore>(b =>
            {
                b.HasIndex(x => x.AnswerId);
                b.HasIndex(x => new { x.AnswerId, x.ReviewerUserId }).IsUnique()
                 .HasFilter("[IsDeleted] = 0"); // لو EntityBase عندك فيه IsDeleted

                b.Property(x => x.Score).HasColumnType("decimal(10,2)");

                b.HasOne(x => x.Answer)
                 .WithMany() // ما نضيف Collection في Answer حتى لا نعدل الكيان
                 .HasForeignKey(x => x.AnswerId)
                 .OnDelete(DeleteBehavior.Cascade);

                b.HasOne(x => x.Reviewer)
                 .WithMany()
                 .HasForeignKey(x => x.ReviewerUserId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // ===== Global soft-delete filters =====
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
}
