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
        private bool isScrubbing;

        public bool IsScrubbing
        {
            get => isScrubbing;
            set
            {
                isScrubbing = value;
                IsScrubbingChanged(this, isScrubbing);
            }
        }

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

            ScrubbedToValue?.Invoke(this, Value);
        }

        public event EventHandler<double> ScrubbedToValue;
        public event EventHandler<bool> IsScrubbingChanged;
    }
}
