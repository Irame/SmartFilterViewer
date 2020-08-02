using CsvHelper;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
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
    class SensorInfo
    {
        public SensorDataList DataList { get; set; }

        public Color GraphColor { get; set; }

        public Shape Shape { get; set; }

        public Stroke Stroke { get; set; }

        public string FileName { get; set; }
    }


    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Dictionary<Shape, SensorInfo> sensorInfos;
        Timer timer;

        DateTime currentDataTime;
        DateTime lastTimerTick;
        DateTime dataStartTime;
        double dataTimeInMillisec;
        double playbackFactor;

        Stroke timeStroke;
        Stroke zoomIndocator1;
        Stroke zoomIndocator2;

        Point mouseDownPos;
        double graphOffset;
        double graphZoomfactor;

        double maxValue;

        PropertyInfo propInfo;

        public MainWindow()
        {
            InitializeComponent();

            timer = new Timer(20);
            timer.AutoReset = true;
            timer.Start();
            timer.Enabled = false;
            timer.Elapsed += ProcessTimerTick;

            playbackFactor = 1;
            graphOffset = 0;
            graphZoomfactor = 1;

            sensorInfos = new Dictionary<Shape, SensorInfo>();

            propInfo = typeof(SensorData).GetProperty(nameof(SensorData.PM2_5_ug_m3));
        }

        

        private void ProcessTimerTick(object sender, ElapsedEventArgs e)
        {
            currentDataTime += TimeSpan.FromMilliseconds((e.SignalTime - lastTimerTick).TotalMilliseconds * playbackFactor);
            lastTimerTick = e.SignalTime;

            foreach (var info in sensorInfos.Values)
            {
                double lerpIdx = info.DataList.FindLerpIndex(currentDataTime);
                double value = info.DataList.GetLerpValue(lerpIdx, propInfo);

                int i = (int)Math.Floor(lerpIdx);
                var lastLookahead = i + 5;
                for (; i < info.DataList.Count && i <= lastLookahead; i++)
                {
                    SensorData record = info.DataList[i];
                    if (record.PM2_5_ug_m3 > 90)
                    {
                        playbackFactor = Math.Min(playbackFactor, 1);
                        break;
                    }
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (IsLoaded)
                    {
                        var factor = value / maxValue;
                        factor = Math.Pow(factor, 0.3);
                        info.Shape.Fill = new SolidColorBrush(HsvColor.FromHSV((float)((1 - factor) * 120), 1, 1));

                        PlaybackSpeedSlider.Value = playbackFactor;
                    }
                });
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                if (IsLoaded && dataTimeInMillisec > 0)
                {
                    var currentTimestamp = currentDataTime - dataStartTime;
                    TimestampLabel.Content = $"{(currentTimestamp < TimeSpan.Zero ? "-" : "")}{currentTimestamp:hh\\:mm\\:ss}";

                    var progress = currentTimestamp.TotalMilliseconds / dataTimeInMillisec;
                    PlaybackProgressBar.Value = progress;

                    DrawTimeStroke();
                }
            });
        }

        private void DrawDataToGraph()
        {
            foreach (var info in sensorInfos.Values)
            {
                var points = new StylusPointCollection();
                foreach (var dataPoint in info.DataList)
                {
                    var xFactor = (dataPoint.DateTime - dataStartTime).TotalMilliseconds / dataTimeInMillisec;
                    var yFactor = 1 - GetValue(dataPoint) / maxValue;

                    var xPos = (xFactor - graphOffset) * graphZoomfactor * TimelineGraph.ActualWidth;
                    var yPos = yFactor * TimelineGraph.ActualHeight;

                    points.Add(new StylusPoint(xPos, yPos));
                }
                info.Stroke = DrawStroke(points, info.GraphColor, info.Stroke);
            }

            DrawTimeStroke();
        }

        private void DrawTimeStroke()
        {
            if (dataTimeInMillisec == 0)
                return;

            var currentTimestamp = currentDataTime - dataStartTime;
            var progress = currentTimestamp.TotalMilliseconds / dataTimeInMillisec;

            var scaledProgress = graphZoomfactor * (progress - graphOffset);

            var points = new StylusPointCollection();
            points.Add(new StylusPoint(TimelineGraph.ActualWidth * scaledProgress, 0));
            points.Add(new StylusPoint(TimelineGraph.ActualWidth * scaledProgress, TimelineGraph.ActualHeight));

            timeStroke = DrawStroke(points, Colors.Red, timeStroke);
        }

        private void DrawZoomIndicators(Point end)
        {
            var points = new StylusPointCollection();
            points.Add(new StylusPoint(mouseDownPos.X, 0));
            points.Add(new StylusPoint(mouseDownPos.X, TimelineGraph.ActualHeight));

            zoomIndocator1 = DrawStroke(points, Colors.Black, zoomIndocator1);

            points = new StylusPointCollection();
            points.Add(new StylusPoint(end.X, 0));
            points.Add(new StylusPoint(end.X, TimelineGraph.ActualHeight));

            zoomIndocator2 = DrawStroke(points, Colors.Black, zoomIndocator2);
        }

        private Stroke DrawStroke(StylusPointCollection points, Color color, Stroke oldStroke)
        {
            TimelineGraph.Strokes.Remove(oldStroke);
            var result = new Stroke(points, new DrawingAttributes { Color = color });
            TimelineGraph.Strokes.Add(result);
            return result;
        }

        private double GetValue(SensorData data)
        {
            return (double)propInfo.GetValue(data);
        }

        private void JumpToRelPos(double pos)
        {
            currentDataTime = dataStartTime + TimeSpan.FromMilliseconds(dataTimeInMillisec * pos);
            DrawTimeStroke();
        }

        private void StartAnimationBtn_Click(object sender, RoutedEventArgs e)
        {
            lastTimerTick = DateTime.Now;
            timer.Enabled = true;
        }

        private void PlaybackSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            playbackFactor = e.NewValue;
            if (PlaybackSpeedLabel != null)
                PlaybackSpeedLabel.Content = $"{e.NewValue:0.0}x";
        }

        private void PlaybackProgressBar_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var clickpos = (double)e.GetPosition(PlaybackProgressBar).X / PlaybackProgressBar.ActualWidth;

            currentDataTime = dataStartTime + TimeSpan.FromMilliseconds(dataTimeInMillisec * clickpos);
        }

        private void TimelineGraph_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (IsLoaded)
                DrawDataToGraph();
        }

        private void TimelineGraph_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var oldZoomFactor = graphZoomfactor;
            var relMousePos = (double)e.GetPosition(TimelineGraph).X / TimelineGraph.ActualWidth;
            var scaledRelMousePos = relMousePos / graphZoomfactor;
            if (e.Delta > 0)
            {
                graphZoomfactor *= 2;
            }
            else if (e.Delta < 0)
            {
                graphZoomfactor /= 2;
            }
            graphOffset += scaledRelMousePos * (1 - oldZoomFactor / graphZoomfactor);
            DrawDataToGraph();
        }

        private void TimelineGraph_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            mouseDownPos = e.GetPosition(TimelineGraph);
            TimelineGraph.CaptureMouse();
            e.Handled = true;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            timer.Enabled = false;
        }

        private void TimelineGraph_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!TimelineGraph.IsMouseCaptured)
                return;

            TimelineGraph.ReleaseMouseCapture();

            var upPosX = e.GetPosition(TimelineGraph).X;
            var upScaledPosX = pointToScaledRelPos(upPosX, TimelineGraph.ActualWidth);

            TimelineGraph.Strokes.Remove(zoomIndocator1);
            TimelineGraph.Strokes.Remove(zoomIndocator2);

            if (Math.Abs(mouseDownPos.X - upPosX) > SystemParameters.MinimumHorizontalDragDistance)
            {
                var downSclaedPosX = pointToScaledRelPos(mouseDownPos.X, TimelineGraph.ActualWidth);
                var left = Math.Min(downSclaedPosX, upScaledPosX);
                var right = Math.Max(downSclaedPosX, upScaledPosX);
                graphOffset = left;
                graphZoomfactor = 1 / (right - left);
                DrawDataToGraph();
            }
            else
            {
                JumpToRelPos(upScaledPosX);
            }

            e.Handled = true;

            double pointToScaledRelPos(double x, double width)
            {
                var clickpos = x / TimelineGraph.ActualWidth;

                return clickpos / graphZoomfactor + graphOffset;
            }
        }

        private void TimelineGraph_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            var pos = e.GetPosition(TimelineGraph);

            if (e.LeftButton == MouseButtonState.Pressed && Math.Abs(pos.X - mouseDownPos.X) > SystemParameters.MinimumHorizontalDragDistance && TimelineGraph.IsMouseCaptured)
            {
                DrawZoomIndicators(e.GetPosition(TimelineGraph));
            }
            else
            {
                TimelineGraph.Strokes.Remove(zoomIndocator1);
                TimelineGraph.Strokes.Remove(zoomIndocator2);

                zoomIndocator1 = null;
                zoomIndocator2 = null;
            }
        }

        private Color[] colors = new[] { Colors.SkyBlue, Colors.Orange, Colors.Cyan, Colors.Gold, Colors.Fuchsia, Colors.Indigo, Colors.Lime, Colors.Lavender, Colors.Silver };

        private void Sensor_MouseUp(object sender, MouseButtonEventArgs e)
        {
            timer.Enabled = false;

            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                var shape = sender as Shape;
                if (!sensorInfos.TryGetValue(shape, out SensorInfo info))
                {
                    info = new SensorInfo { Shape = shape, GraphColor = colors[sensorInfos.Count] };
                    shape.Stroke = new SolidColorBrush(info.GraphColor);
                    sensorInfos.Add(shape, info);
                }

                info.FileName = openFileDialog.FileName;

                var allLines = File.ReadAllLines(info.FileName);
                var dataLines = allLines.SkipWhile(x => !x.StartsWith("OADateTime"))
                    .Skip(1);

                using (var reader = new StringReader(string.Join("\n", dataLines)))
                using (var csv = new CsvReader(reader, CultureInfo.CurrentCulture))
                {
                    csv.Configuration.HasHeaderRecord = false;
                    info.DataList = new SensorDataList(csv.GetRecords<SensorData>());
                }

                dataStartTime = sensorInfos.Values.Select(x => x.DataList.First().DateTime).Min();
                currentDataTime = dataStartTime;

                var dataEndTime = sensorInfos.Values.Select(x => x.DataList.Last().DateTime).Max();

                dataTimeInMillisec = (dataEndTime - dataStartTime).TotalMilliseconds;

                CalcMaxValue();

                DrawDataToGraph();
            }
            lastTimerTick = DateTime.Now;
            timer.Enabled = true;
        }

        private void CalcMaxValue()
        {
            if (sensorInfos.Any())
                maxValue = sensorInfos.Values.SelectMany(x => x.DataList).Select(x => GetValue(x)).Max();
        }

        private void ResetZoomBtn_Click(object sender, RoutedEventArgs e)
        {
            graphZoomfactor = 1;
            graphOffset = 0;
            DrawDataToGraph();
        }

        class PropertyDropdownItem
        {
            public PropertyInfo PropertyInfo { get; set; }

            public override string ToString()
            {
                var attr = Attribute.GetCustomAttribute(PropertyInfo, typeof(NiceNameAttribute)) as NiceNameAttribute;
                return attr?.Name ?? PropertyInfo.Name;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var dropDownItems = typeof(SensorData).GetProperties()
                .Where(x => Attribute.GetCustomAttribute(x, typeof(NiceNameAttribute)) != null)
                .Select(x => new PropertyDropdownItem { PropertyInfo = x }).ToList();
            ValuesComboBox.ItemsSource = dropDownItems;
            ValuesComboBox.SelectedItem = dropDownItems.FirstOrDefault(x => x.PropertyInfo == propInfo);
        }

        private void ValuesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            propInfo = (ValuesComboBox.SelectedItem as PropertyDropdownItem).PropertyInfo;
            CalcMaxValue();
            DrawDataToGraph();
        }
    }
}
