namespace UnifiedChart.C1CWrapper;

public sealed class ChartDataPointData
{
    private readonly Func<int> _lengthProvider;
    private readonly Action<int>? _lengthSetter;

    internal ChartDataPointData(Func<int> lengthProvider, Action<int>? lengthSetter = null)
    {
        _lengthProvider = lengthProvider;
        _lengthSetter = lengthSetter;
    }

    public int Length
    {
        get => _lengthProvider();
        set => (_lengthSetter ?? throw new InvalidOperationException("Lengthは設定できません。"))(value);
    }
}