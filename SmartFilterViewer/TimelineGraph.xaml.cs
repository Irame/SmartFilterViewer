using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SmartFilterViewer
{
    public class TimelineGraphData
    {
        public Color Color { get; set; }

        public List<Point> Data { get; set; }
    }

    /// <summary>
    /// Interaktionslogik für TimelineGraph.xaml
    /// </summary>
    public partial class TimelineGraph : UserControl
    {
        public IEnumerable<TimelineGraphData> Data
        {
            get { return (IEnumerable<TimelineGraphData>)GetValue(DataProperty); }
            set { SetValue(DataProperty, value); }
        }

        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register("Data", typeof(IEnumerable<TimelineGraphData>), typeof(TimelineGraph), new PropertyMetadata(null, DataPropertyChanged));


        public double Progress
        {
            get { return (double)GetValue(ProgressProperty); }
            set { SetValue(ProgressProperty, value); }
        }

        public static readonly DependencyProperty ProgressProperty =
            DependencyProperty.Register("Progress", typeof(double), typeof(TimelineGraph), new PropertyMetadata(0.0, ProgressPropertyChanged));


        private static void DataPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => (d as TimelineGraph).OnDataChanged();
        private static void ProgressPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => (d as TimelineGraph).OnProgressChanged();

        private StrokeCollection dataStrokes = new StrokeCollection();
        private Point mouseDownPos;

        private double zoomFactor;
        private double zoomOffset;

        public TimelineGraph()
        {
            InitializeComponent();

            zoomFactor = 1;

            ZoomIndicatorLeft.Visibility = Visibility.Collapsed;
            ZoomIndicatorRight.Visibility = Visibility.Collapsed;
        }

        public void ResetZoom()
        {
            SetZoomParams(1, 0);
        }

        private void DrawGraphs()
        {
            Canvas.Strokes.Remove(dataStrokes);
            dataStrokes.Clear();

            if (Data == null)
                return;

            foreach (var item in Data)
            {
                var usedPoints = new List<Point>();

                var endPlotRange = zoomOffset + 1 / zoomFactor;
                for (int i = 0; i < item.Data.Count; i++)
                {
                    var point = item.Data[i];
                    if (point.X < zoomOffset)
                        continue;

                    if (i > 0 && usedPoints.Count == 0)
                        usedPoints.Add(transformPoint(item.Data[i - 1]));

                    usedPoints.Add(transformPoint(point));
                }

                dataStrokes.Add(new Stroke(
                    new StylusPointCollection(usedPoints), 
                    new DrawingAttributes { Color = item.Color }));
            }

            Canvas.Strokes.Add(dataStrokes);

            Point transformPoint(Point relDataPoint)
            {
                return new Point(
                    (relDataPoint.X - zoomOffset) * zoomFactor * Canvas.ActualWidth,
                    (1-relDataPoint.Y) * Canvas.ActualHeight);
            }
        }

        private void UpdateTimeIndicator()
        {
            var xPos = (Progress - zoomOffset) * zoomFactor * Canvas.ActualWidth;
            if (!double.IsNaN(xPos) && xPos >= 0 && xPos <= Canvas.ActualWidth)
            {
                TimeIndicator.Margin = new Thickness(xPos, 0, 0, 0);
                TimeIndicator.Visibility = Visibility.Visible;
            }
            else
            {
                TimeIndicator.Visibility = Visibility.Collapsed;
            }
        }

        private void OnDataChanged()
        {
            DrawGraphs();
        }

        private void OnProgressChanged()
        {
            UpdateTimeIndicator();
        }

        private void Canvas_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                mouseDownPos = e.GetPosition(Canvas);
                Canvas.CaptureMouse();
            }
            e.Handled = true;
        }
        private void Canvas_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            var pos = e.GetPosition(Canvas);

            if (e.LeftButton == MouseButtonState.Pressed && Math.Abs(pos.X - mouseDownPos.X) > SystemParameters.MinimumHorizontalDragDistance && Canvas.IsMouseCaptured)
            {
                ZoomIndicatorLeft.Visibility = Visibility.Visible;
                ZoomIndicatorRight.Visibility = Visibility.Visible;

                ZoomIndicatorLeft.Margin = new Thickness(mouseDownPos.X, 0, 0, 0);
                ZoomIndicatorRight.Margin = new Thickness(pos.X, 0, 0, 0);
            }
            else
            {
                ZoomIndicatorLeft.Visibility = Visibility.Collapsed;
                ZoomIndicatorRight.Visibility = Visibility.Collapsed;
            }
        }

        private void Canvas_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!Canvas.IsMouseCaptured)
                return;

            Canvas.ReleaseMouseCapture();

            var upPosX = e.GetPosition(Canvas).X;
            var upScaledPosX = pointToScaledRelPos(upPosX, Canvas.ActualWidth);

            ZoomIndicatorLeft.Visibility = Visibility.Collapsed;
            ZoomIndicatorRight.Visibility = Visibility.Collapsed;

            if (Math.Abs(mouseDownPos.X - upPosX) > SystemParameters.MinimumHorizontalDragDistance)
            {
                var downSclaedPosX = pointToScaledRelPos(mouseDownPos.X, Canvas.ActualWidth);
                var left = Math.Min(downSclaedPosX, upScaledPosX);
                var right = Math.Max(downSclaedPosX, upScaledPosX);
                zoomOffset = left;
                zoomFactor = 1 / (right - left);
                DrawGraphs();
            }
            else
            {
                Progress = upScaledPosX;
            }

            e.Handled = true;

            double pointToScaledRelPos(double x, double width)
            {
                return x / width / zoomFactor + zoomOffset;
            }
        }

        private void Canvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DrawGraphs();
        }

        private void Canvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var relMousePos = (double)e.GetPosition(Canvas).X / Canvas.ActualWidth;
            if (e.Delta > 0)
                ZoomIn(relMousePos);
            else if (e.Delta < 0)
                ZoomOut(relMousePos);
        }

        private void SetZoomParams(double factor, double offset)
        {
            zoomFactor = factor;
            zoomOffset = offset;

            DrawGraphs();
            UpdateTimeIndicator();
        }

        public void Zoom(double center, double factor)
        {
            var oldZoomFactor = zoomFactor;
            var scaledCenter = center / zoomFactor;
            SetZoomParams(zoomFactor * factor, zoomOffset + scaledCenter * (1 - oldZoomFactor / zoomFactor));
        }

        public void ZoomIn(double center = 0.5, double factor = 2) => Zoom(center, factor);
        public void ZoomOut(double center = 0.5, double factor = 2) => Zoom(center, 1 / factor);

    }
}
