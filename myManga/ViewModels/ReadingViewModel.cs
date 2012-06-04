using System;
using System.IO;
using System.Windows.Input;
using BakaBox.MVVM;
using Manga.Archive;
using Manga;
using Manga.Core;
using Manga.Info;
using Manga.Zip;
using myManga.UI;
using Manga.Manager;
using BakaBox.Controls.Threading;
using System.Collections.Generic;
using System.Windows;
using myManga.Properties;

namespace myManga.ViewModels
{
    public sealed class ReadingViewModel : ViewModelBase
    {
        #region Variables
        private MangaInfo _MangaInfo { get; set; }
        public MangaInfo Info
        {
            get { return _MangaInfo; }
            set { _MangaInfo = value; OnPropertyChanged("Info"); OnPropertyChanged("IsEnabled"); }
        }
        private MangaArchiveInfo _MangaArchiveInfo { get; set; }
        public MangaArchiveInfo ArchiveInfo
        {
            get { return _MangaArchiveInfo; }
            set { _MangaArchiveInfo = value; OnPropertyChanged("ArchiveInfo"); OnPropertyChanged("ChapterName"); }
        }

        private Dictionary<Guid, OpenPage> _DownloadingTasks;
        private Dictionary<Guid, OpenPage> DownloadingTasks
        {
            get
            {
                if (_DownloadingTasks == null)
                    _DownloadingTasks = new Dictionary<Guid, OpenPage>();
                return _DownloadingTasks;
            }
        }

        private Dictionary<String, Guid> _PreDownloadChapters;
        private Dictionary<String, Guid> PreDownloadChapters
        {
            get
            {
                if (_PreDownloadChapters == null)
                    _PreDownloadChapters = new Dictionary<String, Guid>();
                return _PreDownloadChapters;
            }
        }

        private String _MZALocation { get; set; }
        public String MZALocation
        {
            get { return _MZALocation; }
            set
            {
                _MZALocation = value;
                OnPropertyChanged("MZALocation");
            }
        }
        private String _MIZALocation { get; set; }
        public String MIZALocation
        {
            get { return _MIZALocation; }
            set
            {
                _MIZALocation = value;
                OnPropertyChanged("MIZALocation");
            }
        }

        public String ChapterName
        {
            get
            {
                if (ArchiveInfo != null)
                {
                    if (Info == null)
                        return ArchiveInfo.MangaDataName(false);
                    else
                    {
                        String ChapterName = Info.ChapterEntries.GetChapterByNumber(ArchiveInfo.Volume, ArchiveInfo.Chapter, ArchiveInfo.SubChapter).Name;
                        if (ChapterName.Equals(String.Empty))
                            return ArchiveInfo.MangaDataName(false);
                        else
                            return String.Format("{0} - {1}", ArchiveInfo.MangaDataName(false), ChapterName);
                    }
                }
                return String.Empty;
            }
        }

        public Double ImageZoom
        {
            get { return PageView.ImageZoom; }
            set { OnPropertyChanging("ImageZoom"); PageView.ImageZoom = value; OnPropertyChanged("ImageZoom"); }
        }

        private Double _Progress;
        public Double Progress
        {
            get { return _Progress; }
            private set
            {
                ProgressVisibility = (value == 0) ? Visibility.Collapsed : Visibility.Visible;
                OnPropertyChanging("Progress");
                _Progress = value;
                OnPropertyChanged("Progress");
            }
        }
        private Visibility _ProgressVisibility;
        public Visibility ProgressVisibility
        {
            get { return _ProgressVisibility; }
            set
            {
                OnPropertyChanging("ProgressVisibility");
                _ProgressVisibility = value;
                OnPropertyChanged("ProgressVisibility");
            }
        }

        public enum PageDirection { Prev, Next }
        public enum OpenPage { First, Last,  Resume}

        public Boolean IsEnabled
        { get { return Info != null; } }

        #endregion

