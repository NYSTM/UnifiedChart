using System.Drawing;

namespace UnifiedChart.C1CWrapper;

public sealed class ChartSymbolStyle
{
    public object Shape { get; set; } = SymbolShapeEnum.Circle;

    public double Size { get; set; } = 3.0;

    internal UnifiedChart.MarkerShape GetMarkerShape()
        => Shape switch
        {
            SymbolShapeEnum shape => shape switch
            {
                SymbolShapeEnum.Dot => UnifiedChart.MarkerShape.Dot,
                SymbolShapeEnum.Square => UnifiedChart.MarkerShape.Square,
                SymbolShapeEnum.Box => UnifiedChart.MarkerShape.Box,
                SymbolShapeEnum.Diamond => UnifiedChart.MarkerShape.Diamond,
                SymbolShapeEnum.Triangle => UnifiedChart.MarkerShape.Triangle,
                SymbolShapeEnum.InvertedTri => UnifiedChart.MarkerShape.InvertedTri,
                SymbolShapeEnum.Cross => UnifiedChart.MarkerShape.Cross,
                SymbolShapeEnum.DiagCross => UnifiedChart.MarkerShape.DiagCross,
                SymbolShapeEnum.Plus => UnifiedChart.MarkerShape.Plus,
                SymbolShapeEnum.Star => UnifiedChart.MarkerShape.Star,
                _ => UnifiedChart.MarkerShape.Circle
            },
            UnifiedChart.MarkerShape markerShape => markerShape,
            _ => UnifiedChart.MarkerShape.Circle
        };

    public object Color { get; set; } = string.Empty;

    internal string? GetColorName()
        => Color switch
        {
            Color color when !color.IsEmpty => ColorTranslator.ToHtml(color),
            string value when !string.IsNullOrWhiteSpace(value) => value,
            _ => null
        };
}