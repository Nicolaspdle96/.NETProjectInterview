using JournalApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JournalApp.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedNever();

        builder.Property(u => u.Username)
            .HasColumnType($"nvarchar({User.UsernameMaxLength})")
            .IsRequired();

        builder.Property(u => u.Email)
            .HasColumnType($"nvarchar({User.EmailMaxLength})")
            .IsRequired();

        builder.Property(u => u.PasswordHash)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(u => u.CreatedAt).HasColumnType("datetime2");

        builder.HasIndex(u => u.Email).IsUnique();
        builder.HasIndex(u => u.Username).IsUnique();

        builder.HasMany(u => u.Entries)
            .WithOne()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
