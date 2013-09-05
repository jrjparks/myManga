using myMangaSiteExtension.Objects;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace myManga_App.ViewModels
{
    public class HomeViewModel : DependencyObject, IDisposable
    {
        public static readonly DependencyProperty MangaObjProperty = DependencyProperty.Register("MangaObj", typeof(MangaObject), typeof(HomeViewModel));
        public MangaObject mangaObj;
        public MangaObject MangaObj
        {
            get { return mangaObj ?? (mangaObj = new MangaObject()); }
            set { mangaObj = value; }
        }

        public HomeViewModel() { }

        public void Dispose() { }
    }
}
