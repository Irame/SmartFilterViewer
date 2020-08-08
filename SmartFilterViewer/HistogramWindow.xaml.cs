using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SmartFilterViewer
{
    /// <summary>
    /// Interaktionslogik für HistogramWindow.xaml
    /// </summary>
    public partial class HistogramWindow : Window
    {
        private double maxValue;
        private Brush barBrush;

        private SensorDataList sensorDataList;
        public SensorDataList SensorDataList
        { 
            get => sensorDataList;
            set {
                sensorDataList = value;
                UpdateMaxValue();
                UpdateBars();
            } 
        }

        private double lerpIdx;
        public double LerpIdx { 
            get => lerpIdx;
            set
            {
                lerpIdx = value;
                UpdateBars();
            }
        }

        private Color barColor;
        public Color BarColor
        {
            get => barColor;
            set
            {
                barColor = value;
                barBrush = new SolidColorBrush(barColor);
                barBrush.Freeze();
                UpdateBarColors();
            }
        }

        public HistogramWindow()
        {
            InitializeComponent();
        }

        public void UpdateBars()
        {
            if (!IsVisible || maxValue == 0 || SensorDataList == null)
                return;

            var rowHeight = BarRow.ActualHeight;

            var sensordataType = typeof(SensorData);

            Application.Current?.Dispatcher.Invoke(() =>
            {
                Bin00Bar.Height = SensorDataList.GetLerpValue(lerpIdx, sensordataType.GetProperty(nameof(SensorData.Bin00))) / maxValue * rowHeight;
                Bin01Bar.Height = SensorDataList.GetLerpValue(lerpIdx, sensordataType.GetProperty(nameof(SensorData.Bin01))) / maxValue * rowHeight;
                Bin02Bar.Height = SensorDataList.GetLerpValue(lerpIdx, sensordataType.GetProperty(nameof(SensorData.Bin02))) / maxValue * rowHeight;
                Bin03Bar.Height = SensorDataList.GetLerpValue(lerpIdx, sensordataType.GetProperty(nameof(SensorData.Bin03))) / maxValue * rowHeight;
                Bin04Bar.Height = SensorDataList.GetLerpValue(lerpIdx, sensordataType.GetProperty(nameof(SensorData.Bin04))) / maxValue * rowHeight;
                Bin05Bar.Height = SensorDataList.GetLerpValue(lerpIdx, sensordataType.GetProperty(nameof(SensorData.Bin05))) / maxValue * rowHeight;
                Bin06Bar.Height = SensorDataList.GetLerpValue(lerpIdx, sensordataType.GetProperty(nameof(SensorData.Bin06))) / maxValue * rowHeight;
                Bin07Bar.Height = SensorDataList.GetLerpValue(lerpIdx, sensordataType.GetProperty(nameof(SensorData.Bin07))) / maxValue * rowHeight;
                Bin08Bar.Height = SensorDataList.GetLerpValue(lerpIdx, sensordataType.GetProperty(nameof(SensorData.Bin08))) / maxValue * rowHeight;
                Bin09Bar.Height = SensorDataList.GetLerpValue(lerpIdx, sensordataType.GetProperty(nameof(SensorData.Bin09))) / maxValue * rowHeight;
                Bin10Bar.Height = SensorDataList.GetLerpValue(lerpIdx, sensordataType.GetProperty(nameof(SensorData.Bin10))) / maxValue * rowHeight;
                Bin11Bar.Height = SensorDataList.GetLerpValue(lerpIdx, sensordataType.GetProperty(nameof(SensorData.Bin11))) / maxValue * rowHeight;
                Bin12Bar.Height = SensorDataList.GetLerpValue(lerpIdx, sensordataType.GetProperty(nameof(SensorData.Bin12))) / maxValue * rowHeight;
                Bin13Bar.Height = SensorDataList.GetLerpValue(lerpIdx, sensordataType.GetProperty(nameof(SensorData.Bin13))) / maxValue * rowHeight;
                Bin14Bar.Height = SensorDataList.GetLerpValue(lerpIdx, sensordataType.GetProperty(nameof(SensorData.Bin14))) / maxValue * rowHeight;
                Bin15Bar.Height = SensorDataList.GetLerpValue(lerpIdx, sensordataType.GetProperty(nameof(SensorData.Bin15))) / maxValue * rowHeight;
                Bin16Bar.Height = SensorDataList.GetLerpValue(lerpIdx, sensordataType.GetProperty(nameof(SensorData.Bin16))) / maxValue * rowHeight;
                Bin17Bar.Height = SensorDataList.GetLerpValue(lerpIdx, sensordataType.GetProperty(nameof(SensorData.Bin17))) / maxValue * rowHeight;
                Bin18Bar.Height = SensorDataList.GetLerpValue(lerpIdx, sensordataType.GetProperty(nameof(SensorData.Bin18))) / maxValue * rowHeight;
                Bin19Bar.Height = SensorDataList.GetLerpValue(lerpIdx, sensordataType.GetProperty(nameof(SensorData.Bin19))) / maxValue * rowHeight;
                Bin20Bar.Height = SensorDataList.GetLerpValue(lerpIdx, sensordataType.GetProperty(nameof(SensorData.Bin20))) / maxValue * rowHeight;
                Bin21Bar.Height = SensorDataList.GetLerpValue(lerpIdx, sensordataType.GetProperty(nameof(SensorData.Bin21))) / maxValue * rowHeight;
                Bin22Bar.Height = SensorDataList.GetLerpValue(lerpIdx, sensordataType.GetProperty(nameof(SensorData.Bin22))) / maxValue * rowHeight;
                Bin23Bar.Height = SensorDataList.GetLerpValue(lerpIdx, sensordataType.GetProperty(nameof(SensorData.Bin23))) / maxValue * rowHeight;
            });
        }

        private void UpdateBarColors()
        {
            Bin00Bar.Fill = barBrush;
            Bin01Bar.Fill = barBrush;
            Bin02Bar.Fill = barBrush;
            Bin03Bar.Fill = barBrush;
            Bin04Bar.Fill = barBrush;
            Bin05Bar.Fill = barBrush;
            Bin06Bar.Fill = barBrush;
            Bin07Bar.Fill = barBrush;
            Bin08Bar.Fill = barBrush;
            Bin09Bar.Fill = barBrush;
            Bin10Bar.Fill = barBrush;
            Bin11Bar.Fill = barBrush;
            Bin12Bar.Fill = barBrush;
            Bin13Bar.Fill = barBrush;
            Bin14Bar.Fill = barBrush;
            Bin15Bar.Fill = barBrush;
            Bin16Bar.Fill = barBrush;
            Bin17Bar.Fill = barBrush;
            Bin18Bar.Fill = barBrush;
            Bin19Bar.Fill = barBrush;
            Bin20Bar.Fill = barBrush;
            Bin21Bar.Fill = barBrush;
            Bin22Bar.Fill = barBrush;
            Bin23Bar.Fill = barBrush;
        }

        public void UpdateMaxValue()
        {
            var props = typeof(SensorData).GetProperties().Where(x => x.Name.StartsWith("Bin")).ToList();
            maxValue = SensorDataList.SelectMany(x => props.Select(p => (double)p.GetValue(x))).Max();
        }

        private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateBars();
        }
    }
}
