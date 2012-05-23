using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using Manga.Info;
using Manga.Core;
using BakaBox.MVVM;
using myManga.Properties;

namespace myManga.Models
{
    public class LibraryItemModel : MangaData
    {
        #region Private
        private ImageSource _Cover { get; set; }
        private UInt32 _Page { get; set; }
        private MangaStatus _mStatus { get; set; }
        private String _MangaInfoPath { get; set; }
        private ChapterEntry _LastChapter { get; set; }

        private ItemStatus _Status { get; set; }
        #endregion

        #region Public
        [Flags]
        public enum ItemStatus
        {
            Idle = 0x01,
            Working = 0x02,
            Deleting = 0x04,
            Updating = 0x08,
            Downloading = 0x16,
            Updated = 0x32
        }

        public LibraryItemModel(String _MangaInfoPath) { MangaInfoPath = _MangaInfoPath; }
        public LibraryItemModel(Manga.Core.MangaData _MD, String _MangaInfoPath) : base(_MD) { MangaInfoPath = _MangaInfoPath; }
        public LibraryItemModel(Manga.Info.MangaInfo _MI, String _MangaInfoPath) : base(_MI) { Page = _MI.Page; mStatus = _MI.Status; MangaInfoPath = _MangaInfoPath; LastChapter = _MI.ChapterEntries.Last(); }

        public Boolean DrawShadow
        { get { return Settings.Default.DrawShadows; } }

        public ImageSource Cover
        {
            get { return _Cover; }
            set
            {
                _Cover = value;
                OnPropertyChanged("Cover");
            }
        }

        public Boolean ChapterStatus
        {
            get
            {
                if (LastChapter is ChapterEntry)
                {
                    Boolean _Vol = (LastChapter.Volume > Volume),
                        _Chap = (LastChapter.Chapter > Chapter),
                        _SubChap = (LastChapter.SubChapter > SubChapter),
                        _NewChap = _Vol || _Chap || _SubChap;
                    return _NewChap;
                }
                return false;
            }
        }

        public new UInt32 Volume
        {
            get { return _Volume; }
            set
            {
                _Volume = value;
                OnPropertyChanged("Volume");
                OnPropertyChanged("ChapterStatus");
            }
        }

        public new UInt32 Chapter
        {
            get { return _Chapter; }
            set
            {
                _Chapter = value;
                OnPropertyChanged("Chapter");
                OnPropertyChanged("ChapterStatus");
            }
        }

        public new UInt32 SubChapter
        {
            get { return _SubChapter; }
            set
            {
                _SubChapter = value;
                OnPropertyChanged("Chapter");
                OnPropertyChanged("ChapterStatus");
            }
        }

        public UInt32 Page
        {
            get { return _Page; }
            set
            {
                _Page = value;
                OnPropertyChanged("Page");
                OnPropertyChanged("ChapterStatus");
            }
        }

        public MangaStatus mStatus
        {
            get { return _mStatus; }
            set { _mStatus = value; OnPropertyChanged("mStatus"); }
        }

        public String MangaInfoPath
        {
            get { return _MangaInfoPath; }
            set { _MangaInfoPath = value; OnPropertyChanged("MangaInfoPath"); }
        }

        private ChapterEntry LastChapter
        {
            get { return _LastChapter; }
            set { _LastChapter = value; OnPropertyChanged("LastChapter"); }
        }

        public ItemStatus Status
        {
            get { return _Status; }
            set { _Status = value; OnPropertyChanged("Status"); }
        }

        public void UpdateMangaInfo(MangaData _MD)
        {
            Name = _MD.Name;
            Site = _MD.Site;
            Volume = _MD.Volume;
            Chapter = _MD.Chapter;
            SubChapter = _MD.SubChapter;
            ID = _MD.ID;
        }
        public void UpdateMangaInfo(Manga.Info.MangaInfo _MI)
        {
            UpdateMangaInfo(_MI as MangaData);
            
            Page = _MI.Page;
            mStatus = _MI.Status;
            LastChapter = _MI.ChapterEntries.Last();
        }
        #endregion
    }
}
