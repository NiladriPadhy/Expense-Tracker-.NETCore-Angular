using ExpenseTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseTracker.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("Users");
        b.HasKey(x => x.Id);
        b.Property(x => x.FullName).IsRequired().HasMaxLength(120);
        b.Property(x => x.Email).IsRequired().HasMaxLength(254);
        b.Property(x => x.Phone).IsRequired().HasMaxLength(32);
        b.Property(x => x.PasswordHash).IsRequired().HasMaxLength(200);
        b.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
        b.Property(x => x.Role).HasConversion<int>();
        b.HasIndex(x => x.Email).IsUnique();
        b.HasIndex(x => x.Phone).IsUnique();
        b.HasIndex(x => x.IsDeleted);
        b.HasOne(x => x.Photo)
            .WithOne()
            .HasForeignKey<User>(x => x.PhotoId)
            .OnDelete(DeleteBehavior.SetNull);
        b.HasOne<Currency>()
            .WithMany()
            .HasForeignKey(x => x.CurrencyCode)
            .HasPrincipalKey(c => c.Code)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class UserProfilePhotoConfiguration : IEntityTypeConfiguration<UserProfilePhoto>
{
    public void Configure(EntityTypeBuilder<UserProfilePhoto> b)
    {
        b.ToTable("UserProfilePhotos");
        b.HasKey(x => x.Id);
        b.Property(x => x.ContentType).IsRequired().HasMaxLength(64);
        b.Property(x => x.Data).IsRequired();
        b.HasIndex(x => x.UserId).IsUnique();
    }
}

public sealed class CurrencyConfiguration : IEntityTypeConfiguration<Currency>
{
    public void Configure(EntityTypeBuilder<Currency> b)
    {
        b.ToTable("Currencies");
        b.HasKey(x => x.Code);
        b.Property(x => x.Code).HasMaxLength(3);
        b.Property(x => x.Name).IsRequired().HasMaxLength(64);
        b.Property(x => x.Symbol).IsRequired().HasMaxLength(8);
        b.HasIndex(x => x.IsActive);
    }
}

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> b)
    {
        b.ToTable("Categories");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(80);
        b.Property(x => x.Type).HasConversion<int>();
        b.HasIndex(x => new { x.Type, x.Name }).IsUnique();
        b.HasIndex(x => x.IsActive);
    }
}

public sealed class EntryConfiguration : IEntityTypeConfiguration<Entry>
{
    public void Configure(EntityTypeBuilder<Entry> b)
    {
        b.ToTable("Entries");
        b.HasKey(x => x.Id);
        b.Property(x => x.Amount).HasPrecision(18, 2);
        b.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
        b.Property(x => x.CategoryNameSnapshot).HasMaxLength(80);
        b.Property(x => x.Note).HasMaxLength(500);
        b.Property(x => x.Type).HasConversion<int>();
        b.HasIndex(x => new { x.UserId, x.EntryDate });
        b.HasIndex(x => x.CategoryId);
        b.HasOne<Category>()
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);
        b.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class MonthlySummaryConfiguration : IEntityTypeConfiguration<MonthlySummary>
{
    public void Configure(EntityTypeBuilder<MonthlySummary> b)
    {
        b.ToTable("MonthlySummaries");
        b.HasKey(x => new { x.UserId, x.Year, x.Month });
        b.Property(x => x.OpeningBalance).HasPrecision(18, 2);
        b.Property(x => x.TotalIncome).HasPrecision(18, 2);
        b.Property(x => x.TotalExpense).HasPrecision(18, 2);
        b.Property(x => x.ClosingBalance).HasPrecision(18, 2);
        b.Property(x => x.SavingsRatePct).HasPrecision(7, 2);
        b.Property(x => x.StatusColor).HasConversion<int>();
        b.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
    }
}

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.ToTable("RefreshTokens");
        b.HasKey(x => x.Id);
        b.Property(x => x.TokenHash).IsRequired().HasMaxLength(200);
        b.HasIndex(x => x.TokenHash).IsUnique();
        b.HasIndex(x => x.UserId);
    }
}
