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
            // Test Data
            MangaList.Add(new MangaObject()
            {
                Name = "One Piece",
                MangaType = myMangaSiteExtension.Enums.MangaObjectType.Manga,
                Released = DateTime.Parse("12/24/1997"),
                Authors = { "Oda", "Eiichiro" },
                Artists = { "Oda", "Eiichiro" },
                Genres = { "G1", "G2", "G3" },
                Covers = { "http://www.mangaupdates.com/image/i163766.jpg", "http://s3.mangareader.net/cover/one-piece/one-piece-l1.jpg" },
                Description = "As a child, Monkey D. Luffy dreamed of becoming the King of the Pirates. But his life changed when he accidentally gained the power to stretch like rubber...at the cost of never being able to swim again! Now Luffy, with the help of a motley collection of nakama, is setting off in search of the \"One Piece,\" said to be the greatest treasure in the world...\n\nCurrently ranked as the best-selling series in manga history.",
                Chapters = { }
            });
            for (int c = 1; c <= 723; ++c)
                MangaList.Last().Chapters.Add(new ChapterObject() { Chapter = c });

            MangaList.Add(new MangaObject()
            {
                Name = "Beelzebub",
                MangaType = myMangaSiteExtension.Enums.MangaObjectType.Manga,
                Released = DateTime.Parse("1/1/2009"),
                Authors = { "Tamura Ryuuhei" },
                Artists = { "Tamura Ryuuhei" },
                Genres = { "G1", "G2", "G3" },
                PreferredCover = 0,
                Covers = { "http://www.mangaupdates.com/image/i165599.jpg", "http://s5.mangareader.net/cover/beelzebub/beelzebub-l0.jpg", "http://m.mhcdn.net/store/manga/5502/cover.jpg?v=1380796982" },
                Description = "The story follows the \"strongest juvenile delinquent\" as he watches over the demon king's son (AKA the future demon king) with the destruction of the world hanging in the balance.",
                Chapters = { }
            });
            for (int c = 1; c <= 223; ++c)
                MangaList.Last().Chapters.Add(new ChapterObject() { Chapter = c });

            MangaList.Add(new MangaObject()
            {
                Name = "Tower of God",
                MangaType = myMangaSiteExtension.Enums.MangaObjectType.Manhwa,
                Released = DateTime.Parse("1/1/2010"),
                Authors = { "SIU" },
                Artists = { "SIU" },
                Genres = { "G1", "G2", "G3" },
                PreferredCover = 0,
                Covers = { "http://www.mangaupdates.com/image/i155145.png" },
                Description = "What do you desire? Fortune? Glory? Power? Revenge? Or something that surpasses all others? Whatever you desire, 'that is here'. Tower of God.",
                Chapters = { }
            });
            for (int c = 1; c <= 81; ++c)
                MangaList.Last().Chapters.Add(new ChapterObject() { Chapter = c });

            if (MangaList.Count > 0)
                MangaObj = MangaList.First();
#endif
        }

        public void Dispose() { }
    }
}
