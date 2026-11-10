namespace VbeSort.Infrastructure;

/// <summary>
/// Laiko abstrakcija. Visa "dienu" logika (serijos, dienos tikslas, kartojimu grafikas)
/// skaiciuojama Lietuvos laiku, o duomenu bazeje laikas saugomas UTC.
/// </summary>
public interface IClock
{
    DateTime UtcNow { get; }
    TimeZoneInfo TimeZone { get; }
    DateOnly Today { get; }
    DateTime ToLocal(DateTime utc);
    DateOnly ToLocalDate(DateTime utc);
    DateTime StartOfDayUtc(DateOnly localDate);
}

public class SystemClock : IClock
{
    public SystemClock() : this(LithuanianTime.Zone) { }

    public SystemClock(TimeZoneInfo zone) => TimeZone = zone;

    public virtual DateTime UtcNow => DateTime.UtcNow;
    public TimeZoneInfo TimeZone { get; }
    public DateOnly Today => ToLocalDate(UtcNow);

    public DateTime ToLocal(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), TimeZone);

    public DateOnly ToLocalDate(DateTime utc) => DateOnly.FromDateTime(ToLocal(utc));

    public DateTime StartOfDayUtc(DateOnly localDate)
    {
        var localMidnight = localDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(localMidnight, TimeZone);
    }
}

/// <summary>Fiksuoto laiko laikrodis testams.</summary>
public sealed class FixedClock : SystemClock
{
    private DateTime _utcNow;

    public FixedClock(DateTime utcNow, TimeZoneInfo? zone = null) : base(zone ?? LithuanianTime.Zone)
        => _utcNow = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);

    public override DateTime UtcNow => _utcNow;

    public void Advance(TimeSpan by) => _utcNow = _utcNow.Add(by);
}

public static class LithuanianTime
{
    public static readonly TimeZoneInfo Zone = Resolve();

    private static TimeZoneInfo Resolve()
    {
        foreach (var id in new[] { "Europe/Vilnius", "FLE Standard Time" })
        {
            if (TimeZoneInfo.TryFindSystemTimeZoneById(id, out var tz)) return tz;
        }
        // Atsarginis variantas, jei sistemoje nera laiko zonu duomenu.
        return TimeZoneInfo.CreateCustomTimeZone("LT", TimeSpan.FromHours(2), "Lietuvos laikas", "Lietuvos laikas");
    }
}
