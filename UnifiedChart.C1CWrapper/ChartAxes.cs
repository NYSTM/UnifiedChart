namespace UnifiedChart.C1CWrapper;

public sealed class ChartAxes
{
    internal ChartAxes(ChartAxis x, ChartAxis y)
    {
        X = x;
        Y = y;
    }

    public ChartAxis X { get; }

    public ChartAxis? X2 { get; set; }

    public ChartAxis Y { get; }

    public ChartAxis? Y2 { get; set; }

    public ChartAxis this[int index] => index switch
    {
        0 => X,
        1 => Y,
        2 when X2 is { } x2 => x2,
        3 when Y2 is { } y2 => y2,
        _ => throw new ArgumentOutOfRangeException(nameof(index), index, "軸インデックスが無効です。")
    };
}
