using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using Manga.Info;
using Manga.Core;
using BakaBox.MVVM;
using myManga.Properties;
using System.Windows;

namespace myManga.Models
{
    public class LibraryItemModel : ModelBase
    {
        #region Fields
        private MangaData _MangaData;

        private ImageSource _Cover;
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
            get { return MangaData.Volume; }
            set
            {
                MangaData.Volume = value;
                OnPropertyChanged("Volume");
                OnPropertyChanged("ChapterStatus");
            }
        }
        public new UInt32 Chapter
        {
            get { return MangaData.Chapter; }
            set
            {
                MangaData.Chapter = value;
                OnPropertyChanged("Chapter");
                OnPropertyChanged("ChapterStatus");
            }
        }
        public new UInt32 SubChapter
        {
            get { return MangaData.SubChapter; }
            set
            {
                MangaData.SubChapter = value;
                OnPropertyChanged("Chapter");
                OnPropertyChanged("ChapterStatus");
            }
        }
        private UInt32 _Page;
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

        private MangaStatus _MangaStatus;
        public MangaStatus MangaStatus
        {
            get { return _MangaStatus; }
            set { _MangaStatus = value; OnPropertyChanged("MangaStatus"); }
        }

        private String _MangaInfoPath;
        public String MangaInfoPath
        {
            get { return _MangaInfoPath; }
            set { _MangaInfoPath = value; OnPropertyChanged("MangaInfoPath"); }
        }

        private ChapterEntry _LastChapter;
        private ChapterEntry LastChapter
        {
            get { return _LastChapter; }
            set { _LastChapter = value; OnPropertyChanged("LastChapter"); }
        }

        private ItemStatus _Status;
        public ItemStatus Status
        {
            get { return _Status; }
            set { _Status = value; OnPropertyChanged("Status"); }
        }

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

        public MangaData MangaData
        {
            get
            {
                if (_MangaData == null)
                    _MangaData = new MangaData();
                return _MangaData;
            }
            set
            {
                _MangaData = value;
                OnPropertyChanged("MangaData");
            }
        }
        
        public Boolean DrawShadow
        { get { return Settings.Default.DrawShadows; } }

        private Guid _SessionMangaID;
        public Guid SessionMangaID
        {
            get
            {
                if (_SessionMangaID == null || _SessionMangaID.Equals(Guid.Empty))
                    _SessionMangaID = Guid.NewGuid();
                return _SessionMangaID;
            }
        }

        private Int32 _Progress;
        public Int32 Progress
        {
            get { return _Progress; }
            set
            {
                _Progress = value;
                ProgressVisibility = Progress.Equals(0) ? Visibility.Collapsed : Visibility.Visible;
                OnPropertyChanged("Progress");
            }
        }
        private Visibility _ProgressVisibility;
        public Visibility ProgressVisibility
        {
            get { return _ProgressVisibility; }
            set
            {
                _ProgressVisibility = value;
                OnPropertyChanged("ProgressVisibility");
            }
        }
        #endregion

        #region Members
        public LibraryItemModel(String _MangaInfoPath)
        {
            CreateShadowLitener();
            MangaInfoPath = _MangaInfoPath;
        }
        public LibraryItemModel(Manga.Core.MangaData _MD, String _MangaInfoPath)
        {
            CreateShadowLitener();
            MangaData = new MangaData(_MD);
            MangaInfoPath = _MangaInfoPath;
        }
        public LibraryItemModel(Manga.Info.MangaInfo _MI, String _MangaInfoPath)
        {
            CreateShadowLitener();
            MangaData = new MangaData(_MI);
            Page = _MI.Page;
            MangaStatus = _MI.Status;
            MangaInfoPath = _MangaInfoPath;
            LastChapter = _MI.ChapterEntries.Last();
        }

        private void CreateShadowLitener()
        {
            Progress = 0;
            Settings.Default.PropertyChanged += Default_PropertyChanged;
        }
        private void Default_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "DrawShadows")
                OnPropertyChanged("DrawShadow");
        }

        public void UpdateMangaInfo(MangaData _MD)
        {
            MangaData.Name = _MD.Name;
            MangaData.Site = _MD.Site;
            Volume = _MD.Volume;
            Chapter = _MD.Chapter;
            SubChapter = _MD.SubChapter;
            MangaData.ID = _MD.ID;
        }
        public void UpdateMangaInfo(Manga.Info.MangaInfo _MI)
        {
            UpdateMangaInfo(_MI as MangaData);

            Page = _MI.Page;
            MangaStatus = _MI.Status;
            LastChapter = _MI.ChapterEntries.Last();
        }
        #endregion
    }
}
