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
using Core.Other;
using myManga_App.ViewModels;

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
