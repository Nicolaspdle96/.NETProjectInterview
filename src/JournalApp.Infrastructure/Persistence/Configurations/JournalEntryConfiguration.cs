using JournalApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JournalApp.Infrastructure.Persistence.Configurations;

public class JournalEntryConfiguration : IEntityTypeConfiguration<JournalEntry>
{
    public void Configure(EntityTypeBuilder<JournalEntry> builder)
    {
        builder.ToTable("JournalEntries");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.Title)
            .HasColumnType($"nvarchar({JournalEntry.TitleMaxLength})")
            .IsRequired();

        // nvarchar(n) caps at 4000, so the 5000-character limit is enforced by the domain.
        builder.Property(e => e.Content)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(e => e.Mood)
            .HasConversion<string>()
            .HasColumnType("nvarchar(20)");

        builder.Property(e => e.CreatedAt).HasColumnType("datetime2");
        builder.Property(e => e.UpdatedAt).HasColumnType("datetime2");

        builder.HasIndex(e => e.UserId);
    }
}
