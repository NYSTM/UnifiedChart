using System.Drawing;

namespace UnifiedChart.C1CWrapper;

public sealed class ChartFillStyle
{
    public Color Color1 { get; set; } = Color.Empty;

    public Color Color2 { get; set; } = Color.Empty;

    public Color OutlineColor { get; set; } = Color.Empty;

    internal string? GetColor1Name()
        => Color1.IsEmpty ? null : ColorTranslator.ToHtml(Color1);

    internal string? GetOutlineColorName()
        => OutlineColor.IsEmpty ? null : ColorTranslator.ToHtml(OutlineColor);
}
