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
using Manga.Zip;
using System.IO;

namespace myManga.Models
{
    public sealed class LibraryItemModel : MangaInfo
    {
        #region Fields
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
                        _Page = (TotalPage > Page),
                        _NewChap = _Vol || _Chap || _SubChap || _Page;
                    return _NewChap;
                }
                return false;
            }
        }
        public new UInt32 Volume
        {
            get { return base.Volume; }
            set
            {
                base.Volume = value;
                OnPropertyChanged("Volume");
                OnPropertyChanged("ChapterStatus");
            }
        }
        public new UInt32 Chapter
        {
            get { return base.Chapter; }
            set
            {
                base.Chapter = value;
                OnPropertyChanged("Chapter");
                OnPropertyChanged("ChapterStatus");
            }
        }
        public new UInt32 SubChapter
        {
            get { return base.SubChapter; }
            set
            {
                base.SubChapter = value;
                OnPropertyChanged("Chapter");
                OnPropertyChanged("ChapterStatus");
            }
        }
        public UInt32 Page
        {
            get { return base.Page; }
            set
            {
                base.Page = value;
                OnPropertyChanged("Page");
                OnPropertyChanged("ChapterStatus");
            }
        }
        private UInt32 _TotalPage;
        public UInt32 TotalPage
        {
            get { return _TotalPage; }
            set
            {
                _TotalPage = value;
                OnPropertyChanged("TotalPage");
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

        private ChapterEntry LastChapter
        {
            get { return (ChapterEntries != null) ? ChapterEntries.Last() : null; }
        }

        private ItemStatus _ItemWorkStatus;
        public ItemStatus ItemWorkStatus
        {
            get { return _ItemWorkStatus; }
            set { _ItemWorkStatus = value; OnPropertyChanged("ItemWorkStatus"); }
        }

        public new Boolean Licensed
        {
            get { return _Licensed; }
            set
            {
                _Licensed = value;
                OnPropertyChanged("Licensed");
            }
        }

        public new Boolean KeepChapters
        {
            get { return _KeepChapters; }
            set
            {
                _KeepChapters = value;
                OnPropertyChanged("KeepChapters");
            }
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
            base.Init();
            CreateShadowLitener();
            MangaInfoPath = _MangaInfoPath;
        }
        public LibraryItemModel(MangaInfo MangaInfo, String _MangaInfoPath)
        {
            CreateShadowLitener();
            base.Init(MangaInfo);
            UpdateMangaInfo(MangaInfo);
            MangaInfoPath = _MangaInfoPath;
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

        public void UpdateMangaInfo(MangaData MangaData)
        {
            Name = MangaData.Name;
            Site = MangaData.Site;
            Volume = MangaData.Volume;
            Chapter = MangaData.Chapter;
            Page = MangaData.Page;
            SubChapter = MangaData.SubChapter;
            ID = MangaData.ID;
        }
        public void UpdateMangaInfo(MangaInfo MangaInfo)
        {
            UpdateMangaInfo(MangaInfo as MangaData);
            ChapterEntries = MangaInfo.ChapterEntries;
            TotalPage = MangaInfo.TotalPage;
            MangaStatus = MangaInfo.Status;
            Licensed = MangaInfo.Licensed;
            KeepChapters = MangaInfo.KeepChapters;
        }
        #endregion
    }
}
