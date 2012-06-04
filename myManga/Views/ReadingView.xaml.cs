using System.Windows.Controls;

namespace myManga.Views
{
    /// <summary>
    /// Interaction logic for ReadingView.xaml
    /// </summary>
    public partial class ReadingView : UserControl
    {
        public ReadingView()
        {
            InitializeComponent();
        }

        private void Slider_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            switch (e.ChangedButton)
            {
                default: break;

                case System.Windows.Input.MouseButton.Right:
                    if (this.DataContext is myManga.ViewModels.ReadingViewModel)
                        (this.DataContext as myManga.ViewModels.ReadingViewModel).ImageZoom = 1;
                    break;
            }
        }
    }
}