        #region Commands
        private DelegateCommand _NextPage { get; set; }
        public ICommand NextPage
        {
            get
            {
                if (_NextPage == null)
                    _NextPage = new DelegateCommand(LoadNextPage, CanNextPage);
                return _NextPage;
            }
        }
        private Boolean CanNextPage()
        {
            if (ArchiveInfo == null) return false;
            return ChapterIndex < ArchiveInfo.PageEntries.Count;
        }

        private DelegateCommand _PrevPage { get; set; }
        public ICommand PrevPage
        {
            get
            {
                if (_PrevPage == null)
                    _PrevPage = new DelegateCommand(LoadPrevPage, CanPrevPage);
                return _PrevPage;
            }
        }
        private Boolean CanPrevPage()
        {
            if (ArchiveInfo == null) return false;
            return ChapterIndex >= 0;
        }

        private DelegateCommand _SwipeLeft { get; set; }
        public ICommand SwipeLeft
        {
            get
            {
                if (_SwipeLeft == null)
                    _SwipeLeft = new DelegateCommand(SwipeLeftSwitch, CanSwipeLeft);
                return _SwipeLeft;
            }
        }
        private void SwipeLeftSwitch()
        {
            if (Info == null) return;
            switch (Info.ReadDirection)
            {
                default:
                case ReadDirection.FromRight:
                    LoadPrevPage();
                    break;

                case ReadDirection.FromLeft:
                    LoadNextPage();
                    break;
            }
        }
        private Boolean CanSwipeLeft()
        {
            if (Info == null) return false;
            return Info.ReadDirection.Equals(ReadDirection.FromRight) ? CanPrevPage() : CanNextPage();
        }

        private DelegateCommand _SwipeRight { get; set; }
        public ICommand SwipeRight
        {
            get
            {
                if (_SwipeRight == null)
                    _SwipeRight = new DelegateCommand(SwipeRightSwitch, CanSwipeRight);
                return _PrevPage;
            }
        }
        private void SwipeRightSwitch()
        {
            if (Info == null) return;
            switch (Info.ReadDirection)
            {
                default:
                case ReadDirection.FromRight:
                    LoadNextPage();
                    break;

                case ReadDirection.FromLeft:
                    LoadPrevPage();
                    break;
            }
        }
        private Boolean CanSwipeRight()
        {
            if (Info == null) return false;
            return Info.ReadDirection.Equals(ReadDirection.FromRight) ? CanNextPage() : CanPrevPage();
        }
        #endregion

        #region Pages
        private UInt32 _ChapterIndex { get; set; }
        public UInt32 ChapterIndex
        {
            get { return (Info != null) ? (UInt32)ArchiveInfo.PageEntries.IndexOfPage(Info.Page) : 0; }
            set
            {
                if (Info != null)
                {
                    Info.Page = ArchiveInfo.PageEntries[(Int32)value].PageNumber;
                    OnPropertyChanged("ChapterIndex");
                    LoadPageImage();
                }
            }
        }

        public AdvImage PageView { get; set; }
        #endregion

        #region Methods
        #region Public
        public ReadingViewModel()
        {
            PageView = new UI.AdvImage();
            PageView.ScrollToOption = UI.AdvImage.ScrollTo.TopRight;

            Progress = 0;

            Manager_v1.Instance.TaskProgress += (s, t) => TaskUpdated(s, t);
            Manager_v1.Instance.TaskComplete += (s, t) => TaskUpdated(s, t);
            Manager_v1.Instance.TaskRemoved += (s, t) => TaskUpdated(s, t);
            Manager_v1.Instance.QueueComplete += (s) => { Progress = 0; };
        }

