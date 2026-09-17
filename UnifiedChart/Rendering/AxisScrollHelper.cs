namespace UnifiedChart;

public static class AxisScrollHelper
{
    public static AxisRange Scroll(AxisRange current, double direction, double? unit, double minimum, double maximum)
    {
        var amount = unit is > 0 ? unit.Value : current.Span * 0.2;
        var lower = Math.Min(minimum, maximum);
        var upper = Math.Max(minimum, maximum);
        var span = Math.Min(current.Span, upper - lower);
        if (span <= 0) return new AxisRange(lower, upper);

        var targetMin = current.Min + amount * direction;
        var targetMax = targetMin + span;
        if (targetMin < lower) targetMin = lower;
        if (targetMax > upper)
        {
            targetMax = upper;
            targetMin = targetMax - span;
        }

        return new AxisRange(targetMin, targetMax);
    }
}
