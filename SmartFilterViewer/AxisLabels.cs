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
    public enum AxisLableTickPosition { Start, End };

    public enum AxisLabelTextAlignment { Start, Center, End };

    public class TickInfo
    {
        public double Pos;
        public string Label;
    }

    public class AxisLabels : Grid
    {
        private Orientation orientation;
        public Orientation Orientation
        {
            get => orientation;
            set
            {
                orientation = value;

                RowDefinitions.Clear();
                ColumnDefinitions.Clear();

                if (orientation == Orientation.Horizontal)
                {
                    RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    RowDefinitions.Add(new RowDefinition { Height = new GridLength(3) });
                    RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });  // TODO: Should be 1* if Width is set
                    RowDefinitions.Add(new RowDefinition { Height = new GridLength(3) });
                    RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                }
                else
                {
                    ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6) });
                    ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });  // TODO: Should be 1* if Width is set
                    ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6) });
                    ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                }
            }
        }

        public AxisLableTickPosition TickPosition { get; set; }
        public AxisLabelTextAlignment TextAlignment { get; set; } = AxisLabelTextAlignment.Center;

        public double TickLength { get; set; } = 8;

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



        public string DescLabel
        {
            get { return (string)GetValue(DescLabelProperty); }
            set { SetValue(DescLabelProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DescLabel.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DescLabelProperty =
            DependencyProperty.Register("DescLabel", typeof(string), typeof(AxisLabels), new PropertyMetadata(null, DescLabelChanged));
        private static void DescLabelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => (d as AxisLabels).OnDescLabelChanged();


        private List<TickControls> tickControlsList;
        private TextBlock descLabel;

        public AxisLabels() : base()
        {
            tickControlsList = new List<TickControls>();

            Loaded += (s, e) => CalcMaxTickCount();
        }

        private double length => Orientation == Orientation.Horizontal ? ActualWidth : ActualHeight;

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

        private void OnDescLabelChanged()
        {
            if (DescLabel != null)
            {
                if (descLabel == null)
                {
                    descLabel = new TextBlock();
                    Children.Add(descLabel);
                }

                if (Orientation == Orientation.Vertical)
                {
                    descLabel.LayoutTransform = new RotateTransform(-90);
                    descLabel.VerticalAlignment = VerticalAlignment.Center;
                }
                else
                {
                    descLabel.HorizontalAlignment = HorizontalAlignment.Center;
                    descLabel.LayoutTransform = null;
                }

                descLabel.Text = DescLabel;
            }
            else if (descLabel != null)
            {
                Children.Remove(descLabel);
                descLabel = null;
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
        }

        private class TickControls : IDisposable
        {
            private AxisLabels parent;

            private double relPos;
            private Size textSize;

            public TextBlock Text;
            public Rectangle tick;

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

                tick = new Rectangle();
                tick.Fill = tickFillBrush;
                parent.Children.Add(tick);

                if (parent.Orientation == Orientation.Horizontal)
                {
                    switch (parent.TextAlignment)
                    {
                        case AxisLabelTextAlignment.Start:
                            Text.VerticalAlignment = VerticalAlignment.Top; break;
                        case AxisLabelTextAlignment.Center:
                            Text.VerticalAlignment = VerticalAlignment.Center; break;
                        case AxisLabelTextAlignment.End:
                            Text.VerticalAlignment = VerticalAlignment.Bottom; break;
                    }
                    
                    Text.HorizontalAlignment = HorizontalAlignment.Left;
                    SetRow(Text, 2);

                    tick.HorizontalAlignment = HorizontalAlignment.Left;
                    tick.Width = 1;
                    tick.Height = parent.TickLength;

                    if (parent.TickPosition == AxisLableTickPosition.Start)
                        SetRow(tick, 0);
                    else
                        SetRow(tick, 4);
                }
                else
                {
                    switch (parent.TextAlignment)
                    {
                        case AxisLabelTextAlignment.Start:
                            Text.HorizontalAlignment = HorizontalAlignment.Left; break;
                        case AxisLabelTextAlignment.Center:
                            Text.HorizontalAlignment = HorizontalAlignment.Center; break;
                        case AxisLabelTextAlignment.End:
                            Text.HorizontalAlignment = HorizontalAlignment.Right; break;
                    }

                    Text.VerticalAlignment = VerticalAlignment.Top;
                    SetColumn(Text, 2);

                    tick.VerticalAlignment = VerticalAlignment.Top;
                    tick.Height = 1;
                    tick.Width = parent.TickLength;

                    if (parent.TickPosition == AxisLableTickPosition.Start)
                        SetColumn(tick, 0);
                    else
                        SetColumn(tick, 4);
                }
            }

            public void Dispose()
            {
                parent.Children.Remove(Text);
                parent.Children.Remove(tick);
            }

            public void Update(TickInfo info)
            {
                Text.Text = info.Label;
                textSize = Utils.MeasureString(info.Label, Text);
                relPos = parent.Orientation == Orientation.Horizontal ? info.Pos : 1 - info.Pos;

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
                    tick.Margin = new Thickness(pos, 0, 0, 0);
                    Text.Margin = new Thickness(textPos, 0, 0, 0);
                }
                else
                {
                    tick.Margin = new Thickness(0, pos, 0, 0);
                    Text.Margin = new Thickness(0, textPos, 0, 0);
                }
            }
        }
    }
}
