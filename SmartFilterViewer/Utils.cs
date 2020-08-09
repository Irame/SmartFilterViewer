using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SmartFilterViewer
{
    static class Utils
    {
        static public Size MeasureString(string candidate, TextBlock textBlock)
        {
            var formattedText = new FormattedText(
                candidate,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(textBlock.FontFamily, textBlock.FontStyle, textBlock.FontWeight, textBlock.FontStretch),
                textBlock.FontSize,
                Brushes.Black,
                new NumberSubstitution(),
                1);

            return new Size(formattedText.Width, formattedText.Height);
        }

        static public string NegativeFormat(this TimeSpan timeSpan, string format = null)
        {
            if (string.IsNullOrEmpty(format))
                return timeSpan.ToString();
            else
                return (timeSpan.Ticks < 0 ? "-" : "") + timeSpan.ToString(format);
        }
    }
}
