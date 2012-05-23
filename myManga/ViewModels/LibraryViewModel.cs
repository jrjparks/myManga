using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows.Input;
using BakaBox.Controls;
using BakaBox.Controls.Threading;
using BakaBox.MVVM;
using Manga;
using Manga.Info;
using Manga.Manager;
using Manga.Zip;
using myManga.Models;
using Manga.Core;
using System.Windows.Threading;
using System.Windows;
using System.Threading;
using Manga.Archive;
using myManga.UI;

namespace myManga.ViewModels
{
    public sealed class LibraryViewModel : ViewModelBase
    {
        #region Events
        public delegate void OpenChapterFileEvent(String File, Boolean Resume);
        public event OpenChapterFileEvent OpenChapter;
        private void OnOpenChapter(String File)
        { OnOpenChapter(File, false); }
        private void OnResumeChapter(String File)
        { OnOpenChapter(File, true); }
        private void OnOpenChapter(String File, Boolean Resume)
        {
            if (OpenChapter != null)
                OpenChapter(File, Resume);
        }

        public delegate void ItemsLoaded(LibraryItemModel[] MangaInfo);
        public event ItemsLoaded LibraryItemsLoaded;
        private void OnLibraryItemsLoaded(params LibraryItemModel[] MangaInfo)
        {
            if (LibraryItemsLoaded != null)
                LibraryItemsLoaded(MangaInfo);
        }

        public delegate void EmptyEvent();
        public event EmptyEvent InitComplete;
        private void OnInitComplete()
        {
            if (InitComplete != null)
                InitComplete();
        }
        #endregion

        #region Variables
        private Boolean HasInit;

        private Boolean _DetailsOpen { get; set; }
        public Boolean DetailsOpen
        {
            get { return _DetailsOpen; }
            set
            {
                _DetailsOpen = value;
                OnPropertyChanged("DetailsOpen");
                if ((CurrentMangaItem is LibraryItemModel) &&
                    (!(CurrentInfo is MangaInfo) || !CurrentInfo.Name.Equals(CurrentMangaItem.Name)))
                    CurrentInfo = MangaDataZip.Instance.MangaInfo(CurrentMangaItem.MangaInfoPath);
                else
                    CurrentInfo = null;
            }
        }

        private QueuedBackgroundWorker<String> _LibraryItemLoader;
        private QueuedBackgroundWorker<String> LibraryItemLoader
        {
            get
            {
                if (_LibraryItemLoader == null)
                    _LibraryItemLoader = new QueuedBackgroundWorker<String>();
                return _LibraryItemLoader;
            }
        }
        private Queue<String> _NewChapters;
        private Queue<String> NewChapters
        {
            get
            {
                if (_NewChapters == null)
                    _NewChapters = new Queue<String>();
                return _NewChapters;
            }
        }

        #region List
        private SortableObservableCollection<LibraryItemModel> _MangaItems;
        public SortableObservableCollection<LibraryItemModel> MangaItems
        {
            get
            {
                if (_MangaItems == null)
                    _MangaItems = new SortableObservableCollection<LibraryItemModel>();
                return _MangaItems;
            }
        }

        private LibraryItemModel _CurrentMangaItem { get; set; }
        public LibraryItemModel CurrentMangaItem
        {
            get { return _CurrentMangaItem; }
            set
            {
                _CurrentMangaItem = value;
                if (value != null && DetailsOpen)
                    CurrentInfo = MangaDataZip.Instance.MangaInfo(value.MangaInfoPath);
                OnPropertyChanged("CurrentMangaItem");
            }
        }
        private MangaInfo _CurrentInfo { get; set; }
        public MangaInfo CurrentInfo
        {
            get { return _CurrentInfo; }
            set { _CurrentInfo = value; OnPropertyChanged("CurrentInfo"); }
        }
        #endregion
        #endregion

        #region Constructor
        public LibraryViewModel()
        {
            HasInit = false;
            MangaItems.Clear();

            LibraryItemLoader.DoWork += _LibraryItemLoader_DoWork;
            LibraryItemLoader.RunWorkerCompleted += _LibraryItemLoader_RunWorkerCompleted;
            LibraryItemLoader.QueueComplete += LibraryItemLoader_QueueComplete;

            MangaDataZip.Instance.MangaInfoUpdated += (s, m, p) => LoadLibraryData(p);

            LoadLibraryDirectory(IO.SafeFolder(MangaDataZip.Instance.MIZAPath));
        }
        #endregion

