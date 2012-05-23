using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Windows.Media;
using System.Diagnostics;
using System.Threading;

namespace myManga.UI
{
    /// <summary>
    /// Interaction logic for ToastNotification.xaml
    /// </summary>
    [DebuggerStepThrough]
    public partial class ToastNotification : UserControl
    {
        private Boolean PauseToast { get; set; }

        public class ToastMessage
        {
            public String Message { get; set; }
            public TimeSpan DisplayTime { get; set; }

            public ToastMessage()
            { Message = ""; DisplayTime = TimeSpan.FromMilliseconds((Int32)DisplayLength.Short); }
            public ToastMessage(String Message, TimeSpan DisplayTime)
            { this.Message = Message; this.DisplayTime = DisplayTime; }
            public ToastMessage(String Message, DisplayLength DisplayLength)
            { this.Message = Message; this.DisplayTime = TimeSpan.FromMilliseconds((Int32)DisplayLength); }
            public ToastMessage(String Message, Int32 Milliseconds)
            { this.Message = Message; this.DisplayTime = TimeSpan.FromMilliseconds(Milliseconds); }
            public ToastMessage(ToastMessage ToastMessage) 
            { Message = ToastMessage.Message; DisplayTime = ToastMessage.DisplayTime; }
        }
        
        private delegate void ToastNotificationInvoke(ToastMessage tMsg);
        
        private enum DisplayStatus { Shown, Hidden, Showing, Hiding }
        private DisplayStatus dStatus { get; set; }

        #region Fading
        private Storyboard ToastFade { get; set; }
        private DispatcherTimer ToastTimer { get; set; }

        private Queue<ToastMessage> ToastQueue { get; set; }
        #endregion

        public ToastNotification()
        {
            PauseToast = false;

            InitializeComponent();
            Visibility = System.Windows.Visibility.Collapsed;
            ToastQueue = new Queue<ToastMessage>();
            ToastTimer = new DispatcherTimer();
            ToastTimer.Tick += ToastTimer_Tick;

            Opacity = 0;
            (ToastTransformGroup.Children[0] as TranslateTransform).Y = this.ActualHeight;
            (ToastTransformGroup.Children[1] as ScaleTransform).ScaleX = 
                (ToastTransformGroup.Children[1] as ScaleTransform).ScaleY = 0.5;

            dStatus = DisplayStatus.Hidden;
            Object tmpResource;
            if ((tmpResource = FindResource("ToastFade")) != null)
                ToastFade = (tmpResource as Storyboard);
            tmpResource = null;
            ToastFade.Completed += ToastFade_Completed;
        }

        public void ResumeToasts()
        { PauseToast = false; ShowNewToast(); }
        public void PauseToasts()
        { PauseToast = true; }

        private void ToastNotification_MouseDown(object sender, MouseButtonEventArgs e)
        {
            FadeOutBegin();
        }

        private void ToastTimer_Tick(object sender, EventArgs e)
        {
            ToastTimer.Stop();
            FadeOutBegin();
        }

        private void FadeOutBegin()
        {
            dStatus = DisplayStatus.Hiding;
            ToastFade.Stop();
            (ToastFade.Children[1] as DoubleAnimation).From = null;//Translate
            (ToastFade.Children[0] as DoubleAnimation).To = 0; //Opacity
            (ToastFade.Children[1] as DoubleAnimation).To = this.ActualHeight; //Translate
            (ToastFade.Children[2] as DoubleAnimation).To = //ScaleX
                (ToastFade.Children[3] as DoubleAnimation).To = 0.5; //ScaleY

            ToastFade.Begin();
        }

        private void FadeInBegin()
        {
            dStatus = DisplayStatus.Showing;
            ToastFade.Stop();
            (ToastFade.Children[0] as DoubleAnimation).To = //Opacity
                (ToastFade.Children[2] as DoubleAnimation).To = //ScaleX
                (ToastFade.Children[3] as DoubleAnimation).To = 1; //ScaleY
            (ToastFade.Children[1] as DoubleAnimation).To = 0; //Translate
            (ToastFade.Children[1] as DoubleAnimation).From = (-1 * this.ActualHeight); //Translate
            ToastFade.Begin();
        }

        private void ToastFade_Completed(object sender, EventArgs e)
        {
            switch (dStatus)
            {
                default: break;

                case DisplayStatus.Showing:
                    dStatus = DisplayStatus.Shown;
                    ToastTimer.Start();
                    break;

                case DisplayStatus.Hiding:
                    dStatus = DisplayStatus.Hidden;
                    ShowNewToast();
                    break;
            }
        }

        private void ShowNewToast()
        {
            if (dStatus.Equals(DisplayStatus.Hidden))
            {
                if (ToastQueue.Count > 0 && !PauseToast)
                {
                    System.Threading.Thread _T = System.Threading.Thread.CurrentThread;
                    Visibility = System.Windows.Visibility.Visible;
                    ToastMessage tm = ToastQueue.Dequeue();
                    ToastTimer.Interval = tm.DisplayTime;
                    NotificationText.Text = tm.Message;
                    FadeInBegin();
                }
                else
                    Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        public enum DisplayLength { Short = 1000, Normal = 2000, Long = 3000 }

        public void ShowToast(String Message) 
        { ShowToast(Message, TimeSpan.FromSeconds(5)); }
        public void ShowToast(String Message, DisplayLength DisplayLength)
        { ShowToast(Message, (Int32)DisplayLength); }
        public void ShowToast(String Message, Int32 Milliseconds) 
        { ShowToast(Message, TimeSpan.FromMilliseconds(Milliseconds)); }
        public void ShowToast(String Message, TimeSpan ShowTime)
        { ShowToast(new ToastMessage() { Message = Message, DisplayTime = ShowTime }); }
        public void ShowToast(ToastMessage ToastMessage)
        {
            if (this.Dispatcher.Thread == Thread.CurrentThread)
            {
                ToastQueue.Enqueue(ToastMessage);
                ShowNewToast();
            }
            else
            {
                this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                    new ToastNotificationInvoke(ShowToast), ToastMessage);
            }
        }
    }    
}
