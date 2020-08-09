using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SmartFilterViewer
{
    public enum AxisLableTickPosition { Start, End, Both };

    public class TickInfo
    {
        public double Pos;
        public string Label;
    }

    public class AxisLabels : Grid
    {
        public Orientation Orientation { get; set; }

        public AxisLableTickPosition TickPosition { get; set; }

        public double MinTickSpacing { get; set; } = 100;

        public IEnumerable<TickInfo> TickInfos
        {
            get { return (IEnumerable<TickInfo>)GetValue(TickInfosProperty); }
            set { SetValue(TickInfosProperty, value); }
        }
        public static readonly DependencyProperty TickInfosProperty =
            DependencyProperty.Register("TickInfos", typeof(IEnumerable<TickInfo>), typeof(AxisLabels), new PropertyMetadata(null, TickInfosChanged));
        private static void TickInfosChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => (d as AxisLabels).OnTickInfosChanged();

        public int MaxTickCount
        {
            get { return (int)GetValue(MaxTickCountProperty); }
            set { SetValue(MaxTickCountProperty, value); }
        }
        public static readonly DependencyProperty MaxTickCountProperty =
            DependencyProperty.Register("MaxTickCount", typeof(int), typeof(AxisLabels), new PropertyMetadata(0));


        private List<TickControls> tickControlsList;

        public AxisLabels() : base()
        {
            tickControlsList = new List<TickControls>();

            Loaded += (s, e) => CalcMaxTickCount();
        }

        private double length => Orientation == Orientation.Horizontal ? ActualWidth : ActualHeight;
        private double space => Orientation == Orientation.Horizontal ? ActualHeight : ActualWidth;

        private void CalcMaxTickCount()
        {
            MaxTickCount = (int)Math.Floor(length / MinTickSpacing) + 1;
        }

        private bool hasTickInfosChanged;

        private void OnTickInfosChanged()
        {
            hasTickInfosChanged = true;

            var TickInfosList = TickInfos?.ToList() ?? new List<TickInfo>();

            for (int i = 0; i < TickInfosList.Count; i++)
            {
                if (tickControlsList.Count == i)
                    tickControlsList.Add(new TickControls(this));

                tickControlsList[i].Update(TickInfosList[i]);
            }

            while (tickControlsList.Count > TickInfosList.Count)
            {
                tickControlsList[tickControlsList.Count - 1].Dispose();
                tickControlsList.RemoveAt(tickControlsList.Count - 1);
            }
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);

            if (Orientation == Orientation.Horizontal && sizeInfo.WidthChanged
                || Orientation == Orientation.Vertical && sizeInfo.HeightChanged)
            {
                hasTickInfosChanged = false;
                CalcMaxTickCount();
                if (!hasTickInfosChanged)
                {
                    foreach (var item in tickControlsList)
                    {
                        item.UpdateTickPosition();
                    }
                }
            }

            if (Orientation == Orientation.Horizontal && sizeInfo.HeightChanged
                || Orientation == Orientation.Vertical && sizeInfo.WidthChanged)
            {
                foreach(var item in tickControlsList)
                {
                    item.UpdateTickLengths();
                }
            }
        }

        private class TickControls : IDisposable
        {
            private AxisLabels parent;

            private double relPos;
            private Size textSize;

            public TextBlock Text;
            public Rectangle tickStart;
            public Rectangle tickEnd;

            private static Brush tickFillBrush;

            static TickControls()
            {
                tickFillBrush = new SolidColorBrush(Colors.Black);
                tickFillBrush.Freeze();
            }

            public TickControls(AxisLabels parent)
            {
                this.parent = parent;

                Text = new TextBlock();
                parent.Children.Add(Text);

                if (parent.TickPosition == AxisLableTickPosition.Start || parent.TickPosition == AxisLableTickPosition.Both)
                {
                    tickStart = new Rectangle();
                    tickStart.Fill = tickFillBrush;
                    parent.Children.Add(tickStart);
                }

                if (parent.TickPosition == AxisLableTickPosition.End || parent.TickPosition == AxisLableTickPosition.Both)
                {
                    tickEnd = new Rectangle();
                    tickEnd.Fill = tickFillBrush;
                    parent.Children.Add(tickEnd);
                }

                if (parent.Orientation == Orientation.Horizontal)
                {
                    Text.VerticalAlignment = VerticalAlignment.Center;
                    Text.HorizontalAlignment = HorizontalAlignment.Left;

                    if (tickStart != null)
                    {
                        tickStart.VerticalAlignment = VerticalAlignment.Top;
                        tickStart.HorizontalAlignment = HorizontalAlignment.Left;
                        tickStart.Width = 1;
                    }

                    if (tickEnd != null)
                    {
                        tickEnd.VerticalAlignment = VerticalAlignment.Bottom;
                        tickEnd.HorizontalAlignment = HorizontalAlignment.Left;
                        tickEnd.Width = 1;
                    }
                }
                else
                {
                    Text.VerticalAlignment = VerticalAlignment.Top;
                    Text.HorizontalAlignment = HorizontalAlignment.Center;

                    if (tickStart != null)
                    {
                        tickStart.VerticalAlignment = VerticalAlignment.Top;
                        tickStart.HorizontalAlignment = HorizontalAlignment.Left;
                        tickStart.Height = 1;
                    }

                    if (tickEnd != null)
                    {
                        tickEnd.VerticalAlignment = VerticalAlignment.Top;
                        tickEnd.HorizontalAlignment = HorizontalAlignment.Right;
                        tickEnd.Height = 1;
                    }
                }
            }

            public void Dispose()
            {
                parent.Children.Remove(Text);
                parent.Children.Remove(tickStart);
                parent.Children.Remove(tickEnd);
            }

            public void Update(TickInfo info)
            {
                Text.Text = info.Label;
                textSize = Utils.MeasureString(info.Label, Text);
                relPos = parent.Orientation == Orientation.Horizontal ? info.Pos : 1-info.Pos;

                UpdateTickLengths();
                UpdateTickPosition();
            }

            public void UpdateTickPosition()
            {
                var pos = relPos * (parent.length - 1);

                var textLength = parent.Orientation == Orientation.Horizontal ? textSize.Width : textSize.Height;

                var textPos = pos - textLength / 2;

                textPos = Math.Min(textPos, parent.length - textLength);
                textPos = Math.Max(textPos, 0);

                if (parent.Orientation == Orientation.Horizontal)
                {
                    if (tickStart != null)
                        tickStart.Margin = new Thickness(pos, 0, 0, 0);

                    if (tickEnd != null)
                        tickEnd.Margin = new Thickness(pos, 0, 0, 0);

                    Text.Margin = new Thickness(textPos, 0, 0, 0);
                }
                else
                {
                    if (tickStart != null)
                        tickStart.Margin = new Thickness(0, pos, 0, 0);

                    if (tickEnd != null)
                        tickEnd.Margin = new Thickness(0, pos, 0, 0);

                    Text.Margin = new Thickness(0, textPos, 0, 0);
                }
            }

            public void UpdateTickLengths()
            {
                var textSpace = parent.Orientation == Orientation.Horizontal ? textSize.Height : textSize.Width;
                var tickLengths = Math.Max(0, (parent.space - textSpace) / 2 - 2);

                if (parent.Orientation == Orientation.Horizontal)
                {
                    if (tickStart != null)
                        tickStart.Height = tickLengths;

                    if (tickEnd != null)
                        tickEnd.Height = tickLengths;
                }
                else
                {
                    if (tickStart != null)
                        tickStart.Width = tickLengths;

                    if (tickEnd != null)
                        tickEnd.Width = tickLengths;
                }
            }
        }
    }
}
