namespace UnifiedChart;

public readonly record struct LabelPlacementRect(double Left, double Top, double Width, double Height)
{
    public double Right => Left + Width;
    public double Bottom => Top + Height;

    public bool Contains(LabelPlacementRect other)
        => other.Left >= Left && other.Top >= Top && other.Right <= Right && other.Bottom <= Bottom;

    public double IntersectionArea(LabelPlacementRect other)
    {
        var width = Math.Min(Right, other.Right) - Math.Max(Left, other.Left);
        var height = Math.Min(Bottom, other.Bottom) - Math.Max(Top, other.Top);
        return width > 0 && height > 0 ? width * height : 0;
    }
}

public static class ChartLabelPlacement
{
    private const double Gap = 8;

    public static LabelPlacementRect Find(
        double pointX,
        double pointY,
        double width,
        double height,
        ChartLabel label,
        LabelPlacementRect plotArea,
        IReadOnlyList<LabelPlacementRect> occupied)
    {
        var candidates = label.Auto
            ? new[]
            {
                new LabelPlacementRect(pointX + Gap, pointY - height - Gap, width, height),
                new LabelPlacementRect(pointX + Gap, pointY + Gap, width, height),
                new LabelPlacementRect(pointX - width - Gap, pointY - height - Gap, width, height),
                new LabelPlacementRect(pointX - width - Gap, pointY + Gap, width, height)
            }
            : new[] { new LabelPlacementRect(pointX + label.OffsetX, pointY + label.OffsetY, width, height) };

        if (!label.Auto)
        {
            return candidates[0];
        }

        var best = candidates[0];
        var bestScore = double.PositiveInfinity;
        foreach (var candidate in candidates)
        {
            var outside = OutsideArea(candidate, plotArea);
            var overlap = occupied.Sum(candidate.IntersectionArea);
            var score = outside * 1_000_000 + overlap;
            if (score < bestScore)
            {
                best = candidate;
                bestScore = score;
            }
            if (outside == 0 && overlap == 0)
            {
                return candidate;
            }
        }

        return best;
    }

    private static double OutsideArea(LabelPlacementRect value, LabelPlacementRect area)
    {
        var insideWidth = Math.Max(0, Math.Min(value.Right, area.Right) - Math.Max(value.Left, area.Left));
        var insideHeight = Math.Max(0, Math.Min(value.Bottom, area.Bottom) - Math.Max(value.Top, area.Top));
        return value.Width * value.Height - insideWidth * insideHeight;
    }
}