        #region Methods
        #region Public
        private delegate void LibraryItemLoaderInvoke(params String[] DataPath_s);
        public void LoadLibraryData(params String[] DataPath_s)
        {
            if (Application.Current.Dispatcher.Thread == Thread.CurrentThread)
                foreach (String DataFile in DataPath_s)
                    LibraryItemLoader.AddToQueue(DataFile);
            else
            {
                Object[] args = { DataPath_s };
                Application.Current.Dispatcher.BeginInvoke(new LibraryItemLoaderInvoke(LoadLibraryData), args);
            }
        }
        public void LoadLibraryDirectory(String FolderPath)
        { LoadLibraryData(Directory.GetFiles(FolderPath, "*.miza", SearchOption.TopDirectoryOnly)); }

        public void UpdateLibraryMangaInfo(Object Sender, MangaInfo MangaInfo, String FullFilePath)
        {
            Int32 Index = IndexOfMangaItemsName(MangaInfo.Name);
            if (Index >= 0)
                MangaItems[Index].UpdateMangaInfo(MangaInfo);
            else
                LoadLibraryData(FullFilePath);
        }

        public void UpdateLibraryManga()
        {
            List<MangaInfo> _Infos = new List<MangaInfo>(MangaItems.Count);
            foreach (LibraryItemModel _Item in MangaItems)
                if (_Item.mStatus == MangaStatus.Ongoing)
                    _Infos.Add(MangaDataZip.Instance.MangaInfo(_Item.MangaInfoPath));
            SendViewModelToastNotification(this, String.Format("Updating {0} Mangas...", _Infos.Count), UI.ToastNotification.DisplayLength.Normal);

            foreach (MangaInfo i in _Infos)
            {
                if (File.Exists(Path.Combine(Environment.CurrentDirectory, "MangaInfo", i.MangaDataName())))
                    Manager_v1.Instance.DownloadManga(i);
                else
                {
                    SendViewModelToastNotification(this, String.Format("Downloading...\n{0}", i.Name), ToastNotification.DisplayLength.Short);
                    Manager_v1.Instance.DownloadManga(i.InfoPage);
                }
            }
        }

        public void OpenDetails()
        { DetailsOpen = true; }
        public void CloseDetails()
        { DetailsOpen = false; }
        #endregion

        #region Private
        private Int32 IndexOfMangaItemsName(String Name)
        {
            Boolean _Found = false;
            Int32 _Index = 0;
            foreach (LibraryItemModel _LibItem in MangaItems)
            {
                if (_LibItem.Name.Equals(Name))
                {
                    _Found = true;
                    break;
                }
                else ++_Index;
            }
            return _Found ? _Index : -1;
        }

        private void RequestOpenChapter(ChapterEntry ChapterEntry)
        { RequestOpenChapter(ChapterEntry, CurrentInfo, false); }
        private void RequestResumeChapter(MangaInfo Info)
        { RequestOpenChapter(Info.LastReadChapterEntry, Info, true); }
        private void RequestOpenChapter(ChapterEntry ChapterEntry, MangaInfo Info, Boolean Resume)
        {
            String _FileName = ChapterEntry.ChapterName(Info),
                        MainPath = MangaDataZip.Instance.MZAPath,
                        TmpPath = Manga.Archive.MangaArchiveData.TmpFolder;

            List<String> _PosibleFiles = new List<String>();
            if (Directory.Exists(MainPath))
                _PosibleFiles.AddRange(Directory.GetFiles(MainPath, _FileName, SearchOption.AllDirectories));
            if (Directory.Exists(TmpPath))
                _PosibleFiles.AddRange(Directory.GetFiles(TmpPath, _FileName, SearchOption.AllDirectories));

            if (_PosibleFiles.Count > 0)
                OnOpenChapter(_PosibleFiles[0], Resume);
            else
            {
                SendViewModelToastNotification(this, String.Format("Downloading...\n{0}", ChapterEntry.ChapterName(Info, false), ToastNotification.DisplayLength.Short));
                Manager_v1.Instance.DownloadChapter(ChapterEntry.UrlLink);
            }
        }

