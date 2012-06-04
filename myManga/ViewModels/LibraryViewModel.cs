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
using BakaBox.IO;
using BakaBox.MVVM.Communications;
using myManga.Properties;

namespace myManga.ViewModels
{
    public sealed class LibraryViewModel : ViewModelBase
    {
        #region Classes
        private class LibraryDataClass
        {
            public MangaInfo MangaInfo { get; set; }
            public String DataPath { get; set; }

            public LibraryDataClass()
                : this(null, String.Empty) { }
            public LibraryDataClass(MangaInfo MangaInfo, String DataPath)
            {
                this.MangaInfo = MangaInfo;
                this.DataPath = DataPath;
            }
        }
        #endregion

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
        #endregion

        #region Variables
        private Boolean HasInit;

        private Boolean _DetailsOpen;
        public Boolean DetailsOpen
        {
            get { return _DetailsOpen; }
            set
            {
                _DetailsOpen = value;
                OnPropertyChanged("DetailsOpen");
                if ((CurrentMangaItem is LibraryItemModel) &&
                    (!(CurrentInfo is MangaInfo) || !CurrentInfo.Name.Equals(CurrentMangaItem.MangaData.Name)))
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

        private LibraryItemModel _CurrentMangaItem;
        public LibraryItemModel CurrentMangaItem
        {
            get { return _CurrentMangaItem; }
            set
            {
                _CurrentMangaItem = value;
                if (value != null && DetailsOpen)
                {
                    CurrentInfo = MangaDataZip.Instance.MangaInfo(value.MangaInfoPath);
                }
                OnPropertyChanged("CurrentMangaItem");
            }
        }
        private ChapterEntryCollection origChapEntryCollection;
        private MangaInfo _CurrentInfo;
        public MangaInfo CurrentInfo
        {
            get
            {
                if (_CurrentInfo != null && origChapEntryCollection != null)
                {
                    ChapterEntry[] tmpChapterEntry = new ChapterEntry[origChapEntryCollection.Count];
                    origChapEntryCollection.CopyTo(tmpChapterEntry, 0);
                    _CurrentInfo.ChapterEntries = new ChapterEntryCollection(tmpChapterEntry);
                    switch (Settings.Default.ChapterListOrder)
                    {
                        default:
                        case Base.ChapterOrder.Ascending:
                            break;

                        case Base.ChapterOrder.Descending:
                            _CurrentInfo.ChapterEntries.Reverse();
                            break;

                        case Base.ChapterOrder.Auto:
                            if ((origChapEntryCollection.Count / 2) < origChapEntryCollection.IndexOf(_CurrentInfo.LastReadChapterEntry))
                                _CurrentInfo.ChapterEntries.Reverse(); // Reverse List if more than halfway.
                            break;
                    }
                }
                return _CurrentInfo;
            }
            set
            {
                _CurrentInfo = value;
                if (value != null)
                    origChapEntryCollection = value.ChapterEntries;
                else
                    origChapEntryCollection = null;
                Messenger.Instance.SendBroadcastMessage(this, "!^RequestCanExecuteUpdate");
                OnPropertyChanged("CurrentInfo");
            }
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

            MangaDataZip.Instance.MangaInfoUpdated += UpdateLibraryMangaInfo;
            Manager_v1.Instance.TaskProgress += UpdateTaskData;
            Manager_v1.Instance.TaskComplete += UpdateTaskData;

            LoadLibraryDirectory(MangaDataZip.Instance.MIZAPath.SafeFolder());

            Settings.Default.PropertyChanged += Default_PropertyChanged;
        }

        private void Default_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("ChapterListOrder"))
                OnPropertyChanged("CurrentInfo");
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
        {
            String[] Files = Directory.GetFiles(FolderPath, "*.miza", SearchOption.TopDirectoryOnly);
            if (Files.Length > 0)
                LoadLibraryData(Files);
            else
                EnableHasInit();
        }

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
            if (HasInit)
            {
                Int32 UpdateCount = 0;
                foreach (LibraryItemModel Item in MangaItems)
                {
                    if (Item.MangaStatus == MangaStatus.Ongoing)
                    {
                        ++UpdateCount;
                        Item.Progress = 1;
                        MangaInfo tmpInfo = MangaDataZip.Instance.MangaInfo(Item.MangaInfoPath);

                        if (File.Exists(Path.Combine(Environment.CurrentDirectory, "MangaInfo", tmpInfo.MangaDataName())))
                            Manager_v1.Instance.DownloadManga(tmpInfo, Item.SessionMangaID);
                        else
                        {
                            SendViewModelToastNotification(this, String.Format("Downloading...\n{0}", tmpInfo.Name), ToastNotification.DisplayLength.Short);
                            Manager_v1.Instance.DownloadManga(tmpInfo.InfoPage, Item.SessionMangaID);
                        }
                    }
                }
                SendViewModelToastNotification(this, String.Format("Updating {0} Mangas...", UpdateCount), UI.ToastNotification.DisplayLength.Normal);
            }
        }

        public void OpenDetails()
        { DetailsOpen = true; }
        public void CloseDetails()
        { DetailsOpen = false; }
        #endregion

