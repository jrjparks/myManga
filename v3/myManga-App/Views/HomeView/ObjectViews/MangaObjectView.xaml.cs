using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace myManga_App.Views.ObjectViews
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
