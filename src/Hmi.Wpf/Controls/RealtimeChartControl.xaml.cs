using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Hmi.ViewModels;

namespace Hmi.Wpf.Controls;

/// <summary>
/// 用 WPF 原生绘图还原原文实时曲线：浅灰横向网格、0-10 纵轴、0-600 滚动横轴。
/// 不引入第三方图表控件，样式与截图完全一致且无额外依赖。
/// </summary>
public partial class RealtimeChartControl : UserControl
{
    public static readonly DependencyProperty SeriesProperty = DependencyProperty.Register(
        nameof(Series),
        typeof(IEnumerable<ChartSeriesViewModel>),
        typeof(RealtimeChartControl),
        new PropertyMetadata(null, OnSeriesChanged));

    public IEnumerable<ChartSeriesViewModel>? Series
    {
        get => (IEnumerable<ChartSeriesViewModel>?)GetValue(SeriesProperty);
        set => SetValue(SeriesProperty, value);
    }

    private static readonly Brush GridBrush = new SolidColorBrush(Color.FromRgb(0xE8, 0xED, 0xF2));
    private static readonly Brush TickBrush = new SolidColorBrush(Color.FromRgb(0x60, 0x69, 0x71));

    public RealtimeChartControl()
    {
        InitializeComponent();
        SizeChanged += (_, _) => Draw();
    }

    private static void OnSeriesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (RealtimeChartControl)d;
        control.Unsubscribe();
        control.Subscribe();
        control.Draw();
    }

    private void Subscribe()
    {
        if (Series is null)
        {
            return;
        }
        foreach (var series in Series) series.ValuesChanged += OnSeriesValuesChanged;
    }

    private void Unsubscribe()
    {
        if (Series is null)
        {
            return;
        }
        foreach (var series in Series) series.ValuesChanged -= OnSeriesValuesChanged;
    }

    private void OnSeriesValuesChanged(object? sender, EventArgs e) => Draw();

    private void Draw()
    {
        RootCanvas.Children.Clear();

        var width = ActualWidth;
        var height = ActualHeight;
        if (width < 40 || height < 40)
        {
            return;
        }

        const double left = 46, right = 14, top = 14, bottom = 28;
        var plotWidth = width - left - right;
        var plotHeight = height - top - bottom;
        if (plotWidth <= 0 || plotHeight <= 0)
        {
            return;
        }

        // 横向网格线 + 纵轴刻度 0..10
        for (var value = 0; value <= 10; value++)
        {
            var y = top + plotHeight * (1 - value / 10.0);
            RootCanvas.Children.Add(new Line
            {
                X1 = left, X2 = left + plotWidth, Y1 = y, Y2 = y,
                Stroke = GridBrush, StrokeThickness = 1
            });

            var label = new TextBlock
            {
                Text = value.ToString(),
                FontSize = 12,
                Foreground = TickBrush,
                FontFamily = new FontFamily("Segoe UI")
            };
            Canvas.SetLeft(label, left - 26);
            Canvas.SetTop(label, y - 9);
            RootCanvas.Children.Add(label);
        }

        // 横轴刻度 0..600
        for (var i = 0; i <= 6; i++)
        {
            var x = left + plotWidth * i / 6.0;
            var label = new TextBlock
            {
                Text = (i * 100).ToString(),
                FontSize = 12,
                Foreground = TickBrush,
                FontFamily = new FontFamily("Segoe UI")
            };
            Canvas.SetLeft(label, x - 11);
            Canvas.SetTop(label, top + plotHeight + 6);
            RootCanvas.Children.Add(label);
        }

        if (Series is null)
        {
            return;
        }

        foreach (var series in Series)
        {
            var values = series.ValuesProvider();
            if (values.Count < 2)
            {
                continue;
            }

            var points = new PointCollection();
            for (var i = 0; i < values.Count; i++)
            {
                var x = left + plotWidth * i / 600.0;
                var clamped = Math.Max(0, Math.Min(10, values[i]));
                var y = top + plotHeight * (1 - clamped / 10.0);
                points.Add(new Point(x, y));
            }

            var brush = new BrushConverter().ConvertFromString(series.ColorHex) as Brush ?? Brushes.Blue;
            RootCanvas.Children.Add(new Polyline
            {
                Points = points,
                Stroke = brush,
                StrokeThickness = 1.5,
                StrokeLineJoin = PenLineJoin.Round
            });
        }
    }
}
