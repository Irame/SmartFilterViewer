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
    }

    public class ViewModel : PropertyChangedBase
    {
        private DateTime currentDataTime;
        private double playbackFactor;

        public List<SensorViewModel> SensorInfos { get; }

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

        public DateTime lastTimerTick;
        public DateTime dataStartTime;
        public double dataTimeInMillisec;

        private PropertyInfo propInfo;
        public double maxValue;

        public bool isPaused;
        private int heatLegendMaxTickCount;
        private int graphLegendMaxTickCount;
        private int timelineMaxTickCount;
        private List<TickInfo> timelineTickInfos;
        private List<TickInfo> heatLegendTickInfos;
        private List<TickInfo> graphLegendTickInfos;

        public bool IsScrubbing { get; set; }


        public PropertyInfo PropInfo
        {
            get => propInfo;
            set
            {
                propInfo = value;
                CalcMaxValiue();
                OnNotifyPropertyChanged(nameof(TimelineGraphData));
            }
        }

        public IEnumerable<SensorViewModel> ValidSensorInfos => SensorInfos.Where(x => x.DataList != null);

        public ViewModel()
        {
            PlaybackFactor = 1;

            propInfo = typeof(SensorData).GetProperty(nameof(SensorData.PM2_5_ug_m3));

            isPaused = true;

            SensorInfos = new List<SensorViewModel>();
            for (int i = 0; i < 9; i++)
            {
                var sensorVM = new SensorViewModel { GraphColor = HsvColor.FromHSV(180 + i * 15, 1, 1) };
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

                        OnNotifyPropertyChanged(nameof(ValidSensorInfos));
                        OnNotifyPropertyChanged(nameof(TimelineGraphData));
                    }
                };
            }
        }

        private void CalcMaxValiue()
        {
            if (ValidSensorInfos.Any())
                maxValue = ValidSensorInfos.SelectMany(x => x.DataList).Select(x => GetValue(x)).Max();

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

        private List<TickInfo> GenerateValueTickInfo(int maxTickCount)
        {
            var tickDist = Math.Ceiling((maxValue / maxTickCount) / 100) * 100;

            var tickInfos = new List<TickInfo>();
            tickInfos.Add(new TickInfo { Pos = 0, Label = "0" });
            for (double val = tickDist; val < maxValue - tickDist / 2; val += tickDist)
            {
                tickInfos.Add(new TickInfo { Pos = val / maxValue, Label = $"{val}" });
            }
            tickInfos.Add(new TickInfo { Pos = 1, Label = $"{maxValue}" });
            return tickInfos;
        }

        private void UpdateTimelineTickInfos()
        {
            var tickCount = Math.Min(timelineMaxTickCount, 100);

            var tickGap = TimeSpan.FromMilliseconds(dataTimeInMillisec / (tickCount - 1));
            var curTick = TimeSpan.Zero;

            var newTickInfos = new List<TickInfo>();
            for (int i = 0; i < tickCount; i++)
            {
                newTickInfos.Add(new TickInfo { Pos = (double)i / (tickCount - 1), Label = curTick.ToString(@"hh\:mm\:ss") });
                curTick += tickGap;
            }
            TimelineTickInfos = newTickInfos;
        }


        public void Update()
        {
            var currentTime = DateTime.Now;

            if (!isPaused && !IsScrubbing)
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
                factor = Math.Pow(factor, 0.3);
                info.ShapeColor = HsvColor.FromHSV((float)((1 - factor) * 120), 1, 1);
            }
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

        private void LoadDataForSensor(SensorViewModel info)
        {
            ViewModel.isPaused = true;

            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                info.DataList = SensorDataList.FromFile(openFileDialog.FileName);
            }

            ViewModel.lastTimerTick = DateTime.Now;
            ViewModel.isPaused = false;
        }

        private void ResetZoomBtn_Click(object sender, RoutedEventArgs e)
        {
            TimelineGraph.ResetZoom();
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
            ValuesComboBox.SelectedItem = dropDownItems.FirstOrDefault(x => x.PropertyInfo == ViewModel.PropInfo);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            foreach (var info in ViewModel.SensorInfos)
            {
                info.HistogramWindow.Close();
            }
        }

        private void ValuesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ViewModel.PropInfo = (ValuesComboBox.SelectedItem as PropertyDropdownItem).PropertyInfo;
        }

        private void StartAnimationBtn_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.lastTimerTick = DateTime.Now;
            ViewModel.isPaused = false;
        }

        private void Sensor_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var shape = sender as Shape;
            var info = shape.DataContext as SensorViewModel;

            if (e.ChangedButton == MouseButton.Left)
            {
                LoadDataForSensor(info);
            }
            else if (e.ChangedButton == MouseButton.Right)
            {
                if (info.HistogramWindow.IsVisible)
                    info.HistogramWindow.Activate();
                else
                    info.HistogramWindow.Show();
            }
        }
    }
}
