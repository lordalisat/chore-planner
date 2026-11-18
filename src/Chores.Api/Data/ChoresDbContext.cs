using Chores.Domain;
using Microsoft.EntityFrameworkCore;

namespace Chores.Api.Data;

/// <summary>Maps the Domain model to SQLite; keys are shadow properties, so the Domain stays free of them.</summary>
public sealed class ChoresDbContext(DbContextOptions<ChoresDbContext> options) : DbContext(options)
{
    public DbSet<Area> Areas => Set<Area>();
    public DbSet<Chore> Chores => Set<Chore>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Chore>(chore =>
        {
            chore.Property<int>("Id");
            chore.HasKey("Id");
            // Get-only properties, here and in Interval and Season, are not mapped by convention
            chore.Property(c => c.CreatedOn);
            chore.HasDiscriminator<string>("Kind")
                .HasValue<RecurringChore>("Recurring")
                .HasValue<OneOffChore>("OneOff");
            chore.HasOne(c => c.Area).WithMany().HasForeignKey("AreaId");
            // A Skip is a Completion with Skipped set, so done and skipped share the table
            chore.OwnsMany(c => c.Completions, completion =>
            {
                completion.ToTable("Completions");
                completion.Property<int>("Id");
                completion.HasKey("Id");
            });
            chore.Navigation(c => c.Completions).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<RecurringChore>(chore =>
        {
            // EF never passes null to a converter
            chore.Property(c => c.PreferredDay).HasConversion(day => ToCode(day!), code => FromCode(code));
            chore.ComplexProperty(c => c.Interval, interval =>
            {
                interval.Property(i => i.Count);
                interval.Property(i => i.Unit);
            });
            chore.ComplexProperty(c => c.Season, season =>
            {
                season.Property(s => s.FromMonth);
                season.Property(s => s.ToMonth);
            });
        });

        modelBuilder.Entity<Area>(area =>
        {
            area.Property<int>("Id");
            area.HasKey("Id");
            area.HasOne(a => a.Parent).WithMany().HasForeignKey("ParentId");
        });
    }

    // A Preferred Day is stored as its weekday's number, or 7 and 8 for the first and last of the month
    private static int ToCode(PreferredDay day) => day switch
    {
        PreferredDay.Weekday weekday => (int)weekday.Day,
        PreferredDay.FirstOfMonth => 7,
        PreferredDay.LastOfMonth => 8,
        _ => throw new ArgumentOutOfRangeException(nameof(day), day, null),
    };

    private static PreferredDay FromCode(int code) => code switch
    {
        7 => new PreferredDay.FirstOfMonth(),
        8 => new PreferredDay.LastOfMonth(),
        _ => (DayOfWeek)code,
    };
}