        #region Private
        private delegate void TaskUpdateDelegate(Object Sender, QueuedTask<ManagerData<String, MangaInfo>> Task);
        private void UpdateTaskData(Object Sender, QueuedTask<ManagerData<String, MangaInfo>> Task)
        {
            if (Application.Current.Dispatcher.Thread == Thread.CurrentThread)
            {
                foreach (LibraryItemModel Item in MangaItems)
                    if (Item.SessionMangaID.Equals(Task.Guid))
                        switch (Task.TaskStatus)
                        {
                            default:
                                Item.Progress = Task.Progress;
                                break;
                            case System.Threading.Tasks.TaskStatus.RanToCompletion:
                                Item.Progress = 0;
                                if (Item.ChapterStatus)
                                    SendViewModelToastNotification(this, String.Format("{0} has more chapters to read.", Item.MangaData.Name), UI.ToastNotification.DisplayLength.Normal);
                                break;
                        }
            }
            else
                Application.Current.Dispatcher.BeginInvoke(new TaskUpdateDelegate(UpdateTaskData), Sender, Task);
        }
        private Int32 IndexOfMangaItemsName(String Name)
        {
            Boolean _Found = false;
            Int32 _Index = 0;
            foreach (LibraryItemModel _LibItem in MangaItems)
            {
                if (_LibItem.MangaData.Name.Equals(Name))
                {
                    _Found = true;
                    break;
                }
                else ++_Index;
            }
            return _Found ? _Index : -1;
        }

        #region Chapter Request
        private void RequestOpenChapter(ChapterEntry ChapterEntry)
        { ChapterWorkerData(ChapterEntry, CurrentInfo, false, false); }
        private void RequestResumeChapter(MangaInfo Info)
        { ChapterWorkerData(Info.LastReadChapterEntry, Info, true, false); }
        private void DownloadOpenChapter(ChapterEntry ChapterEntry)
        { ChapterWorkerData(ChapterEntry, CurrentInfo, false, true); }
        private void ChapterWorkerData(ChapterEntry ChapterEntry, MangaInfo Info, Boolean Resume, Boolean Download)
        {
            String _FileName = ChapterEntry.ChapterName(Info),
                           MainPath = MangaDataZip.Instance.MZAPath,
                           TmpPath = ZipNamingExtensions.TempSaveLocation;

            List<String> _PosibleFiles = new List<String>();
            if (Directory.Exists(MainPath))
                _PosibleFiles.AddRange(Directory.GetFiles(MainPath, _FileName, SearchOption.AllDirectories));
            if (Directory.Exists(TmpPath))
                _PosibleFiles.AddRange(Directory.GetFiles(TmpPath, _FileName, SearchOption.AllDirectories));

            if (_PosibleFiles.Count > 0 && !Download)
            {
                DetailsOpen = false;
                OnOpenChapter(_PosibleFiles[0], Resume);
            }
            else
            {
                SendViewModelToastNotification(this, String.Format("Downloading...\n{0}", ChapterEntry.ChapterName(Info, false), ToastNotification.DisplayLength.Short));
                Manager_v1.Instance.DownloadChapter(ChapterEntry);
            }
        }
        #endregion

        private void Resume_Manga(LibraryItemModel LIM)
        {
            if (LIM.MangaData.VolumeSpecified || LIM.MangaData.ChapterSpecified || LIM.MangaData.SubChapterSpecified)
            {
                SendViewModelToastNotification(this, String.Format("Resuming: {0}", LIM.MangaData.Name));
                MangaInfo Info = MangaDataZip.Instance.MangaInfo(LIM.MangaInfoPath);
                RequestResumeChapter(Info);
                DetailsOpen = false;
            }
        }

        #region Library Loading
        private void _LibraryItemLoader_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            QueuedTask<String> _Data = e.Argument as QueuedTask<String>;
            if (File.Exists(_Data.Data))
                e.Result = new LibraryDataClass(MangaDataZip.Instance.MangaInfo(_Data.Data), _Data.Data);
            else
                e.Result = String.Format("File does not exist.\n{0}", _Data.Data);
        }

        private void _LibraryItemLoader_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
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
                    MangaItems.Sort(Item => Item.MangaData.Name, ListSortDirection.Ascending);


                    if (_Index.Equals(-1) ? MangaItems.Last().ChapterStatus : MangaItems[_Index].ChapterStatus && !HasInit)
                        NewChapters.Enqueue(String.Format("{0} has more chapters to read.", LDC.MangaInfo.Name));

                    if (_LibraryItemLoader.IsQueueEmpty &&
                        !(CurrentMangaItem is LibraryItemModel))
                        CurrentMangaItem = MangaItems.First();
                }
            }
        }

        private void LibraryItemLoader_QueueComplete(object Sender)
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
            EnableHasInit();
        }
        #endregion

        private void EnableHasInit()
        {
            if (!HasInit)
            {
                HasInit = true;
                Messenger.Instance.SendBroadcastMessage(this, "!^RequestCanExecuteUpdate");
                if (MangaItems.Count == 0)
                {
                    Messenger.Instance.SendBroadcastMessage(this, "!^SetViewIndex3");
                    SendViewModelToastNotification(this, String.Empty, ToastNotification.DisplayLength.Long);
                    SendViewModelToastNotification(this, "It seems to be your first time running myManga.\nTo get started search for a manga to read.", ToastNotification.DisplayLength.Long);
                }
                Messenger.Instance.SendBroadcastMessage(this, "!^ResumeToast");
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
                    _UpdateLibrary = new DelegateCommand(UpdateLibraryManga, CanUpdateLibrary);
                return _UpdateLibrary;
            }
        }
        private Boolean CanUpdateLibrary()
        { return HasInit; }

        private DelegateCommand _DownloadAllChapters { get; set; }
        public ICommand DownloadAllChapters
        {
            get
            {
                if (_DownloadAllChapters == null)
                    _DownloadAllChapters = new DelegateCommand(DownloadAll, CanDownloadAll);
                return _DownloadAllChapters;
            }
        }
        private void DownloadAll()
        {
            foreach (ChapterEntry ChapEnt in origChapEntryCollection)
                DownloadOpenChapter(ChapEnt);
        }
        private Boolean CanDownloadAll()
        { return origChapEntryCollection != null; }

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
}
