using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using UnifiedChart.WinForms;

namespace UnifiedChart.C1CWrapper;

public sealed class ChartControl : UserControl
{
    private readonly PlotView plotView;
    private readonly UnifiedChart.PlotModel plotModel = new();
    private string propBag = string.Empty;

    public ChartControl()
    {
        plotView = new PlotView
        {
            Dock = DockStyle.Fill
        };
        Controls.Add(plotView);
        ChartArea = new ChartArea();
        plotView.AxisScroll += PlotView_AxisScroll;
        WireCoordinateLookups();
        RefreshChart();
    }

    public ChartArea ChartArea { get; }

    public ChartHeader Header => ChartArea.Header;

    public List<ChartGroup> ChartGroups => ChartArea.ChartGroups;

    public PlotView PlotView => plotView;

    public event EventHandler? AxisScroll;

    public Image GetImage()
        => GetImage(ClientSize.Width > 0 && ClientSize.Height > 0
            ? ClientSize
            : new Size(Math.Max(1, Width), Math.Max(1, Height)));

    public Image GetImage(Size size)
    {
        if (size.Width <= 0 || size.Height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size), "画像サイズは1以上で指定してください。");
        }

        using var source = new Bitmap(Math.Max(1, ClientSize.Width), Math.Max(1, ClientSize.Height));
        DrawToBitmap(source, new Rectangle(Point.Empty, source.Size));

        var image = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppPArgb);
        using var graphics = Graphics.FromImage(image);
        graphics.Clear(BackColor);
        graphics.DrawImage(source, new Rectangle(Point.Empty, size));
        return image;
    }

    public Image GetImage(ImageFormat format)
    {
        ArgumentNullException.ThrowIfNull(format);
        return GetImage();
    }

    public Image GetImage(ImageFormat format, Size size)
    {
        ArgumentNullException.ThrowIfNull(format);
        return GetImage(size);
    }

    public void SaveImage(string fileName, ImageFormat format)
        => SaveImage(fileName, format, GetOutputSize());

    public void SaveImage(string fileName, ImageFormat format, Size size)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentNullException.ThrowIfNull(format);
        using var image = GetImage(size);
        image.Save(fileName, format);
    }

    public void SaveImage(Stream stream, ImageFormat format)
        => SaveImage(stream, format, GetOutputSize());

    public void SaveImage(Stream stream, ImageFormat format, Size size)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(format);
        using var image = GetImage(size);
        image.Save(stream, format);
    }

    public void SaveImage(ref byte[] imageData, ImageFormat format)
        => SaveImage(ref imageData, format, GetOutputSize());

    public void SaveImage(ref byte[] imageData, ImageFormat format, Size size)
    {
        ArgumentNullException.ThrowIfNull(format);
        using var stream = new MemoryStream();
        SaveImage(stream, format, size);
        imageData = stream.ToArray();
    }

    public void SaveImage(ImageFormat format)
    {
        ArgumentNullException.ThrowIfNull(format);
        using var image = GetImage();
    }

    public void SaveImage(ImageFormat format, Size size)
    {
        ArgumentNullException.ThrowIfNull(format);
        using var image = GetImage(size);
    }

    private Size GetOutputSize()
        => ClientSize.Width > 0 && ClientSize.Height > 0
            ? ClientSize
            : new Size(Math.Max(1, Width), Math.Max(1, Height));

    public string PropBag
    {
        get => propBag;
        set
        {
            propBag = value ?? string.Empty;
            PropBagDeserializer.Apply(propBag, ChartArea);
            ChartArea.ApplyLayout(this);
            RefreshChart();
        }
    }

    public void RefreshChart()
    {
        WireCoordinateLookups();
        ChartArea.ApplyTo(plotModel);
        UnifiedChart.ChartLogger.Write("ChartControl.RefreshChart", plotModel);
        plotView.Model = plotModel;
        plotView.InvalidatePlot();
    }

    public void ResetZoom()
    {
        UnifiedChart.ChartLogger.Write("ChartControl.ResetZoom", plotModel);
        plotView.ResetZoom();
    }

    public UnifiedChart.AxisRange? DisplayXRange => plotView.DisplayXRange;

    public UnifiedChart.AxisRange? DisplayYRange => plotView.DisplayYRange;

    public int CoordToDataIndex(Point screenPoint)
        => plotView.CoordToDataIndex(screenPoint);

    public bool TryCoordToDataIndex(Point screenPoint, out int dataIndex)
        => plotView.TryCoordToDataIndex(screenPoint, out dataIndex);

    public int CoordToDataIndex(int x, int y)
        => plotView.CoordToDataIndex(new Point(x, y));

    private void WireCoordinateLookups()
    {
        for (var i = 0; i < ChartArea.ChartGroups.Count; i++)
        {
            var groupIndex = i;
            ChartArea.ChartGroups[i].GroupIndex = groupIndex;
            ChartArea.ChartGroups[i].CoordinateLookup = (int x, int y, CoordinateFocusEnum focus, out int seriesIndex, out int pointIndex, out int distance) =>
                plotView.CoordToDataIndex(x, y, groupIndex, (int)focus, out seriesIndex, out pointIndex, out distance);
        }
    }

    public void SetDisplayRange(UnifiedChart.AxisRange? xRange, UnifiedChart.AxisRange? yRange)
    {
        UnifiedChart.ChartLogger.Write("ChartControl.SetDisplayRange", plotModel, $"X={xRange}, Y={yRange}");
        plotView.SetDisplayRange(xRange, yRange);
    }

    public void ScrollX(double direction = 1)
    {
        UnifiedChart.ChartLogger.Write("ChartControl.ScrollX", plotModel, $"Direction={direction}");
        plotView.ScrollX(direction);
    }

    public void ScrollY(double direction = 1)
    {
        UnifiedChart.ChartLogger.Write("ChartControl.ScrollY", plotModel, $"Direction={direction}");
        plotView.ScrollY(direction);
    }

    private void PlotView_AxisScroll(object? sender, EventArgs e)
    {
        AxisScroll?.Invoke(this, e);
    }
}