        private void Resume_Manga(LibraryItemModel LIM)
        {
            if (LIM.VolumeSpecified || LIM.ChapterSpecified || LIM.SubChapterSpecified)
            {
                SendViewModelToastNotification(this, String.Format("Resuming: {0}", LIM.Name));
                MangaInfo Info = MangaDataZip.Instance.MangaInfo(LIM.MangaInfoPath);
                RequestResumeChapter(Info);
                DetailsOpen = false;
            }
        }

        void _LibraryItemLoader_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            QueuedTask<String> _Data = e.Argument as QueuedTask<String>;
            if (File.Exists(_Data.Data))
                e.Result = new LibraryDataClass() { MangaInfo = MangaDataZip.Instance.MangaInfo(_Data.Data), DataPath = _Data.Data };
            else
                e.Result = String.Format("File does not exist.\n{0}", _Data.Data);
        }

        void _LibraryItemLoader_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            if (e.Result is String)
                SendViewModelToastNotification(this, e.Result as String);
            else if (e.Result is LibraryDataClass)
            {
                LibraryDataClass LDC = e.Result as LibraryDataClass;

                lock (MangaItems)
                {
                    Int32 _Index = IndexOfMangaItemsName(LDC.MangaInfo.Name);

                    switch (_Index)
                    {
                        default:
                            MangaItems[_Index].UpdateMangaInfo(LDC.MangaInfo);
                            break;

                        case -1:
                            LibraryItemModel _NewItem = new LibraryItemModel(LDC.MangaInfo, LDC.DataPath) { Cover = MangaDataZip.Instance.CoverStream(LDC.DataPath).StreamToBitmapImage().Image };
                            OnLibraryItemsLoaded(_NewItem);
                            MangaItems.Add(_NewItem);
                            break;
                    }
                    MangaItems.Sort(Item => Item.Name, ListSortDirection.Ascending);


                    if (_Index.Equals(-1) ? MangaItems.Last().ChapterStatus : MangaItems[_Index].ChapterStatus && !HasInit)
                        NewChapters.Enqueue(String.Format("{0} has more chapters to read.", LDC.MangaInfo.Name));

                    if (_LibraryItemLoader.IsQueueEmpty &&
                        !(CurrentMangaItem is LibraryItemModel))
                        CurrentMangaItem = MangaItems.First();
                }
            }
        }

        void LibraryItemLoader_QueueComplete(object Sender)
        {
            while (NewChapters.Count > 0)
            {
                String Text = NewChapters.Dequeue();
                for(Int32 l = 0; l < 24 && NewChapters.Count > 0; ++l)
                {
                    Text = String.Format("{0}\n{1}", Text, NewChapters.Dequeue());
                }
                SendViewModelToastNotification(this, Text, UI.ToastNotification.DisplayLength.Normal);
            }
            if (!HasInit)
            {
                OnInitComplete();
                HasInit = true;
            }
        }
        #endregion
        #endregion

        #region Commands
        private DelegateCommand<ChapterEntry> _ReadChapter { get; set; }
        public ICommand ReadChapter
        {
            get
            {
                if (_ReadChapter == null)
                    _ReadChapter = new DelegateCommand<ChapterEntry>(RequestOpenChapter);
                return _ReadChapter;
            }
        }

        private DelegateCommand _UpdateLibrary { get; set; }
        public ICommand UpdateLibrary
        {
            get
            {
                if (_UpdateLibrary == null)
                    _UpdateLibrary = new DelegateCommand(UpdateLibraryManga);
                return _UpdateLibrary;
            }
        }

        private DelegateCommand<LibraryItemModel> _ResumeManga { get; set; }
        public ICommand ResumeManga
        {
            get
            {
                if (_ResumeManga == null)
                    _ResumeManga = new DelegateCommand<LibraryItemModel>(Resume_Manga);
                return _ResumeManga;
            }
        }
        #endregion
    }

    internal class LibraryDataClass
    {
        public MangaInfo MangaInfo { get; set; }
        public String DataPath { get; set; }
    }
}
