using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace SmartFilterViewer
{
    class HsvColor
    {
        public static Color FromHSV(float h, float s, float v)
        {
            int h_i = (int)Math.Floor(h / 60);
            var f = h / 60 - h_i;

            var p = v * (1 - s);
            var q = v * (1 - s * f);
            var t = v * (1 - s * (1 - f));
            
            switch (h_i)
            {
                case 0:
                case 6:
                    return Color.FromScRgb(1, v, t, p);
                case 1:
                    return Color.FromScRgb(1, q, v, p);
                case 2:
                    return Color.FromScRgb(1, p, v, t);
                case 3:
                    return Color.FromScRgb(1, p, q, v);
                case 4:
                    return Color.FromScRgb(1, t, p, v);
                case 5:
                    return Color.FromScRgb(1, v, p, q);
                default: return Colors.Black;
            }
        }
    }
}
