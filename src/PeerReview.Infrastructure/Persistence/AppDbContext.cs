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

            // ===== Questions =====
            // ===== Questions =====
            modelBuilder.Entity<Question>(b =>
            {
                b.Property(x => x.TitleAr).IsRequired().HasMaxLength(256)
                 .HasConversion(v => v == null ? null : v.Trim(), v => v);

                b.Property(x => x.TitleEn).IsRequired().HasMaxLength(256)
                 .HasConversion(v => v == null ? null : v.Trim(), v => v);

                b.Property(x => x.DescriptionAr).HasMaxLength(2000)
                 .HasConversion(v => v == null ? null : v.Trim(), v => v);

                b.Property(x => x.DescriptionEn).HasMaxLength(2000)
                 .HasConversion(v => v == null ? null : v.Trim(), v => v);

                // Category مطلوبة
                b.HasOne(x => x.Category)
                 .WithMany()
                 .HasForeignKey(x => x.CategoryId)
                 .OnDelete(DeleteBehavior.Restrict);

                // SubCategory اختيارية (يكفي جعل FK nullable؛ ويمكن صراحةً وضع IsRequired(false))
                b.HasOne(x => x.SubCategory)
                 .WithMany()
                 .HasForeignKey(x => x.SubCategoryId)
                 .OnDelete(DeleteBehavior.Restrict); // أو SetNull إذا تحب
                                                     // .IsRequired(false); // (اختياري توضيحيًا)

                b.HasIndex(x => x.CategoryId);
                b.HasIndex(x => x.SubCategoryId);
            });


            // ===== QuestionItems =====
            modelBuilder.Entity<QuestionItem>(b =>
            {
                b.Property(x => x.TextAr)
                 .IsRequired()
                 .HasMaxLength(256)
                 .HasConversion(v => v == null ? null : v.Trim(), v => v);

                b.Property(x => x.TextEn)
                 .IsRequired()
                 .HasMaxLength(256)
                 .HasConversion(v => v == null ? null : v.Trim(), v => v);

                b.Property(x => x.OptionsCsvAr)
                 .HasMaxLength(1024)
                 .HasConversion(v => v == null ? null : v.Trim(), v => v);

                b.Property(x => x.OptionsCsvEn)
                 .HasMaxLength(1024)
                 .HasConversion(v => v == null ? null : v.Trim(), v => v);

                b.HasOne(qi => qi.Question)
                 .WithMany(q => q.Items)
                 .HasForeignKey(qi => qi.QuestionId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

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
                b.Property(l => l.NameAr)
                 .IsRequired()
                 .HasMaxLength(128)
                 .HasConversion(v => v == null ? null : v.Trim(), v => v);

                b.Property(l => l.NameEn)
                 .IsRequired()
                 .HasMaxLength(128)
                 .HasConversion(v => v == null ? null : v.Trim(), v => v);

                b.Property(l => l.TypeAr)
                 .IsRequired()
                 .HasMaxLength(64)
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

                b.HasIndex(l => new { l.TypeAr, l.NameAr })
                 .IsUnique()
                 .HasFilter("[IsDeleted] = 0");

                b.HasIndex(l => l.Code)
                 .IsUnique()
                 .HasFilter("[IsDeleted] = 0");
            });

            // ===== SubLookups =====
            modelBuilder.Entity<SubLookup>(b =>
            {
                b.Property(s => s.NameAr)
                 .IsRequired()
                 .HasMaxLength(128)
                 .HasConversion(v => v == null ? null : v.Trim(), v => v);

                b.Property(s => s.NameEn)
                 .IsRequired()
                 .HasMaxLength(128)
                 .HasConversion(v => v == null ? null : v.Trim(), v => v);

                b.HasOne(s => s.Lookup)
                 .WithMany(l => l.SubLookups)
                 .HasForeignKey(s => s.LookupId)
                 .OnDelete(DeleteBehavior.Cascade);

                b.HasIndex(s => new { s.LookupId, s.NameAr })
                 .IsUnique()
                 .HasFilter("[IsDeleted] = 0");

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
