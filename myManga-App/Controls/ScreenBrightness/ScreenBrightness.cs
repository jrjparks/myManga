using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace myManga_App.Controls.ScreenBrightness
{
    public sealed class ScreenBrightness : Slider
    {
        public ScreenBrightness()
            : base()
        {
            Dispatcher.ShutdownStarted += (s, e) => MonitorBrightness.Instance.Dispose();
            IsSnapToTickEnabled = true;
            LargeChange = 5;
            SmallChange = 1;

            MonitorBrightness.Instance.MonitorBrightnessChanged += brightnessManager_MonitorBrightnessChange;
            if (DesignerProperties.GetIsInDesignMode(this))
            {
                Minimum = Maximum = Value = 0;
                IsEnabled = false;
            }
            else if (MonitorBrightness.Instance.SupportsControl)
            {
                Minimum = 0;
                Maximum = MonitorBrightness.Instance.BrightnessLevels.Count() - 1;
                Value = MonitorBrightness.Instance.IndexOfByte(MonitorBrightness.Instance.CurrentBrightness);
                ValueChanged += Slider_ValueChanged;
                MonitorBrightness.Instance.StartMonitorWatch();
            }
            else
            {
#if !DEBUG
                Visibility = Visibility.Hidden;
                MonitorBrightness.Instance.Dispose();
#endif
            }
        }

        #region Monitor Value Update
        private void brightnessManager_MonitorBrightnessChange(Object sender, GenericEventArgs<Byte> e)
        {
            if (Equals(Dispatcher.Thread, Thread.CurrentThread))
                UpdateBrightness(e.Value);
            else
                Dispatcher.Invoke(new UpdateBrightnessDelegate(UpdateBrightness), e);
        }
        private delegate void UpdateBrightnessDelegate(Byte b);
        private void UpdateBrightness(Byte b)
        {
            ValueChanged -= Slider_ValueChanged;
            Value = MonitorBrightness.Instance.IndexOfByte(b);
            ValueChanged += Slider_ValueChanged;
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<Double> e)
        { MonitorBrightness.Instance.CurrentBrightness = MonitorBrightness.Instance.BrightnessLevels[(Int32)Value]; }
        #endregion
    }
}
