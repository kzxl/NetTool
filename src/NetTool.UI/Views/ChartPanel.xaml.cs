using System.Windows.Controls;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace NetTool.UI.Views
{
    public partial class ChartPanel : UserControl
    {
        public ChartPanel()
        {
            InitializeComponent();

            var labelPaint = new SolidColorPaint(SKColors.LightGray);

            RpsChart.YAxes = new Axis[]
            {
                new Axis
                {
                    Name = "RPS",
                    NamePaint = labelPaint,
                    LabelsPaint = labelPaint,
                    TextSize = 11,
                    MinLimit = 0,
                }
            };
            RpsChart.XAxes = new Axis[]
            {
                new Axis
                {
                    Name = "Time (s)",
                    NamePaint = labelPaint,
                    LabelsPaint = new SolidColorPaint(SKColors.Transparent),
                    TextSize = 11,
                }
            };

            LatencyChart.YAxes = new Axis[]
            {
                new Axis
                {
                    Name = "ms",
                    NamePaint = labelPaint,
                    LabelsPaint = labelPaint,
                    TextSize = 11,
                    MinLimit = 0,
                }
            };
            LatencyChart.XAxes = new Axis[]
            {
                new Axis
                {
                    Name = "Time (s)",
                    NamePaint = labelPaint,
                    LabelsPaint = new SolidColorPaint(SKColors.Transparent),
                    TextSize = 11,
                }
            };
        }
    }
}
