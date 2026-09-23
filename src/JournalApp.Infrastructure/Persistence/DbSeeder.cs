using JournalApp.Application.Common.Interfaces;
using JournalApp.Domain.Entities;
using JournalApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace JournalApp.Infrastructure.Persistence;

/// <summary>
/// Applies pending migrations and loads demo data when the database is empty.
/// </summary>
public static class DbSeeder
{
    public const string DemoEmail = "demo@journal.com";
    public const string DemoPassword = "Demo123!";
    public const string AlexEmail = "alex@journal.com";
    public const string AlexPassword = "Alex123!";

    public static async Task SeedAsync(
        AppDbContext context, IPasswordHasher passwordHasher, TimeProvider timeProvider, CancellationToken cancellationToken)
    {
        await context.Database.MigrateAsync(cancellationToken);

        if (await context.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;

        var demo = User.Create("demo", DemoEmail, passwordHasher.Hash(DemoPassword), now.AddDays(-30));
        var alex = User.Create("alex", AlexEmail, passwordHasher.Hash(AlexPassword), now.AddDays(-20));
        context.Users.AddRange(demo, alex);

        context.JournalEntries.AddRange(
            JournalEntry.Create(demo.Id, "First day of the new job",
                "Met the team today. Everyone was welcoming and I already have a small task to work on. " +
                "Feeling nervous but excited about what's coming.",
                Mood.Great, now.AddDays(-14).AddHours(-3)),
            JournalEntry.Create(demo.Id, "Rainy Sunday",
                "Stayed in all day reading and drinking tea. Not much happened, which was exactly what I needed.",
                Mood.Neutral, now.AddDays(-9).AddHours(-5)),
            JournalEntry.Create(demo.Id, "Missed the train",
                "Overslept, missed the train and arrived late to the planning meeting. " +
                "Tomorrow I'm setting two alarms.",
                Mood.Bad, now.AddDays(-6).AddHours(-2)),
            JournalEntry.Create(demo.Id, "Weekend hike",
                "Hiked to the lake with friends. The view from the top was worth every step. " +
                "Legs are sore but the mind is clear.",
                Mood.Good, now.AddDays(-3).AddHours(-7)),
            JournalEntry.Create(demo.Id, "A rough one",
                "Nothing seemed to go right today: a failed deploy, a long bug hunt and a headache. " +
                "Writing it down helps to let it go.",
                Mood.Awful, now.AddHours(-4)),
            JournalEntry.Create(alex.Id, "Alex's private note",
                "This entry belongs to Alex and must never be visible to the demo user.",
                Mood.Good, now.AddDays(-2)),
            JournalEntry.Create(alex.Id, "Cooking experiment",
                "Tried a new curry recipe. Too spicy, but I'll try again next week.",
                null, now.AddDays(-1)));

        await context.SaveChangesAsync(cancellationToken);
    }
}
