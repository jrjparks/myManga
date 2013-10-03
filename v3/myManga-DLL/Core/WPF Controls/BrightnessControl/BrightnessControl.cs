using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Threading;
using Core.WPF_Controls.BrightnessControl;

namespace System.Windows.Controls
{
    public sealed class BrightnessControl : Slider
    {
        private Boolean IsInDesignerMode
        { get { return DesignerProperties.GetIsInDesignMode(new DependencyObject()); } }

        public BrightnessControl()
            : base()
        {
            this.Dispatcher.ShutdownStarted += Dispatcher_ShutdownStarted;
            this.IsSnapToTickEnabled = true;
            this.LargeChange = 5;
            this.SmallChange = 1;

            MonitorBrightness.Instance.MonitorBrightnessChanged += brightnessManager_MonitorBrightnessChange;

            if (MonitorBrightness.Instance.SupportsControl)
            {
                this.Minimum = 0;
                this.Maximum = MonitorBrightness.Instance.BrightnessLevels.Count() - 1;
                this.Value = MonitorBrightness.Instance.IndexOfByte(MonitorBrightness.Instance.CurrentBrightness);
                this.ValueChanged += Slider_ValueChanged;
            }
            else if (IsInDesignerMode)
            {
                this.Minimum = this.Maximum = this.Value = 0;
                this.IsEnabled = false;
            }
            else
            {
#if RELEASE
                this.Visibility = System.Windows.Visibility.Hidden;
                MonitorBrightness.Instance.Dispose();
#endif
            }
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<Double> e)
        {
            Int32 Index = (Int32)this.Value;
            MonitorBrightness.Instance.CurrentBrightness = MonitorBrightness.Instance.BrightnessLevels[Index];
        }

        #region Monitor Value Update
        private void brightnessManager_MonitorBrightnessChange(Object sender, Byte b)
        {
            if (this.Dispatcher.Thread == Thread.CurrentThread)
                UpdateBrightness(b);
            this.Dispatcher.Invoke(new UpdateBrightnessDelegate(UpdateBrightness), b);
        }
        private delegate void UpdateBrightnessDelegate(Byte b);
        private void UpdateBrightness(Byte b)
        {
            this.ValueChanged -= Slider_ValueChanged;
            this.Value = MonitorBrightness.Instance.IndexOfByte(b);
            this.ValueChanged += Slider_ValueChanged;
        }
        #endregion

        #region Dispose
        private void Dispatcher_ShutdownStarted(object sender, EventArgs e)
        {
            MonitorBrightness.Instance.Dispose();
        }
        #endregion
    }
}
