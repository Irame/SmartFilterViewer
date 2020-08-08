using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SmartFilterViewer
{
    class ScrubProgressBar : ProgressBar
    {
        public bool IsScrubbing
        {
            get { return (bool)GetValue(IsScrubbingProperty); }
            set { SetValue(IsScrubbingProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsScrubbing.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsScrubbingProperty =
            DependencyProperty.Register("IsScrubbing", typeof(bool), typeof(ScrubProgressBar), new PropertyMetadata(false));



        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            CaptureMouse();
            IsScrubbing = true;
            SetValueForMousePos(e.GetPosition(this));
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (IsMouseCaptured)
            {
                SetValueForMousePos(e.GetPosition(this));
            }
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            if (IsMouseCaptured)
            {
                SetValueForMousePos(e.GetPosition(this));
                IsScrubbing = false;
                ReleaseMouseCapture();
            }
        }

        private void SetValueForMousePos(Point mousePos)
        {
            Value = (Maximum - Minimum) * (mousePos.X / ActualWidth);
        }
    }
}