        public Boolean OpenMZA(String MZA_Path)
        { return OpenMZA(MZA_Path, OpenPage.Resume); }
        public Boolean OpenMZA(String MZA_Path, OpenPage OpenPage)
        {
            Progress = 0;
            ArchiveInfo = MangaDataZip.Instance.MangaArchiveInfo(MZALocation = MZA_Path);

            MIZALocation = MangaDataZip.Instance.CreateMIZAPath(new MangaInfo(ArchiveInfo as MangaData));

            if (File.Exists(MIZALocation))
                Info = MangaDataZip.Instance.MangaInfo(MIZALocation);
            else
                Info = new MangaInfo(ArchiveInfo as MangaData);
            
            Info.Volume = ArchiveInfo.Volume;
            Info.Chapter = ArchiveInfo.Chapter;
            Info.SubChapter = ArchiveInfo.SubChapter;

            PageView.ScrollToOption =
                (Info.ReadDirection == ReadDirection.FromRight) ?
                AdvImage.ScrollTo.TopRight : AdvImage.ScrollTo.TopLeft;
            ImageZoom = 1;

            switch (OpenPage)
            {
                default:
                case OpenPage.Resume:
                    ChapterIndex = (UInt32)ArchiveInfo.PageEntries.IndexOfPage(Info.Page == 0 ? 1 : Info.Page);
                    break;

                case OpenPage.First:
                    ChapterIndex = 0;
                    break;

                case OpenPage.Last:
                    ChapterIndex = (UInt32)(ArchiveInfo.PageEntries.Count - 1);
                    break;
            }
            if (Settings.Default.AutoClean && Info != null)
                CleanPrevChapter();

            SendViewModelToastNotification(this, String.Format("Opened {0}.", ArchiveInfo.MangaDataName()));

            return true;
        }
        #endregion

        #region Private
        private void TaskUpdated(Object Sender, QueuedTask<ManagerData<String, MangaInfo>> Task)
        {
            if (DownloadingTasks.ContainsKey(Task.Guid) || PreDownloadChapters.ContainsKey(Task.Data.Title))
            {
                switch (Task.TaskStatus)
                {
                    default: break;

                    case System.Threading.Tasks.TaskStatus.RanToCompletion:
                        if (DownloadingTasks.ContainsKey(Task.Guid))
                        {
                            OpenMZA(Task.Data.Data, DownloadingTasks[Task.Guid]);
                            DownloadingTasks.Remove(Task.Guid);
                        }
                        if (PreDownloadChapters.ContainsKey(Task.Data.Title))
                        {
                            Progress = 0;
                            PreDownloadChapters.Remove(Task.Data.Title);
                        }
                        Progress = 0;
                        break;

                    case System.Threading.Tasks.TaskStatus.Faulted:
                        DownloadingTasks.Remove(Task.Guid);
                        Progress = 0;
                        SendViewModelToastNotification(this, String.Format("Error downloading chapter.\n{0}", Sender.ToString()), ToastNotification.DisplayLength.Long);
                        break;

                    case System.Threading.Tasks.TaskStatus.Running:
                        Progress = Task.Progress;
                        break;
                }
            }
        }

