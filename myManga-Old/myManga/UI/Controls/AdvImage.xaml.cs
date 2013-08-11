using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.ComponentModel;
using System.Windows.Input;
using myManga.Properties;

namespace myManga.UI
{
    /// <summary>
    /// Interaction logic for AdvImage.xaml
    /// </summary>
    public partial class AdvImage : UserControl, INotifyPropertyChanged
    {
        #region INotifyPropertyChanged Members
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(String Name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(Name));
        }
        #endregion

        #region Vars
        public static DependencyProperty ImageZoomProperty = DependencyProperty.Register("ImageZoom", typeof(Double), typeof(AdvImage), new PropertyMetadata((Double)1));
        public static DependencyProperty MinImageZoomProperty = DependencyProperty.Register("MinImageZoom", typeof(Double), typeof(AdvImage), new PropertyMetadata((Double)0.8));
        public static DependencyProperty MaxImageZoomProperty = DependencyProperty.Register("MaxImageZoom", typeof(Double), typeof(AdvImage), new PropertyMetadata((Double)2));

        public static DependencyProperty SourceStringProperty = DependencyProperty.Register("SourceString", typeof(String), typeof(AdvImage));
        public static DependencyProperty SourceStreamProperty = DependencyProperty.Register("SourceStream", typeof(System.IO.Stream), typeof(AdvImage));
        public static DependencyProperty ScrollToOptionProperty = DependencyProperty.Register("ScrollToOption", typeof(ScrollTo), typeof(AdvImage));

        public static DependencyProperty HorizontalScrollbarShowingProperty = DependencyProperty.Register("HorizontalScrollbarShowing", typeof(ScrollTo), typeof(AdvImage));
        public static DependencyProperty VerticalScrollbarShowingProperty = DependencyProperty.Register("VerticalScrollbarShowing", typeof(ScrollTo), typeof(AdvImage));

        public ImageSource Source
        {
            get { return ImageStream.Source; }
            set
            {
                ImageStream.Source = value;
                FadeOutImage();
            }
        }
        public String SourceString
        {
            get { return ImageStream.SourceString; }
            set
            {
                ImageStream.SourceString = value;
                FadeOutImage();
            }
        }
        public System.IO.Stream SourceStream
        {
            get { return ImageStream.SourceStream; }
            set
            {
                ImageStream.SourceStream = value;
                FadeOutImage();
            }
        }

        public Double ImageZoom
        {
            get { return (Double)GetValue(ImageZoomProperty); }
            set
            {
                if (value < 0.8) value = 0.8;
                if (value > 2) value = 2;
                SetValue(ImageZoomProperty, value);
                OnPropertyChanged("ImageZoom");
            }
        }
        public Double MinImageZoom
        {
            get { return (Double)GetValue(MinImageZoomProperty); }
            set
            {
                SetValue(MinImageZoomProperty, (value < 1) ? ((value < MaxImageZoom) ? value : MaxImageZoom - 0.1) : 0.9);
                OnPropertyChanged("MinImageZoom");
            }
        }
        public Double MaxImageZoom
        {
            get { return (Double)GetValue(MaxImageZoomProperty); }
            set
            {
                SetValue(MaxImageZoomProperty, (value > 1) ? ((value > MinImageZoom) ? value : MinImageZoom + 0.1) : 1.1);
                OnPropertyChanged("MaxImageZoom");
            }
        }

        public Boolean HorizontalScrollbarShowing
        {
            get { return ImageScroll.ComputedHorizontalScrollBarVisibility == System.Windows.Visibility.Visible; }
        }
        public Boolean VerticalScrollbarShowing
        {
            get { return ImageScroll.ComputedVerticalScrollBarVisibility == System.Windows.Visibility.Visible; }
        }

        private readonly Storyboard FadeImage;
        #endregion

        public AdvImage()
        {
            dStatus = DisplayStatus.Hidden;
            InitializeComponent();
            
            FadeImage = (this.FindResource("FadeImage") as Storyboard);
            FadeImage.Completed += Fade_Completed;
        }
        public void ResetZoom()
        { ImageZoom = 1; }

        #region Scroll
        public enum ScrollTo
        {
            TopLeft = 0,
            TopRight = 1,
            BottomLeft = 2,
            BottomRight = 3
        }

        public ScrollTo ScrollToOption
        {
            get { return (ScrollTo)GetValue(ScrollToOptionProperty); }
            set
            {
                if (dStatus == DisplayStatus.Shown) FadeOutImage();
                SetValue(ScrollToOptionProperty, value);
            }
        }

        public void SetScrollView(Boolean Val)
        {
            ImageScroll.HorizontalScrollBarVisibility = Val ? ScrollBarVisibility.Auto : ScrollBarVisibility.Hidden;
            ImageScroll.VerticalScrollBarVisibility = Val ? ScrollBarVisibility.Auto : ScrollBarVisibility.Hidden;
        }

        private void ScrollToPos()
        {
            switch (ScrollToOption)
            {
                default:
                case ScrollTo.TopLeft:
                    ImageScroll.ScrollToTop();
                    ImageScroll.ScrollToLeftEnd();
                    break;

                case ScrollTo.TopRight:
                    ImageScroll.ScrollToTop();
                    ImageScroll.ScrollToRightEnd();
                    break;

                case ScrollTo.BottomLeft:
                    ImageScroll.ScrollToBottom();
                    ImageScroll.ScrollToLeftEnd();
                    break;

                case ScrollTo.BottomRight:
                    ImageScroll.ScrollToBottom();
                    ImageScroll.ScrollToRightEnd();
                    break;
            }
        }
        #endregion

        #region Fader
        private enum DisplayStatus { Shown, Hidden, Showing, Hiding }
        private DisplayStatus dStatus { get; set; }

        private void FadeInImage()
        {
            dStatus = DisplayStatus.Showing;
            FadeImage.Stop();
            (FadeImage.Children[0] as DoubleAnimation).To = 1; // Opacity
            (FadeImage.Children[1] as DoubleAnimation).To = 0; // Blur
            FadeImage.Begin();
        }
        private void FadeOutImage()
        {
            if (Settings.Default.FadeImages)
            {
                dStatus = DisplayStatus.Hiding;
                FadeImage.Stop();
                (FadeImage.Children[0] as DoubleAnimation).To = 0; // Opacity
                (FadeImage.Children[1] as DoubleAnimation).To = 25; // Blur
                FadeImage.Begin();
            }
            else
            {
                ScrollToPos();
                ImageScroll.Opacity = 1;
                BlurEffect.Radius = 0;
                ImageStream.UpdateImage();
            }
        }

        private void Fade_Completed(object sender, EventArgs e)
        {
            switch (dStatus)
            {
                default: break;

                case DisplayStatus.Showing:
                    dStatus = DisplayStatus.Shown;
                    ImageScroll.Opacity = 1;
                    BlurEffect.Radius = 0;
                    break;

                case DisplayStatus.Hiding:
                    dStatus = DisplayStatus.Hidden;
                    ScrollToPos();
                    ImageScroll.Opacity = 0;
                    BlurEffect.Radius = 25;
                    ImageStream.UpdateImage();
                    FadeInImage();
                    break;
            }
        }
        #endregion
    }
}