namespace UnifiedChart;

public static class AxisMarkerDirection
{
    public static (double X, double Y) Resolve(string? value, bool isXAxis)
    {
        var direction = (value ?? string.Empty).Trim().ToLowerInvariant();
        return direction switch
        {
            "north" or "up" => (0, -1),
            "south" or "down" => (0, 1),
            "east" or "right" => (1, 0),
            "west" or "left" => (-1, 0),
            "inside" => isXAxis ? (0, -1) : (1, 0),
            "outside" => isXAxis ? (0, 1) : (-1, 0),
            _ => isXAxis ? (0, 1) : (-1, 0)
        };
    }
}
