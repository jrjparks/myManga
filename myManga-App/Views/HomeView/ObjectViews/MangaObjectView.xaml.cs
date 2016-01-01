using System.Windows.Controls;
using System.Windows.Data;

namespace myManga_App.Views.HomeView.ObjectViews
{
    /// <summary>
    /// Interaction logic for MangaObjectView.xaml
    /// </summary>
    public partial class MangaObjectView : UserControl
    {
        public MangaObjectView()
        {
            InitializeComponent();
            this.ChapterListBox.SourceUpdated += ChapterListBox_SourceUpdated;
        }

        private void ChapterListBox_SourceUpdated(object sender, DataTransferEventArgs e)
        { if (sender is ListBox) (sender as ListBox).ScrollIntoView((sender as ListBox).SelectedItem); }
    }
}
