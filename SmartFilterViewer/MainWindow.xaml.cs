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
using System.Windows.Threading;

namespace SmartFilterViewer
{
    public class SensorViewModel : PropertyChangedBase
    {
        private string loadedFile;
        private SensorDataList dataList;
        private Color graphColor;
        private Color shapeColor;

        public SensorDataList DataList
        {
            get => dataList;
            set
            {
                HistogramWindow.SensorDataList = value;
                SetAndNotify(value, ref dataList);
            }
        }

        public Color GraphColor
        {
            get => graphColor;
            set
            {
                SetAndNotify(value, ref graphColor);
                HistogramWindow.BarColor = graphColor;
            }
        }

        public Color ShapeColor { get => shapeColor; set => SetAndNotify(value, ref shapeColor); }

        public HistogramWindow HistogramWindow { get; set; } = new HistogramWindow();

        public void LoadDataFromUserFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                LoadDataFromFile(openFileDialog.FileName);
            }
        }

        public void LoadDataFromFile(string fileName)
        {
            if (fileName != null)
            {
                DataList = SensorDataList.FromFile(fileName);
                loadedFile = fileName;
            }
        }

        public void ShowHistogram()
        {
            if (HistogramWindow.IsVisible)
                HistogramWindow.Activate();
            else
                HistogramWindow.Show();
        }

        public void ApplySettings(SensorSettings settings)
        {
            GraphColor = settings.Color ?? GraphColor;
            LoadDataFromFile(settings.FileName);
        }

        public SensorSettings GenerateSettings()
        {
            var result = new SensorSettings();

            result.Color = GraphColor;
            result.FileName = loadedFile;

            return result;
        }
    }

    public class PropertyDropdownItem
    {
        public PropertyInfo PropertyInfo { get; set; }

        public override string ToString()
        {
            var attr = Attribute.GetCustomAttribute(PropertyInfo, typeof(NiceNameAttribute)) as NiceNameAttribute;
            return attr?.Name ?? PropertyInfo.Name;
        }
    }

    public class ViewModel : PropertyChangedBase
    {
        private DateTime currentDataTime;
        private double playbackFactor;

        private DateTime lastTimerTick;
        private DateTime dataStartTime;
        private double dataTimeInMillisec;

        private PropertyInfo propInfo;
        private double maxValue;

        private int heatLegendMaxTickCount;
        private int graphLegendMaxTickCount;
        private int timelineMaxTickCount;
        private int timelineGraphMaxTickCount;
        private List<TickInfo> timelineTickInfos;
        private List<TickInfo> timelineGraphTickInfos;
        private List<TickInfo> heatLegendTickInfos;
        private List<TickInfo> graphLegendTickInfos;
        private bool isPaused;

        public List<SensorViewModel> SensorInfos { get; }

        public bool IsPaused { get => isPaused; private set => SetAndNotify(value, ref isPaused); }

        public IEnumerable<TimelineGraphData> TimelineGraphData =>
            ValidSensorInfos.Select(sensorVM => new TimelineGraphData
            {
                Data = sensorVM.DataList.Select(dataPoint => new Point((dataPoint.DateTime - dataStartTime).TotalMilliseconds / dataTimeInMillisec, (double)propInfo.GetValue(dataPoint) / maxValue)).ToList(),
                Color = sensorVM.GraphColor
            });

        public double PlaybackFactor { get => playbackFactor; set => SetAndNotify(value, ref playbackFactor); }
        public TimeSpan CurrentTimestamp
        {
            get => CurrentDataTime - dataStartTime;
            set => CurrentDataTime = dataStartTime + value;
        }
        public double Progress
        {
            get => CurrentTimestamp.TotalMilliseconds / dataTimeInMillisec;
            set => CurrentDataTime = dataStartTime + TimeSpan.FromMilliseconds(dataTimeInMillisec * value);
        }
        public DateTime CurrentDataTime
        {
            get => currentDataTime;
            set
            {
                SetAndNotify(value, ref currentDataTime);
                OnNotifyPropertyChanged(nameof(Progress));
                OnNotifyPropertyChanged(nameof(CurrentTimestamp));
            }
        }

        public int TimelineMaxTickCount
        {
            get => timelineMaxTickCount;
            set
            {
                timelineMaxTickCount = value;
                UpdateTimelineTickInfos();
            }
        }
        public List<TickInfo> TimelineTickInfos { get => timelineTickInfos; set => SetAndNotify(value, ref timelineTickInfos); }

        public int TimelineGraphMaxTickCount
        {
            get => timelineGraphMaxTickCount;
            set
            {
                timelineGraphMaxTickCount = value;
                UpdateTimelineGraphTickInfos();
            }
        }
        public List<TickInfo> TimelineGraphTickInfos { get => timelineGraphTickInfos; set => SetAndNotify(value, ref timelineGraphTickInfos); }

        public int HeatLegendMaxTickCount
        {
            get => heatLegendMaxTickCount;
            set
            {
                heatLegendMaxTickCount = value;
                UpdateHeatLegendTickInfos();
            }
        }
        public List<TickInfo> HeatLegendTickInfos { get => heatLegendTickInfos; set => SetAndNotify(value, ref heatLegendTickInfos); }

        public int GraphLegendMaxTickCount
        {
            get => graphLegendMaxTickCount;
            set
            {
                graphLegendMaxTickCount = value;
                UpdateGraphLegendTickInfos();
            }
        }
        public List<TickInfo> GraphLegendTickInfos { get => graphLegendTickInfos; set => SetAndNotify(value, ref graphLegendTickInfos); }

        private Viewport viewport;
        public Viewport Viewport
        {
            set
            {
                viewport = value;
                UpdateTimelineGraphTickInfos();
            }
        }

        public bool IsScrubbing { get; set; }

        public string Unit { get => unit; set => SetAndNotify(value, ref unit); }

        public PropertyInfo PropInfo
        {
            get => propInfo;
            set
            {
                propInfo = value;
                CalcMaxValiue();
                OnNotifyPropertyChanged(nameof(TimelineGraphData));
                OnNotifyPropertyChanged(nameof(SelectedPropertyDropdownItem));

                var attr = Attribute.GetCustomAttribute(propInfo, typeof(NiceNameAttribute)) as NiceNameAttribute;
                Unit = attr?.Name;
            }
        }

        public List<PropertyDropdownItem> PropSelectItems { get; set; }
        public PropertyDropdownItem SelectedPropertyDropdownItem
        {
            get => PropSelectItems.FirstOrDefault(x => x.PropertyInfo == PropInfo);
            set => PropInfo = value.PropertyInfo;
        }

        private List<KeyValuePair<GradientKey, Color>> dataGradientStops;
        public List<KeyValuePair<GradientKey, Color>> DataGradientStops
        {
            get => dataGradientStops;
            set
            {
                SetAndNotify(value, ref dataGradientStops);
                UpdateGradientStops();
            }
        }

        private GradientStopCollection gradientStops;
        private string unit;

        public GradientStopCollection GradientStops { get => gradientStops; set => SetAndNotify(value, ref gradientStops); }

        public IEnumerable<SensorViewModel> ValidSensorInfos => SensorInfos.Where(x => x.DataList != null);

        public ViewModel()
        {
            PlaybackFactor = 1;

            IsPaused = true;

            DataGradientStops = new List<KeyValuePair<GradientKey, Color>>();
            DataGradientStops.Add(new KeyValuePair<GradientKey, Color>(new GradientKey { IsRelative = true, Value = 1 }, Colors.Red));
            DataGradientStops.Add(new KeyValuePair<GradientKey, Color>(new GradientKey { IsRelative = true, Value = 0.5 }, Colors.Yellow));
            DataGradientStops.Add(new KeyValuePair<GradientKey, Color>(new GradientKey { IsRelative = true, Value = 0 }, Color.FromRgb(0, 255, 0)));
            UpdateGradientStops();

            SensorInfos = new List<SensorViewModel>();
            for (int i = 0; i < 9; i++)
            {
                var sensorVM = new SensorViewModel { GraphColor = HsvColor.FromHSV(180 + i * 15, 1, 1) };
                sensorVM.HistogramWindow.Title = $"Histogramm - Sensor {i + 1}";
                SensorInfos.Add(sensorVM);
                sensorVM.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(SensorViewModel.DataList))
                    {
                        dataStartTime = ValidSensorInfos.Select(x => x.DataList.First().DateTime).Min();
                        CurrentDataTime = dataStartTime;

                        var dataEndTime = ValidSensorInfos.Select(x => x.DataList.Last().DateTime).Max();

                        dataTimeInMillisec = (dataEndTime - dataStartTime).TotalMilliseconds;

                        CalcMaxValiue();
                        UpdateTimelineTickInfos();
                        UpdateTimelineGraphTickInfos();

                        OnNotifyPropertyChanged(nameof(ValidSensorInfos));
                        OnNotifyPropertyChanged(nameof(TimelineGraphData));
                    }
                };
            }

            PropSelectItems = typeof(SensorData).GetProperties()
                .Where(x => Attribute.GetCustomAttribute(x, typeof(NiceNameAttribute)) != null)
                .Select(x => new PropertyDropdownItem { PropertyInfo = x }).ToList();

            PropInfo = typeof(SensorData).GetProperty(nameof(SensorData.PM2_5_ug_m3));
        }

        private void CalcMaxValiue()
        {
            if (ValidSensorInfos.Any())
                maxValue = ValidSensorInfos.SelectMany(x => x.DataList).Select(x => GetValue(x)).Max();

            UpdateGradientStops();
            UpdateHeatLegendTickInfos();
            UpdateGraphLegendTickInfos();
        }

        private void UpdateHeatLegendTickInfos()
        {
            HeatLegendTickInfos = GenerateValueTickInfo(heatLegendMaxTickCount);
        }

        private void UpdateGraphLegendTickInfos()
        {
            GraphLegendTickInfos = GenerateValueTickInfo(graphLegendMaxTickCount);
        }

        private void UpdateGradientStops()
        {
            GradientStops = new GradientStopCollection(DataGradientStops
                .Select(x => new GradientStop(x.Value, x.Key.IsRelative ? x.Key.Value : x.Key.Value / maxValue))
                .OrderBy(x => x.Offset));
        }

        public void ApplySettings(Settings settings)
        {
            var settingsProp = PropSelectItems.FirstOrDefault(x => x.ToString() == settings.Property);
            if (settingsProp != null)
                SelectedPropertyDropdownItem = settingsProp;

            for (int i = 0; i < SensorInfos.Count; i++)
            {
                if (settings.Sensors.TryGetValue(i + 1, out SensorSettings sensorSettings))
                    SensorInfos[i].ApplySettings(sensorSettings);
            }

            if (settings.ColorGradient.Any())
                DataGradientStops = settings.ColorGradient.ToList();
        }

        public Settings GenerateSettings()
        {
            var result = new Settings();

            result.Property = SelectedPropertyDropdownItem.ToString();

            for (int i = 0; i < SensorInfos.Count; i++)
            {
                result.Sensors.Add(i + 1, SensorInfos[i].GenerateSettings());
            }

            foreach (var item in DataGradientStops)
            {
                result.ColorGradient.Add(item.Key, item.Value);
            }

            return result;
        }

        private List<TickInfo> GenerateValueTickInfo(int maxTickCount)
        {
            var tickDist = Math.Ceiling((maxValue / maxTickCount) / 100) * 100;

            var tickInfos = new List<TickInfo>();
            tickInfos.Add(new TickInfo { Pos = 0, Label = "0" });
            for (double val = tickDist; val < maxValue - tickDist / 2; val += tickDist)
            {
                tickInfos.Add(new TickInfo { Pos = val / maxValue, Label = $"{val}" });
            }
            tickInfos.Add(new TickInfo { Pos = 1, Label = $"{maxValue:0}" });
            return tickInfos;
        }

        private void UpdateTimelineTickInfos()
        {
            TimelineTickInfos = GenerateTimelineTickInfo(timelineMaxTickCount);
        }

        private void UpdateTimelineGraphTickInfos()
        {
            TimelineGraphTickInfos = GenerateTimelineTickInfo(timelineGraphMaxTickCount, viewport.Offset.X, viewport.Offset.X + viewport.Size.Width);
        }

        private List<TickInfo> GenerateTimelineTickInfo(int maxTickCount, double relFrom = 0, double relTo = 1)
        {
            var tickCount = Math.Min(maxTickCount, 100);

            var newTickInfos = new List<TickInfo>();
            if (tickCount == 1)
            {
                newTickInfos.Add(new TickInfo { Pos = 1 , Label = TimeSpan.FromMilliseconds(dataTimeInMillisec).NegativeFormat(@"hh\:mm\:ss") });
            }
            else
            {
                var tickGap = TimeSpan.FromMilliseconds((relTo - relFrom) * dataTimeInMillisec / (tickCount - 1));
                var curTick = TimeSpan.FromMilliseconds(relFrom * dataTimeInMillisec);

                for (int i = 0; i < tickCount; i++)
                {
                    newTickInfos.Add(new TickInfo { Pos = (double)i / (tickCount - 1), Label = curTick.NegativeFormat(@"hh\:mm\:ss") });
                    curTick += tickGap;
                }
            }
            return newTickInfos;
        }


        public void Update()
        {
            var currentTime = DateTime.Now;

            if (!IsPaused && !IsScrubbing)
                CurrentDataTime += TimeSpan.FromMilliseconds((currentTime - lastTimerTick).TotalMilliseconds * PlaybackFactor);

            lastTimerTick = currentTime;

            foreach (var info in ValidSensorInfos)
            {
                double lerpIdx = info.DataList.FindLerpIndex(CurrentDataTime);
                double value = info.DataList.GetLerpValue(lerpIdx, propInfo);

                info.HistogramWindow.LerpIdx = lerpIdx;

                int i = (int)Math.Floor(lerpIdx);
                var lastLookahead = i + 5;
                for (; i < info.DataList.Count && i <= lastLookahead; i++)
                {
                    SensorData record = info.DataList[i];
                    if (GetValue(record) > 0.4 * maxValue)
                    {
                        PlaybackFactor = Math.Min(PlaybackFactor, 1);
                        break;
                    }
                }

                var factor = value / maxValue;

                if (GradientStops.Count == 0)
                    info.ShapeColor = Colors.Transparent;
                else if (GradientStops.Count == 1)
                    info.ShapeColor = GradientStops[0].Color;
                else if (GradientStops[0].Offset > factor)
                    info.ShapeColor = GradientStops[0].Color;
                else
                {
                    for (int j = 1; j < GradientStops.Count; j++)
                    {
                        var gradientStop = GradientStops[j];

                        if (gradientStop.Offset > factor)
                        {
                            var prevGradientStop = GradientStops[j - 1];

                            var diff = (gradientStop.Offset - prevGradientStop.Offset);
                            var relFactor = (factor - prevGradientStop.Offset);
                            var colFacor = (float)(relFactor / diff);
                            info.ShapeColor = prevGradientStop.Color * (1 - colFacor) + gradientStop.Color * colFacor;
                            break;
                        }

                        if (j == GradientStops.Count - 1)
                        {
                            info.ShapeColor = gradientStop.Color;
                            break;
                        }
                    }
                }
            }
        }

        public void StartAnimation()
        {
            lastTimerTick = DateTime.Now;
            IsPaused = false;
        }

        public void PauseAnimation()
        {
            IsPaused = true;
        }

        public void StopAnimation()
        {
            CurrentDataTime = dataStartTime;
            IsPaused = true;
        }

        public double GetValue(SensorData data)
        {
            return (double)propInfo.GetValue(data);
        }

        public DateTime RelativeToDataTime(double relTime)
        {
            return dataStartTime + TimeSpan.FromMilliseconds(dataTimeInMillisec * relTime);
        }

        public void JumpToRelPos(double pos)
        {
            CurrentDataTime = RelativeToDataTime(pos);
        }

        public void OnSensorClicked(SensorViewModel sensorViewModel, MouseButton mouseButton)
        {
            if (mouseButton == MouseButton.Left)
            {
                PauseAnimation();
                sensorViewModel.LoadDataFromUserFile();
                StartAnimation();
            }
            else if (mouseButton == MouseButton.Right)
            {
                sensorViewModel.ShowHistogram();
            }
        }
    }


    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ViewModel ViewModel => DataContext as ViewModel;

        public MainWindow()
        {
            InitializeComponent();

            DataContext = new ViewModel();

            CompositionTarget.Rendering += OnRendering;
        }

        private void OnRendering(object sender, EventArgs e)
        {
            ViewModel.Update();
        }

        private void ResetZoomBtn_Click(object sender, RoutedEventArgs e)
        {
            TimelineGraph.ResetZoom();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            foreach (var info in ViewModel.SensorInfos)
            {
                info.HistogramWindow.Close();
            }
        }

        private void StartAnimationBtn_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.StartAnimation();
        }

        private void PauseAnimationBtn_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.PauseAnimation();
        }

        private void StopAnimationBtn_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.StopAnimation();
        }

        private void Sensor_MouseDown(object sender, MouseButtonEventArgs e)
        {
            (sender as Shape).CaptureMouse();
        }

        private void Sensor_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var shape = sender as Shape;

            if (shape.IsMouseCaptured)
            {
                shape.ReleaseMouseCapture();

                if (shape.IsMouseOver)
                    ViewModel.OnSensorClicked(shape.DataContext as SensorViewModel, e.ChangedButton);
            }
        }

        private void LoadSettingsBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                ViewModel.ApplySettings(Settings.FromHJson(openFileDialog.FileName));
            }
        }

        private void SaveSettingsBtn_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            if (saveFileDialog.ShowDialog() == true)
            {
                ViewModel.GenerateSettings().ToHJson(saveFileDialog.FileName);
            }
        }
    }
}
