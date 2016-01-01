using Core.Other;
using myManga_App.ViewModels;
using System.Windows.Controls;
using System.Windows.Data;

namespace myManga_App.Views
{
    /// <summary>
    /// Interaction logic for ReaderView.xaml
    /// </summary>
    public partial class ReaderView : UserControl
    {
        public ReaderView()
        { InitializeComponent(); }

        private void ImageContent_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            this.ImageContentScrollViewer.ScrollToHome();
            this.PageList.ScrollToCenterOfView(this.PageList.SelectedItem);
            if (this.DataContext is ReaderViewModel)
                if ((this.DataContext as ReaderViewModel).MangaObject.MangaType == myMangaSiteExtension.Enums.MangaObjectType.Manga)
                    this.ImageContentScrollViewer.ScrollToRightEnd();
        }

        private void PageList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        { this.PageList.ScrollToCenterOfView(this.PageList.SelectedItem); }
    }
}