        private void LoadNextPage()
        {
            if (ArchiveInfo.PageEntries.Count > 0)
            {
                if (ChapterIndex + 1 < ArchiveInfo.PageEntries.Count)
                    ++ChapterIndex;
                else
                    ChangeChapter(PageDirection.Next);
            }
        }
        private void LoadPrevPage()
        {
            if (ArchiveInfo.PageEntries.Count > 0)
            {
                if ((Int32)ChapterIndex - 1 >= 0)
                    --ChapterIndex;
                else
                    ChangeChapter(PageDirection.Prev);
            }
        }
        private void ChangeChapter(PageDirection PageDirection)
        {
            if (Info != null && Info.InfoPage != String.Empty)
            {
                Boolean ChangeChapter = false;
                ChapterEntry ArchiveChapter = Info.ChapterEntries[ArchiveInfo.Volume, ArchiveInfo.Chapter, ArchiveInfo.SubChapter];
                Int32 IndexOfChapter = Info.ChapterEntries[ArchiveChapter];
                switch (PageDirection)
                {
                    default: break;
                    case ReadingViewModel.PageDirection.Next:
                        if (IndexOfChapter + 1 < Info.ChapterEntries.Count)
                        {
                            ArchiveChapter = Info.ChapterEntries[IndexOfChapter + 1];
                            ChangeChapter = true;
                        }
                        break;

                    case ReadingViewModel.PageDirection.Prev:
                        if (IndexOfChapter - 1 >= 0)
                        {
                            ArchiveChapter = Info.ChapterEntries[IndexOfChapter - 1];
                            ChangeChapter = true;
                        }
                        break;
                }
                if (ChangeChapter)
                {
                    String ChapterFileName = ArchiveChapter.ChapterName(Info),
                        ChapterFilePath = Path.Combine(Path.GetDirectoryName(MZALocation), ChapterFileName);

                    if (File.Exists(ChapterFilePath))
                        OpenMZA(ChapterFilePath, PageDirection.Equals(PageDirection.Next) ? OpenPage.First : OpenPage.Last);
                    else if(!PreDownloadChapters.ContainsKey(ArchiveChapter.ChapterName(Info)))
                    {
                        SendViewModelToastNotification(this, String.Format("Downloading Chapter.\n{0}", ChapterFileName));
                        DownloadingTasks.Add(Manager_v1.Instance.DownloadChapter(ArchiveChapter.UrlLink), PageDirection.Equals(PageDirection.Next) ? OpenPage.First : OpenPage.Last);
                    }
                }
                else
                    SendViewModelToastNotification(this, (PageDirection.Equals(ReadingViewModel.PageDirection.Next)) ? "The author and artist are tired.\nBe patient." : "That chapter will NEVER exist.\n!NEVER!", ToastNotification.DisplayLength.Long);
            }
            else
                SendViewModelToastNotification(this, "Cannot change chapter for unknown manga.\nPlease open your manga from the Library.", UI.ToastNotification.DisplayLength.Normal);
        }

        private void LoadPageImage()
        {
            PageView.SourceStream = MangaDataZip.Instance.PageStream(Info.Page, ArchiveInfo, MZALocation);
            if (Info != null && Info.InfoPage != String.Empty)
                MangaDataZip.Instance.MIZA(Info);
            if (Settings.Default.AutoDownload && Info != null)
                DownloadNextChapter();
        }

        private void DownloadNextChapter()
        {
            ChapterEntry CurrentArchiveChapter = Info.ChapterEntries[ArchiveInfo.Volume, ArchiveInfo.Chapter, ArchiveInfo.SubChapter];
            Int32 Index = Info.ChapterEntries[CurrentArchiveChapter];
            if (Index < Info.ChapterEntries.Count - 1)
            {
                CurrentArchiveChapter = Info.ChapterEntries[++Index];
                String ChapterFileName = CurrentArchiveChapter.ChapterName(Info),
                    ChapterFilePath = Path.Combine(Path.GetDirectoryName(MZALocation), ChapterFileName);
                if (!File.Exists(ChapterFilePath) &&
                    !PreDownloadChapters.ContainsKey(ChapterFileName))
                {
                    SendViewModelToastNotification(this, String.Format("Downloading Chapter.\n{0}", ChapterFileName));
                    PreDownloadChapters.Add(ChapterFileName, Manager_v1.Instance.DownloadChapter(CurrentArchiveChapter.UrlLink));
                }
            }
        }
        private void CleanPrevChapter()
        {
            ChapterEntry CurrentArchiveChapter = Info.ChapterEntries[ArchiveInfo.Volume, ArchiveInfo.Chapter, ArchiveInfo.SubChapter];

            Int32 Index = Info.ChapterEntries[CurrentArchiveChapter];
            if (--Index > 0)
            {
                for (--Index; Index >= 0; --Index)
                {
                    CurrentArchiveChapter = Info.ChapterEntries[Index];
                    String ChapterFileName = CurrentArchiveChapter.ChapterName(Info),
                        ChapterFilePath = Path.Combine(Path.GetDirectoryName(MZALocation), ChapterFileName);
                    if (File.Exists(ChapterFilePath))
                    {
                        SendViewModelToastNotification(this, String.Format("Deleteing...\n{0}", ChapterFileName), ToastNotification.DisplayLength.Long);
                        File.Delete(ChapterFilePath);
                    }
                }
            }
        }
        #endregion
        #endregion
    }
}
