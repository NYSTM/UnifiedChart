namespace UnifiedChart;

public static class MissingValueContract
{
    public static bool IsMissing(double value, double? hole)
        => double.IsNaN(value) || double.IsInfinity(value) || hole.HasValue && value == hole.Value;

    public static IEnumerable<IReadOnlyList<(double X, double Y)>> SplitSegments(
        IEnumerable<(double X, double Y)> points,
        double? hole)
    {
        var segment = new List<(double X, double Y)>();
        foreach (var point in points)
        {
            if (IsMissing(point.X, hole) || IsMissing(point.Y, hole))
            {
                if (segment.Count > 0)
                {
                    yield return segment;
                    segment = new();
                }
                continue;
            }

            segment.Add(point);
        }

        if (segment.Count > 0)
        {
            yield return segment;
        }
    }
}
