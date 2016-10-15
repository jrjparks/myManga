using myMangaSiteExtension.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace myManga_App.ViewModels.Objects
{
    public class ChapterPageViewModel
    {
        public BitmapImage Image { get; private set; }
        public PageObject PageInfo { get; private set; }

        public ChapterPageViewModel(BitmapImage img, PageObject info)
        {
            Image = img;
            PageInfo = info;
        }
    }
}
