using System.Globalization;
using System.Drawing;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace UnifiedChart.C1CWrapper;

internal static class PropBagDeserializer
{
    public static void Apply(string xml, ChartArea area)
    {
        ArgumentNullException.ThrowIfNull(area);
        if (string.IsNullOrWhiteSpace(xml))
        {
            return;
        }

        XDocument document;
        try
        {
            document = XDocument.Parse(xml, LoadOptions.None);
        }
        catch (Exception exception) when (exception is XmlException or InvalidOperationException)
        {
            throw new FormatException("PropBagのXMLを読み込めませんでした。", exception);
        }

        var root = document.Root;
        if (root is null || root.Name.LocalName != "Chart2DPropBag")
        {
            throw new FormatException("PropBagのルート要素がChart2DPropBagではありません。");
        }

        var header = root.Element("Header");
        if (header is not null)
        {
            area.Header.Compass = AttributeValue(header, "Compass");
            area.Header.Visible = AttributeBool(header, "Visible", true);
            area.Header.Text = header.Element("Text")?.Value ?? string.Empty;
        }

        var chartArea = root.Element("ChartArea");
        if (chartArea is not null)
        {
            area.LocationDefault = AttributePoint(chartArea, "LocationDefault");
            area.SizeDefault = AttributeSize(chartArea, "SizeDefault");
        }

        var footer = root.Element("Footer");
        if (footer is not null)
        {
            area.Footer.Compass = AttributeValue(footer, "Compass");
            area.Footer.Visible = AttributeBool(footer, "Visible", true);
            area.Footer.Text = footer.Element("Text")?.Value ?? string.Empty;
        }

        var legend = root.Element("Legend");
        if (legend is not null)
        {
            area.Legend.Visible = AttributeBool(legend, "Visible", true);
            area.Legend.Position = ToLegendPosition(AttributeValue(legend, "Compass"));
        }

        var unsupportedItems = FindUnsupportedItems(root);
        ApplyAxes(root.Element("Axes"), area);
        var chartGroupsCollection = root.Element("ChartGroupsCollection")
            ?? root.Element("ChartGroupCollection");
        ApplyGroups(chartGroupsCollection, area);
        WriteLog(xml, area, unsupportedItems);
    }

