using myManga_App.ViewModels.Pages;
using System.Windows.Controls;
using System.Windows.Data;

namespace myManga_App.Views.Pages
{
    /// <summary>
    /// Interaction logic for ChapterReaderView.xaml
    /// </summary>
    public partial class ChapterReaderView : UserControl
    {
        public ChapterReaderView()
        {
            InitializeComponent();
        }

        private void PageImageContent_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            PageImageContentScrollViewer.ScrollToHome();
            ChapterOverviewList.ScrollToCenterOfView(ChapterOverviewList.SelectedItem);
            if (DataContext is ChapterReaderViewModel)
                if (Equals((DataContext as ChapterReaderViewModel).MangaObject.MangaType, myMangaSiteExtension.Enums.MangaObjectType.Manga))
                    PageImageContentScrollViewer.ScrollToRightEnd();
        }

        private void ChapterOverviewList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        { ChapterOverviewList.ScrollToCenterOfView(ChapterOverviewList.SelectedItem); }
    }
}
