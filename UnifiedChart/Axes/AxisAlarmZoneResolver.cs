namespace UnifiedChart;

public static class AxisAlarmZoneResolver
{
    public static IEnumerable<(double From, double To)> Resolve(AxisAlarmZone zone, double axisMinimum, double axisMaximum)
    {
        if (zone.Mode == AlarmZoneMode.Explicit)
        {
            yield return Normalize(zone.From, zone.To);
            yield break;
        }

        var lower = zone.LowerLimit ?? zone.From;
        var upper = zone.UpperLimit ?? zone.To;
        if (lower > upper)
        {
            (lower, upper) = (upper, lower);
        }

        if (zone.Mode == AlarmZoneMode.Inside)
        {
            yield return Normalize(lower, upper);
            yield break;
        }

        if (axisMinimum < lower)
        {
            yield return Normalize(axisMinimum, lower);
        }

        if (upper < axisMaximum)
        {
            yield return Normalize(upper, axisMaximum);
        }
    }

    private static (double From, double To) Normalize(double from, double to)
        => from <= to ? (from, to) : (to, from);
}
