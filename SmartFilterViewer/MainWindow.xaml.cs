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
        private SensorDataList dataList;
        private Color graphColor;

        public SensorDataList DataList
        {
            get => dataList;
            set
            {
                dataList = value;
                HistogramWindow.SensorDataList = value;
            }
        }

        public Color GraphColor
        {
            get => graphColor;
            set
            {
                graphColor = value;
                Shape.Stroke = new SolidColorBrush(graphColor);
                Stroke.DrawingAttributes.Color = graphColor;
            }
        }

        public Shape Shape { get; set; }

        public Stroke Stroke { get; set; } = MainWindow.MakeEmptyStroke();

        public string FileName { get; set; }

        public HistogramWindow HistogramWindow { get; set; } = new HistogramWindow();

        public bool HasData => DataList != null;
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
        bool isPaused;

        Stroke timeStroke;
        Stroke zoomIndocator1;
        Stroke zoomIndocator2;

        Point mouseDownPos;
        double graphOffset;
        double graphZoomfactor;

        double maxValue;

        PropertyInfo propInfo;

        private IEnumerable<SensorInfo> ValidSensorInfos => sensorInfos.Values.Where(x => x.DataList != null);

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

            timeStroke = MakeEmptyStroke();
            timeStroke.DrawingAttributes.Color = Colors.Red;
        }

        static public Stroke MakeEmptyStroke()
        {
            return new Stroke(new StylusPointCollection(new[] { new Point(-100, -100) }));
        }
        

        private void ProcessTimerTick(object sender, ElapsedEventArgs e)
        {
            if (!isPaused)
                currentDataTime += TimeSpan.FromMilliseconds((e.SignalTime - lastTimerTick).TotalMilliseconds * playbackFactor);
            
            lastTimerTick = e.SignalTime;

            foreach (var info in ValidSensorInfos)
            {
                double lerpIdx = info.DataList.FindLerpIndex(currentDataTime);
                double value = info.DataList.GetLerpValue(lerpIdx, propInfo);

                info.HistogramWindow.LerpIdx = lerpIdx;

                int i = (int)Math.Floor(lerpIdx);
                var lastLookahead = i + 5;
                for (; i < info.DataList.Count && i <= lastLookahead; i++)
                {
                    SensorData record = info.DataList[i];
                    if (record.PM2_5_ug_m3 > 0.4*maxValue)
                    {
                        playbackFactor = Math.Min(playbackFactor, 1);
                        break;
                    }
                }

                Application.Current?.Dispatcher.Invoke(() =>
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

            Application.Current?.Dispatcher.Invoke(() =>
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
            var startTime = RelativeToDataTime(graphOffset);
            var endTime = RelativeToDataTime(graphOffset + 1/graphZoomfactor);

            foreach (var info in ValidSensorInfos)
            {
                var startIdx = (int)Math.Floor(info.DataList.FindLerpIndex(startTime));
                var endIdx = (int)Math.Ceiling(info.DataList.FindLerpIndex(endTime));

                startIdx = Math.Max(startIdx, 0);
                endIdx = Math.Min(endIdx, info.DataList.Count-1);

                var points = new StylusPointCollection();
                for (int i = startIdx; i <= endIdx; i++)
                {
                    SensorData dataPoint = info.DataList[i];

                    var xFactor = (dataPoint.DateTime - dataStartTime).TotalMilliseconds / dataTimeInMillisec;
                    var yFactor = 1 - GetValue(dataPoint) / maxValue;

                    var xPos = (xFactor - graphOffset) * graphZoomfactor * TimelineGraph.ActualWidth;
                    var yPos = yFactor * TimelineGraph.ActualHeight;

                    points.Add(new StylusPoint(xPos, yPos));
                }
                info.Stroke.StylusPoints = points;
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

            timeStroke.StylusPoints = points;
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

        private DateTime RelativeToDataTime(double relTime)
        {
            return dataStartTime + TimeSpan.FromMilliseconds(dataTimeInMillisec * relTime);
        }

        private void JumpToRelPos(double pos)
        {
            currentDataTime = RelativeToDataTime(pos);
            DrawTimeStroke();
        }

        private void LoadDataForSensor(Shape sensorShape)
        {
            timer.Enabled = false;

            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                var info = sensorInfos[sensorShape];

                info.FileName = openFileDialog.FileName;

                var allLines = File.ReadAllLines(info.FileName);
                var dataLines = allLines.SkipWhile(x => !x.StartsWith("OADateTime"))
                    .Skip(1)
                    .Select(x => x.Replace("n. def.", "0"));

                using (var reader = new StringReader(string.Join("\n", dataLines)))
                using (var csv = new CsvReader(reader, CultureInfo.CurrentCulture))
                {
                    csv.Configuration.HasHeaderRecord = false;
                    info.DataList = new SensorDataList(csv.GetRecords<SensorData>());
                }

                dataStartTime = ValidSensorInfos.Select(x => x.DataList.First().DateTime).Min();
                currentDataTime = dataStartTime;

                var dataEndTime = ValidSensorInfos.Select(x => x.DataList.Last().DateTime).Max();

                dataTimeInMillisec = (dataEndTime - dataStartTime).TotalMilliseconds;

                CalcMaxValue();

                DrawDataToGraph();
            }
            lastTimerTick = DateTime.Now;
            timer.Enabled = true;
        }

        private void OpenHistogramForSensor(Shape sensorShape)
        {
            sensorInfos[sensorShape].HistogramWindow.Show();
        }

        private void CalcMaxValue()
        {
            if (ValidSensorInfos.Any())
                maxValue = ValidSensorInfos.SelectMany(x => x.DataList).Select(x => GetValue(x)).Max();
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


            var sensorShapes = new[] { Sensor1, Sensor2, Sensor3, Sensor4, Sensor5, Sensor6, Sensor7, Sensor8, Sensor9, };
            for (int i = 0; i < sensorShapes.Length; i++)
            {
                Ellipse sensorShape = sensorShapes[i];
                var info = new SensorInfo { Shape = sensorShape, GraphColor = HsvColor.FromHSV(180 + i * 15, 1, 1) };
                sensorShape.Stroke = new SolidColorBrush(info.GraphColor);
                TimelineGraph.Strokes.Add(info.Stroke);
                sensorInfos.Add(sensorShape, info);
            }

            TimelineGraph.Strokes.Add(timeStroke);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            timer.Enabled = false;

            foreach (var info in sensorInfos.Values)
            {
                info.HistogramWindow.Close();
            }
        }

        private void ValuesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            propInfo = (ValuesComboBox.SelectedItem as PropertyDropdownItem).PropertyInfo;
            CalcMaxValue();
            DrawDataToGraph();
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
                return x / width / graphZoomfactor + graphOffset;
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

        private void Sensor_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                LoadDataForSensor(sender as Shape);
            }
            else if (e.ChangedButton == MouseButton.Right)
            {
                OpenHistogramForSensor(sender as Shape);
            }
        }

        private void PlaybackProgressBar_ScrubbedToValue(object sender, double e)
        {
            JumpToRelPos(e);
        }

        private void PlaybackProgressBar_IsScrubbingChanged(object sender, bool e)
        {
            isPaused = e;
        }
    }
}
