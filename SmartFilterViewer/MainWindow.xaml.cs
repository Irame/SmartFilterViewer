using CsvHelper;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
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
    public class SensorInfo
    {
        public SensorDataList DataList { get; }

        public Color GraphColor { get; }

        public Shape Shape { get; }
    }


    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        SensorDataList records;
        Timer timer;
        DateTime currentDataTime;
        DateTime lastTimerTick;
        double playbackFactor;
        double dataTimeInMillisec;
        Stroke dataStroke;
        Stroke timeStroke;

        Stroke zoomIndocator1;
        Stroke zoomIndocator2;

        Point mouseDownPos;

        double graphOffset;
        double graphZoomfactor;

        double maxValue;

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
        }

        

        private void ProcessTimerTick(object sender, ElapsedEventArgs e)
        {
            currentDataTime += TimeSpan.FromMilliseconds((e.SignalTime - lastTimerTick).TotalMilliseconds * playbackFactor);
            lastTimerTick = e.SignalTime;

            double lerpIdx = records.FindLerpIndex(currentDataTime);

            double value = records.GetLerpValue(lerpIdx, nameof(SensorData.PM2_5_ug_m3));

            int i = (int)Math.Floor(lerpIdx);
            var lastLookahead = i + 5;
            for (; i < records.Count && i <= lastLookahead; i++)
            {
                SensorData record = records[i];
                if (record.PM2_5_ug_m3 > 90)
                {
                    playbackFactor = Math.Min(playbackFactor, 1);
                    break;
                }
            }

            var currentTimestamp = currentDataTime - records.First().DateTime;
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (IsLoaded)
                {
                    TimestampLabel.Content = $"{(currentTimestamp < TimeSpan.Zero ? "-" : "")}{currentTimestamp:hh\\:mm\\:ss}";

                    var progress = currentTimestamp.TotalMilliseconds / dataTimeInMillisec;
                    PlaybackProgressBar.Value = progress;

                    var factor = value / maxValue;
                    factor = Math.Pow(factor, 0.3);
                    Sensor1.Fill = new SolidColorBrush(HsvColor.FromHSV((float)((1 - factor) * 120), 1, 1));

                    PlaybackSpeedSlider.Value = playbackFactor;

                    DrawTimeStroke();
                }
            });
        }

        private void BrowseBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
                fileSelectEdit.Text = openFileDialog.FileName;
        }

        private void ReadDataBtn_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(fileSelectEdit.Text))
            {
                var allLines = File.ReadAllLines(fileSelectEdit.Text);
                var dataLines = allLines.SkipWhile(x => !x.StartsWith("OADateTime"))
                    .Skip(1);

                using (var reader = new StringReader(string.Join("\n", dataLines)))
                using (var csv = new CsvReader(reader, CultureInfo.CurrentCulture))
                {
                    csv.Configuration.HasHeaderRecord = false;
                    records = new SensorDataList(csv.GetRecords<SensorData>());
                }

                dataTimeInMillisec = (records.Last().DateTime - records.First().DateTime).TotalMilliseconds;

                maxValue = records.Select(x => x.PM2_5_ug_m3).Max();

                currentDataTime = records.First().DateTime;

                DrawDataToGraph();
            }
            else
            {
                MessageBox.Show($"Datei {fileSelectEdit.Text} nicht gefunden");
            }
        }

        private void DrawDataToGraph()
        {
            if (records == null)
                return;

            var points = new StylusPointCollection();
            var startTime = records.First().DateTime;
            foreach (var record in records)
            {
                var xFactor = (record.DateTime - startTime).TotalMilliseconds / dataTimeInMillisec;
                var yFactor = 1 - record.PM2_5_ug_m3 / maxValue;

                var xPos = (xFactor - graphOffset) * graphZoomfactor * TimelineGraph.ActualWidth;
                var yPos = yFactor * TimelineGraph.ActualHeight;

                points.Add(new StylusPoint(xPos, yPos));
            }
            TimelineGraph.Strokes.Remove(dataStroke);
            dataStroke = new Stroke(points, new DrawingAttributes { Color = Colors.SkyBlue });
            TimelineGraph.Strokes.Add(dataStroke);

            DrawTimeStroke();
        }

        private void DrawTimeStroke()
        {
            if (records == null)
                return;

            var currentTimestamp = currentDataTime - records.First().DateTime;
            var progress = currentTimestamp.TotalMilliseconds / dataTimeInMillisec;

            var scaledProgress = graphZoomfactor * (progress - graphOffset);

            var points = new StylusPointCollection();
            points.Add(new StylusPoint(TimelineGraph.ActualWidth * scaledProgress, 0));
            points.Add(new StylusPoint(TimelineGraph.ActualWidth * scaledProgress, TimelineGraph.ActualHeight));

            TimelineGraph.Strokes.Remove(timeStroke);
            timeStroke = new Stroke(points, new DrawingAttributes { Color = Colors.Red });
            TimelineGraph.Strokes.Add(timeStroke);
        }

        private void DrawZoomIndicators(Point end)
        {
            var points = new StylusPointCollection();
            points.Add(new StylusPoint(mouseDownPos.X, 0));
            points.Add(new StylusPoint(mouseDownPos.X, TimelineGraph.ActualHeight));

            TimelineGraph.Strokes.Remove(zoomIndocator1);
            zoomIndocator1 = new Stroke(points, new DrawingAttributes { Color = Colors.Black });
            TimelineGraph.Strokes.Add(zoomIndocator1);

            points = new StylusPointCollection();
            points.Add(new StylusPoint(end.X, 0));
            points.Add(new StylusPoint(end.X, TimelineGraph.ActualHeight));

            TimelineGraph.Strokes.Remove(zoomIndocator2);
            zoomIndocator2 = new Stroke(points, new DrawingAttributes { Color = Colors.Black });
            TimelineGraph.Strokes.Add(zoomIndocator2);
        }

        private void JumpToRelPos(double pos)
        {
            currentDataTime = records.First().DateTime + TimeSpan.FromMilliseconds(dataTimeInMillisec * pos);
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

            currentDataTime = records.First().DateTime + TimeSpan.FromMilliseconds(dataTimeInMillisec * clickpos);
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
            e.Handled = true;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            timer.Enabled = false;
        }

        private void TimelineGraph_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
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

            if (e.LeftButton == MouseButtonState.Pressed && Math.Abs(pos.X - mouseDownPos.X) > SystemParameters.MinimumHorizontalDragDistance)
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
    }
}