    private static void WriteLog(string xml, ChartArea area, IReadOnlyList<string> unsupportedItems)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"ChartGroups: {area.ChartGroups.Count}");
        for (var groupIndex = 0; groupIndex < area.ChartGroups.Count; groupIndex++)
        {
            var group = area.ChartGroups[groupIndex];
            builder.AppendLine($"  ChartGroup[{groupIndex}]: Name=\"{group.Name}\", ChartType={group.ChartType}, ChartData={group.ChartData.Count}");
            for (var dataIndex = 0; dataIndex < group.ChartData.Count; dataIndex++)
            {
                var data = group.ChartData[dataIndex];
                builder.AppendLine($"    ChartData[{dataIndex}]: Series={data.SeriesList.Count}");
                for (var seriesIndex = 0; seriesIndex < data.SeriesList.Count; seriesIndex++)
                {
                    var series = data.SeriesList[seriesIndex];
                    builder.AppendLine($"      Series[{seriesIndex}]: Label=\"{series.Label}\", Display={series.Display}, Count={series.Count}");
                }
            }
        }

        UnifiedChart.ChartLogger.WritePropBag(xml, builder.ToString(), unsupportedItems);
    }

    private static IReadOnlyList<string> FindUnsupportedItems(XElement root)
    {
        var items = new List<string>();
        var supportedElements = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Chart2DPropBag", "StyleCollection", "NamedStyle", "NameStyle", "StyleData", "Header", "Footer", "Text", "Legend", "Axes", "Axis", "ChartArea", "Margin", "AutoLabelArrangement", "GridMajor",
            "TickLabels", "TickLabel", "LabelMode", "AnnotationMethod", "SB", "ScrollBar",
            "ScrollButtons", "ValueLabels", "ValueLabel", "ValueLabelCollection", "AlarmZones", "AlarmZone", "AlarmZoneCollection",
            "Markers", "Marker", "MarkerCollection", "GridLines", "GridLine", "GridLineCollection", "ChartGroupsCollection",
            "ChartGroupCollection", "ChartGroup", "DataSerializer", "DataSeriesCollection", "DataSeriesSerializer", "DataSeries", "LineStyle", "StyleData", "SymbolStyle", "SeriesLabel", "DataType", "DataTypes", "FillStyle", "Histogram", "Highlight", "X", "Y"
        };

        foreach (var element in root.DescendantsAndSelf())
        {
            if (!supportedElements.Contains(element.Name.LocalName))
            {
                items.Add($"未対応要素: <{element.Name.LocalName}>");
            }
        }

        var supportedAttributes = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Header"] = Set("Compass", "Visible"),
            ["Footer"] = Set("Compass", "Visible"),
            ["NamedStyle"] = Set("Name", "ParentName", "StyleData"),
            ["NameStyle"] = Set("Name", "ParentName"),
            ["StyleData"] = Set("GradientStyle", "Border", "HatchStyle", "BackColor", "BackColor2", "FrameType", "Rounding", "Font", "Rotation", "Alignment", "AlignVert", "ForeColor", "Color", "WrapText"),
            ["ChartGroup"] = Set("Name", "ChartType", "Type", "Chart2DType", "Use3D"),
            ["Legend"] = Set("Compass", "Visible"),
            ["ChartArea"] = Set("LocationDefault", "SizeDefault"),
            ["Axis"] = Set("AutoMin", "Min", "AutoMax", "Max", "UnitMajor", "UnitMinor", "AutoMajor", "AutoMinor", "Compass", "Origin", "OriginValue", "TickDirection", "TickMarkDirection", "TickLabelsVisible", "LabelMode", "AnnotationMethod", "ChartValueLabelsEnabled", "ScrollUnit", "ScrollMin", "ScrollMax", "ScrollEnabled", "SynchronizeWithY2", "Scale", "IsLogarithmic", "IsDateTime", "DateTimeFormat"),
            ["TickLabels"] = Set("Visible"),
            ["GridMajor"] = Set("Visible", "Spacing"),
            ["SB"] = Set("Visible", "Buttons", "Mode", "Unit"),
            ["ScrollBar"] = Set("Visible", "Buttons", "Mode", "Unit"),
            ["ScrollButtons"] = Set("Visible", "Buttons", "Mode", "Unit"),
            ["ValueLabel"] = Set("Value", "Position", "Text", "Caption", "Color", "Compass"),
            ["AlarmZone"] = Set("Mode", "From", "To", "LowerLimit", "UpperLimit", "Color", "Alpha", "Visible"),
            ["Marker"] = Set("Value", "Text", "Color", "ArrowDirection", "ShowArrow", "ShowConnectionLine"),
            ["GridLine"] = Set("Value", "Color", "DashStyle", "Thickness", "Visible"),
            ["DataSerializer"] = Set("Hole", "DefaultSet"),
            ["DataSeriesSerializer"] = Set("DataType", "DataTypes"),
            ["DataSeries"] = Set("DataType", "DataTypes"),
            ["LineStyle"] = Set("Color", "Thickness"),
            ["SymbolStyle"] = Set("Color", "OutlineColor", "Shape"),
            ["DataTypes"] = Set(),
            ["FillStyle"] = Set("Color1", "Color2", "OutlineColor"),
            ["Histogram"] = Set(),
            ["Margin"] = Set()
        };

        foreach (var element in root.DescendantsAndSelf())
        {
            if (!supportedAttributes.TryGetValue(element.Name.LocalName, out var attributes))
            {
                continue;
            }

            foreach (var attribute in element.Attributes())
            {
                if (!attributes.Contains(attribute.Name.LocalName))
                {
                    items.Add($"未対応属性: <{element.Name.LocalName} {attribute.Name.LocalName}=\"{attribute.Value}\">");
                }
            }
        }

        foreach (var element in root.Descendants().Where(item =>
                     string.Equals(item.Name.LocalName, "AnnotationMethod", StringComparison.OrdinalIgnoreCase)
                     || string.Equals(item.Name.LocalName, "LabelMode", StringComparison.OrdinalIgnoreCase)))
        {
            var value = element.Value.Trim();
            if (value.Equals(nameof(AnnotationMethodEnum.Rotate), StringComparison.OrdinalIgnoreCase)
                || value.Equals(nameof(AnnotationMethodEnum.Stagger), StringComparison.OrdinalIgnoreCase)
                || value.Equals(nameof(AnnotationMethodEnum.Wrap), StringComparison.OrdinalIgnoreCase))
            {
                items.Add($"部分対応値: {element.Name.LocalName}=\"{value}\"（Wrapperでは保持しますが自動描画は未対応）");
            }
        }

        return items.Distinct(StringComparer.Ordinal).ToArray();

        static HashSet<string> Set(params string[] values)
            => new(values, StringComparer.OrdinalIgnoreCase);
    }

    private static void ApplyAxes(XElement? axesElement, ChartArea area)
    {
        if (axesElement is null)
        {
            return;
        }

        var axes = axesElement.Elements("Axis").Take(3).ToList();
        if (axes.Count > 0)
        {
            ApplyAxis(axes[0], area.AxisX);
        }

        if (axes.Count > 1)
        {
            ApplyAxis(axes[1], area.AxisY);
        }

        if (axes.Count > 2)
        {
            var axisY2 = area.Y2Axis ?? new ChartAxis();
            ApplyAxis(axes[2], axisY2);
            area.Y2Axis = axisY2;
        }
    }

    private static void ApplyAxis(XElement element, ChartAxis axis)
    {
        var autoMin = AttributeBool(element, "AutoMin", true);
        var autoMax = AttributeBool(element, "AutoMax", true);
        axis.Min = autoMin ? null : AttributeDouble(element, "Min");
        axis.Max = autoMax ? null : AttributeDouble(element, "Max");
        axis.UnitMajor = AttributeDouble(element, "UnitMajor");
        axis.UnitMinor = AttributeDouble(element, "UnitMinor");
        axis.AutoMajor = AttributeBool(element, "AutoMajor", true);
        axis.AutoMinor = AttributeBool(element, "AutoMinor", true);
        axis.AutoMin = autoMin;
        axis.AutoMax = autoMax;
        axis.IsDateTime = AttributeBool(element, "IsDateTime", axis.IsDateTime);
        axis.DateTimeFormat = AttributeValue(element, "DateTimeFormat") ?? axis.DateTimeFormat;
        axis.Compass = AttributeValue(element, "Compass");
        axis.Text = element.Element("Text")?.Value ?? string.Empty;
        axis.Origin = AttributeDouble(element, "Origin") ?? AttributeDouble(element, "OriginValue");
        axis.TickDirection = ToTickDirection(
            AttributeValue(element, "TickDirection"),
            AttributeValue(element, "TickMarkDirection"));
        var tickLabels = element.Element("TickLabels") ?? element.Element("TickLabel");
        axis.TickLabelsVisible = tickLabels is null
            ? AttributeBool(element, "TickLabelsVisible", true)
            : AttributeBool(tickLabels, "Visible", true);
        axis.LabelMode = ToLabelMode(
            AttributeValue(element, "LabelMode"),
            AttributeValue(element, "AnnotationMethod"),
            AttributeBool(element, "ChartValueLabelsEnabled", false));
        var labelMode = element.Element("LabelMode") ?? element.Element("AnnotationMethod");
        if (labelMode is not null)
        {
            axis.LabelMode = ToLabelMode(labelMode.Value, string.Empty, axis.ChartValueLabelsEnabled);
        }

        var gridMajor = element.Element("GridMajor");
        if (gridMajor is not null)
        {
            axis.GridMajorVisible = AttributeBool(gridMajor, "Visible", true);
            axis.GridMajorSpacing = AttributeDouble(gridMajor, "Spacing");
        }

        var scrollButtons = element.Element("SB")
            ?? element.Element("ScrollBar")
            ?? element.Element("ScrollButtons");
        axis.ShowScrollButtons = scrollButtons is not null &&
            (scrollButtons.Attribute("Visible") is not null
                ? AttributeBool(scrollButtons, "Visible", false)
                : string.Equals((string?)scrollButtons.Attribute("Buttons"), "ScrollButtons", StringComparison.OrdinalIgnoreCase) ||
                  string.Equals((string?)scrollButtons.Attribute("Mode"), "ScrollButtons", StringComparison.OrdinalIgnoreCase));
        var scrollUnit = scrollButtons?.Attribute("Unit") is not null
            ? AttributeDouble(scrollButtons, "Unit")
            : AttributeDouble(element, "ScrollUnit");
        axis.ScrollUnit = scrollUnit;
        var scrollScale = scrollButtons is not null
            ? AttributeDouble(scrollButtons, "Scale")
            : AttributeDouble(element, "Scale");
        if (scrollScale.HasValue)
        {
            axis.ScrollBar.Scale = scrollScale.Value;
        }
        axis.ScrollMin = scrollButtons is not null ? AttributeDouble(scrollButtons, "Min") : AttributeDouble(element, "ScrollMin");
        axis.ScrollMax = scrollButtons is not null ? AttributeDouble(scrollButtons, "Max") : AttributeDouble(element, "ScrollMax");
        axis.ScrollEnabled = scrollButtons is null || AttributeBool(scrollButtons, "Enabled", true);
        axis.SynchronizeWithY2 = scrollButtons is not null &&
            (AttributeBool(scrollButtons, "SynchronizeWithY2", false) || AttributeBool(scrollButtons, "Y2Linked", false));

        ApplyGridLines(element, axis);
        ApplyMarkers(element, axis);
        ApplyAlarmZones(element, axis);
        ApplyValueLabels(element, axis);
    }

    private static void ApplyValueLabels(XElement element, ChartAxis axis)
    {
        var container = element.Element("ValueLabels")
            ?? element.Element("ValueLabelCollection")
            ?? element.Element("ValueLabel");
        if (container is null)
        {
            return;
        }

        axis.ValueLabels.Clear();
        var items = container.Name.LocalName == "ValueLabel"
            ? new[] { container }
            : container.Elements().Where(item => item.Name.LocalName is "ValueLabel" or "Label");
        foreach (var item in items)
        {
            axis.ValueLabels.Add(new ChartValueLabel
            {
                Value = AttributeDouble(item, "Value") ?? AttributeDouble(item, "Position") ?? 0,
                Text = item.Element("Text")?.Value
                    ?? item.Element("Label")?.Value
                    ?? FirstAttribute(item, "Text", "Label", "Caption")
                    ?? string.Empty,
                Color = FirstAttribute(item, "Color", "TextColor") ?? "Black",
                Visible = AttributeBool(item, "Visible", true)
            });
        }
    }

    private static void ApplyAlarmZones(XElement element, ChartAxis axis)
    {
        var container = element.Element("AlarmZones") ?? element.Element("AlarmZoneCollection");
        if (container is null)
        {
            return;
        }

        axis.AlarmZones.Clear();
        foreach (var item in container.Elements("AlarmZone"))
        {
            axis.AlarmZones.Add(new ChartAlarmZone
            {
                Mode = ToAlarmZoneMode(FirstAttribute(item, "Mode", "AlarmZoneMode", "InsideOutside") ?? "Inside"),
                LowerLimit = AttributeDouble(item, "LowerLimit") ?? AttributeDouble(item, "LimitLow") ?? AttributeDouble(item, "ControlLimitLow"),
                UpperLimit = AttributeDouble(item, "UpperLimit") ?? AttributeDouble(item, "LimitHigh") ?? AttributeDouble(item, "ControlLimitHigh"),
                From = AttributeDouble(item, "From") ?? AttributeDouble(item, "Min") ?? 0,
                To = AttributeDouble(item, "To") ?? AttributeDouble(item, "Max") ?? 0,
                FillColor = FirstAttribute(item, "FillColor", "Color") ?? "Yellow",
                Opacity = (byte)Math.Clamp((int)(AttributeDouble(item, "Opacity") ?? 64), 0, 255),
                BorderColor = FirstAttribute(item, "BorderColor", "LineColor") ?? "Transparent",
                BorderThickness = (float)(AttributeDouble(item, "BorderThickness") ?? AttributeDouble(item, "LineWidth") ?? 1),
                BorderDashStyle = FirstAttribute(item, "BorderDashStyle", "LineStyle") ?? "Solid",
                Text = item.Element("Text")?.Value ?? AttributeValue(item, "Text"),
                Visible = AttributeBool(item, "Visible", true)
            });
        }
    }

    private static string? FirstAttribute(XElement element, params string[] names)
        => names.Select(name => (string?)element.Attribute(name)).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

    private static void ApplyGridLines(XElement element, ChartAxis axis)
    {
        var container = element.Element("GridLines") ?? element.Element("GridLineCollection");
        if (container is null)
        {
            return;
        }

        axis.GridLines.Clear();
        foreach (var item in container.Elements("GridLine"))
        {
            axis.GridLines.Add(new ChartGridLine
            {
                Value = AttributeDouble(item, "Value") ?? 0,
                Color = AttributeValue(item, "Color") is { Length: > 0 } color ? color : "LightGray",
                DashStyle = AttributeValue(item, "DashStyle") is { Length: > 0 } dashStyle ? dashStyle : "Dash",
                Thickness = (float)(AttributeDouble(item, "Thickness") ?? 1),
                Visible = AttributeBool(item, "Visible", true)
            });
        }
    }

    private static void ApplyMarkers(XElement element, ChartAxis axis)
    {
        var container = element.Element("Markers") ?? element.Element("MarkerCollection");
        if (container is null)
        {
            return;
        }

        axis.Markers.Clear();
        foreach (var item in container.Elements("Marker"))
        {
            axis.Markers.Add(new ChartAxisMarker
            {
                Value = AttributeDouble(item, "Value") ?? 0,
                Text = item.Element("Text")?.Value ?? AttributeValue(item, "Text"),
                Color = AttributeValue(item, "Color") is { Length: > 0 } color ? color : "Black",
                ArrowDirection = AttributeValue(item, "ArrowDirection") is { Length: > 0 } direction ? direction : "Auto",
                ShowArrow = AttributeBool(item, "ShowArrow", true),
                ShowConnectionLine = AttributeBool(item, "ShowConnectionLine", true)
            });
        }
    }

    private static void ApplyGroups(XElement? collection, ChartArea area)
    {
        if (collection is null)
        {
            return;
        }

        area.ChartGroups.Clear();
        foreach (var element in collection.Elements("ChartGroup"))
        {
            var group = new ChartGroup
            {
                Name = AttributeValue(element, "Name"),
                ChartType = ToChartType(
                    FirstAttribute(element, "ChartType", "Type", "Chart2DType"))
            };
            var serializer = element.Element("DataSerializer");
            if (serializer is not null)
            {
                group.MissingValueHole = AttributeDouble(serializer, "Hole");
                group.DefaultDataSerializer = AttributeBool(serializer, "DefaultSet", false);
                ApplyDataSeries(serializer, group.ChartData[0]);
            }

            area.ChartGroups.Add(group);
        }

        if (area.ChartGroups.Count == 0)
        {
            area.ChartGroups.Add(new ChartGroup());
        }
    }

    private static void ApplyDataSeries(XElement serializer, ChartData data)
    {
        var collection = serializer.Element("DataSeriesCollection")
            ?? serializer.Element("DataSerializer")?.Element("DataSeriesCollection")
            ?? serializer.Descendants("DataSeriesCollection").FirstOrDefault();
        if (collection is null)
        {
            return;
        }

        foreach (var element in collection.Elements().Where(item =>
                     item.Name.LocalName is "DataSeriesSerializer" or "DataSeries"))
        {
            var series = data.AddNewSeries();
            series.Label = element.Element("SeriesLabel")?.Value.Trim() ?? string.Empty;
            series.DataType = FirstAttribute(element, "DataType")
                ?? element.Element("DataType")?.Value.Trim()
                ?? string.Empty;
            AddDataTypes(series, FirstAttribute(element, "DataTypes")
                ?? element.Element("DataTypes")?.Value);

            var lineStyle = element.Element("LineStyle") ?? element.Element("StyleData");
            if (lineStyle is not null)
            {
                series.Color = AttributeValue(lineStyle, "Color") is { Length: > 0 } color ? color : series.Color;
                series.StrokeThickness = AttributeDouble(lineStyle, "Thickness") ?? series.StrokeThickness;
            }

            var symbolStyle = element.Element("SymbolStyle");
            if (symbolStyle is not null)
            {
                if (AttributeValue(symbolStyle, "Color") is { Length: > 0 } color)
                {
                    series.SymbolStyle.Color = color;
                }

                if (AttributeValue(symbolStyle, "OutlineColor") is { Length: > 0 } outlineColor)
                {
                    series.MarkerOutlineColor = outlineColor;
                }

                if (AttributeValue(symbolStyle, "Shape") is { Length: > 0 } shape)
                {
                    series.SymbolStyle.Shape = ToSymbolShape(shape);
                }

                series.ShowMarkers = true;
            }

            var fillStyle = element.Element("FillStyle");
            if (fillStyle is not null)
            {
                if (AttributeValue(fillStyle, "Color1") is { Length: > 0 } color1)
                {
                    series.FillStyle.Color1 = ToColor(color1);
                }

                if (AttributeValue(fillStyle, "Color2") is { Length: > 0 } color2)
                {
                    series.FillStyle.Color2 = ToColor(color2);
                }

                if (AttributeValue(fillStyle, "OutlineColor") is { Length: > 0 } outlineColor)
                {
                    series.FillStyle.OutlineColor = ToColor(outlineColor);
                }
            }

            AddValues(series.X, element.Element("X")?.Value, series.DataTypes, series.DataType);
            AddValues(series.Y, element.Element("Y")?.Value, series.DataTypes, series.DataType);
        }
    }

    private static void AddDataTypes(ChartDataSeries series, string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        series.DataTypes.Clear();
        series.DataTypes.AddRange(text.Split(
            new[] { ';', ',', ' ', '\t', '\r', '\n' },
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    private static void AddValues(ChartDataValues values, string? text, IReadOnlyList<string> dataTypes, string defaultType)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var index = 0;
        foreach (var token in text.Split(
                     new[] { ';', ' ', '\t', '\r', '\n' },
                     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var typeName = dataTypes.Count > index ? dataTypes[index] : defaultType;
            if (TryParseValue(token, typeName, out var value))
            {
                values.Add(value);
            }

            index++;
        }
    }

    private static bool TryParseValue(string token, string typeName, out double value)
    {
        if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
        {
            return false;
        }

        if (typeName.Equals("Single", StringComparison.OrdinalIgnoreCase) ||
            typeName.Equals("Float", StringComparison.OrdinalIgnoreCase))
        {
            value = (float)value;
        }

        return true;
    }

    private static UnifiedChart.MarkerShape ToSymbolShape(string value)
        => value.ToLowerInvariant() switch
        {
            "dot" => UnifiedChart.MarkerShape.Dot,
            "square" => UnifiedChart.MarkerShape.Square,
            "box" => UnifiedChart.MarkerShape.Box,
            "diamond" => UnifiedChart.MarkerShape.Diamond,
            "tri" or "triangle" => UnifiedChart.MarkerShape.Triangle,
            "invertedtri" or "invertedtriangle" => UnifiedChart.MarkerShape.InvertedTri,
            "cross" => UnifiedChart.MarkerShape.Cross,
            "diagcross" => UnifiedChart.MarkerShape.DiagCross,
            "plus" => UnifiedChart.MarkerShape.Plus,
            "star" => UnifiedChart.MarkerShape.Star,
            _ => UnifiedChart.MarkerShape.Circle
        };

    private static System.Drawing.Color ToColor(string value)
        => System.Drawing.ColorTranslator.FromHtml(value);

    private static string AttributeValue(XElement element, string name)
        => (string?)element.Attribute(name) ?? string.Empty;

    private static double? AttributeDouble(XElement element, string name)
        => double.TryParse((string?)element.Attribute(name), NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;

    private static Point? AttributePoint(XElement element, string name)
        => ParsePair((string?)element.Attribute(name)) is { } pair
            ? new Point(pair.First, pair.Second)
            : null;

    private static Size? AttributeSize(XElement element, string name)
        => ParsePair((string?)element.Attribute(name)) is { } pair
            ? new Size(pair.First, pair.Second)
            : null;

    private static (int First, int Second)? ParsePair(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var values = text.Trim('(', ')', '[', ']')
            .Split(new[] { ',', ';', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (values.Length < 2
            || !int.TryParse(values[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var first)
            || !int.TryParse(values[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var second))
        {
            return null;
        }

        return (first, second);
    }

    private static bool AttributeBool(XElement element, string name, bool defaultValue)
        => bool.TryParse((string?)element.Attribute(name), out var value) ? value : defaultValue;

    private static UnifiedChart.LegendPosition ToLegendPosition(string compass)
        => compass.ToLowerInvariant() switch
        {
            "west" => UnifiedChart.LegendPosition.TopLeft,
            "south" => UnifiedChart.LegendPosition.BottomLeft,
            "north" => UnifiedChart.LegendPosition.TopRight,
            _ => UnifiedChart.LegendPosition.TopRight
        };

    private static UnifiedChart.AxisTickDirection ToTickDirection(string value, string alias)
        => (string.IsNullOrWhiteSpace(value) ? alias : value).ToLowerInvariant() switch
        {
            "inward" or "inside" or "in" => UnifiedChart.AxisTickDirection.Inward,
            "cross" or "both" => UnifiedChart.AxisTickDirection.Cross,
            _ => UnifiedChart.AxisTickDirection.Outward
        };

    private static UnifiedChart.AlarmZoneMode ToAlarmZoneMode(string value)
        => (value ?? string.Empty).ToLowerInvariant() switch
        {
            "inside" or "inner" or "in" => UnifiedChart.AlarmZoneMode.Inside,
            "outside" or "outer" or "out" => UnifiedChart.AlarmZoneMode.Outside,
            _ => UnifiedChart.AlarmZoneMode.Explicit
        };

    private static UnifiedChart.AxisLabelMode ToLabelMode(string value, string alias, bool valueLabelsEnabled)
    {
        var mode = string.IsNullOrWhiteSpace(value) ? alias : value;
        if (string.Equals(mode, "ValueLabels", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(mode, "ValueLabel", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(mode, "AnnotationMethodEnum.ValueLabels", StringComparison.OrdinalIgnoreCase))
        {
            return UnifiedChart.AxisLabelMode.ValueLabels;
        }

        return valueLabelsEnabled ? UnifiedChart.AxisLabelMode.ValueLabels : UnifiedChart.AxisLabelMode.Standard;
    }

    private static Chart2DType ToChartType(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Chart2DType.Line;
        }

        return value.ToLowerInvariant() switch
        {
            "linesymbols" or "line_symbols" or "linewithsymbols" => Chart2DType.LineSymbols,
            "xyscatter" or "scatter" => Chart2DType.XYScatter,
            "bar" => Chart2DType.Bar,
            "column" => Chart2DType.Column,
            "area" => Chart2DType.Area,
            "pie" => Chart2DType.Pie,
            "doughnut" or "donut" => Chart2DType.Doughnut,
            "radar" => Chart2DType.Radar,
            _ => throw new FormatException($"未対応のChartTypeです: {value}")
        };
    }
}
