using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManager.Domain.Tasks;

namespace TaskManager.Infrastructure.Persistence.Configurations;

internal sealed class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    private const int StatusMaxLength = 20;

    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("Tasks");

        builder.HasKey(task => task.Id);
        builder.Property(task => task.Id).ValueGeneratedNever();

        builder.Property(task => task.Title).IsRequired().HasMaxLength(TaskItem.TitleMaxLength);
        builder.Property(task => task.Description).HasMaxLength(TaskItem.DescriptionMaxLength);

        builder.Property(task => task.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(StatusMaxLength);

        builder.Property(task => task.CreatedAt).IsRequired();

        builder.HasIndex(task => task.UserId);
        builder.HasIndex(task => new { task.UserId, task.Status });
    }
}
