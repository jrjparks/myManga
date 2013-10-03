using myMangaSiteExtension.Objects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace myManga_App.ViewModels
{
    public class HomeViewModel : IDisposable, INotifyPropertyChanging, INotifyPropertyChanged
    {
        #region NotifyPropertyChange
        public event PropertyChangingEventHandler PropertyChanging;
        protected void OnPropertyChanging([CallerMemberName] String caller = "")
        {
            if (PropertyChanging != null)
                PropertyChanging(this, new PropertyChangingEventArgs(caller));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] String caller = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(caller));
        }
        #endregion

        private ObservableCollection<MangaObject> mangaList;
        public ObservableCollection<MangaObject> MangaList
        {
            get { return mangaList ?? (mangaList = new ObservableCollection<MangaObject>()); }
            set
            {
                OnPropertyChanging();
                mangaList = value;
                OnPropertyChanged();
            }
        }

        private MangaObject mangaObj;
        public MangaObject MangaObj
        {
            get { return mangaObj; }
            set
            {
                OnPropertyChanging();
                mangaObj = value;
                OnPropertyChanged();
            }
        }

        public HomeViewModel()
        {
#if DEBUG
            MangaList.Add(new MangaObject()
            {
                Name = "One Piece",
                Released = DateTime.Parse("12/24/1997"),
                Authors = { "Oda", "Eiichiro" },
                Artists = { "Oda", "Eiichiro" },
                Genres = { "G1", "G2", "G3" },
                Description = "Seeking to be the greatest pirate in the world, young Monkey D. Luffy, endowed with stretching powers from the legendary \"Gomu Gomu\" Devil's fruit, travels towards the Grand Line in search of One Piece, the greatest treasure in the world.",
                Chapters = { 
                    new ChapterObject(){Name = "Chapter 1", Chapter = 1},
                    new ChapterObject(){Name = "Chapter 2", Chapter = 2}
                }
            });
            if (MangaList.Count > 0)
                MangaObj = MangaList.First();
#endif
        }

        public void Dispose() { }
    }
}
