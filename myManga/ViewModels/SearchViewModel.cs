using System;
using System.Collections.Generic;
using System.Windows.Input;
using BakaBox.MVVM;
using BakaBox.Extensions;
using Manga.Core;
using Manga.Plugin;
using BakaBox.Controls.Threading;
using System.Windows;
using Manga.Manager;
using System.ComponentModel;
using myManga.UI;
using Manga.Info;
using System.Threading;
using myManga.Properties;

namespace myManga.ViewModels
{
    public sealed class SearchViewModel : ViewModelBase
    {
        #region Variables
        private SearchInfoCollection _SearchCollection { get; set; }
        public SearchInfoCollection SearchCollection
        {
            get { return _SearchCollection; }
            set { _SearchCollection = value; OnPropertyChanged("SearchCollection"); }
        }
        private delegate void UpdateSearchCollectionDelegate(SearchInfoCollection value);
        private void UpdateSearchCollection(SearchInfoCollection value)
        {
            if (Application.Current.Dispatcher.Thread == Thread.CurrentThread)
                SearchCollection = value;
            else
                Application.Current.Dispatcher.Invoke(new UpdateSearchCollectionDelegate(UpdateSearchCollection), value);
        }

        private SearchInfo _SearchInf;
        public SearchInfo SearchInf
        {
            get { return _SearchInf; }
            set
            {
                _SearchInf = value;
                DetailsInfo = null;
                DetailsOpen = DetailsOpen;
                DetailsEnabled = (value != null);
                OnPropertyChanged("SearchInf");
            }
        }

        private IMangaPlugin _Plugin;
        public IMangaPlugin Plugin
        {
            get { return _Plugin; }
            set { _Plugin = value; OnPropertyChanged("Plugin"); }
        }

        #region Progress
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
        private delegate void UpdateProgressDelegate(Double value);
        private void UpdateProgress(Double value)
        {
            if (Application.Current.Dispatcher.Thread == Thread.CurrentThread)
                Progress = value;
            else
                Application.Current.Dispatcher.Invoke(new UpdateProgressDelegate(UpdateProgress), value);
        }
        #endregion

        #region Details
        private Boolean _DetailsOpen;
        public Boolean DetailsOpen
        {
            get { return _DetailsOpen; }
            set
            {
                _DetailsOpen = value;
                if (value)
                    if (_Plugin != null && SearchInf != null)
                        Details(SearchInf.InformationLocation);
                OnPropertyChanged("DetailsOpen");
            }
        }

        private Boolean _DetailsEnabled;
        public Boolean DetailsEnabled
        {
            get { return _DetailsEnabled; }
            set
            {
                _DetailsEnabled = value;
                if (!value)
                {
                    DetailsOpen = false;
                    DetailsInfo = null;
                }
                OnPropertyChanged("DetailsEnabled");
            }
        }

        private MangaInfo _DetailsInfo;
        public MangaInfo DetailsInfo
        {
            get { return _DetailsInfo; }
            set
            {
                _DetailsInfo = value;
                OnPropertyChanged("DetailsInfo");
            }
        }
        private delegate void UpdateDetailsInfoDelegate(MangaInfo value);
        private void UpdateDetailsInfo(MangaInfo value)
        {
            if (Application.Current.Dispatcher.Thread == Thread.CurrentThread)
                DetailsInfo = value;
            else
                Application.Current.Dispatcher.Invoke(new UpdateDetailsInfoDelegate(UpdateDetailsInfo), value);
        }
        #endregion

        private QueuedBackgroundWorker<WorkerTask> _SearchQueueWorker;
        private QueuedBackgroundWorker<WorkerTask> SearchQueueWorker
        {
            get
            {
                if (_SearchQueueWorker == null)
                    _SearchQueueWorker = new QueuedBackgroundWorker<WorkerTask>();
                return _SearchQueueWorker;
            }
        }
        #endregion
        
        #region Commands
        private DelegateCommand<String> _SearchMangaText { get; set; }
        public ICommand SearchMangaText
        {
            get
            {
                if (_SearchMangaText == null)
                    _SearchMangaText = new DelegateCommand<String>(Search);
                return _SearchMangaText;
            }
        }

        private DelegateCommand<SearchInfo> _DownloadMangaInfo { get; set; }
        public ICommand DownloadMangaInfo
        {
            get
            {
                if (_DownloadMangaInfo == null)
                    _DownloadMangaInfo = new DelegateCommand<SearchInfo>(AddMangaInfoForDownload);
                return _DownloadMangaInfo;
            }
        }
        private void AddMangaInfoForDownload(SearchInfo Item)
        {
            SendViewModelToastNotification(this, String.Format("Downloading...\n{0}", Item.Title), ToastNotification.DisplayLength.Short);
            Manager_v1.Instance.DownloadManga(Item.InformationLocation);
        }
        #endregion

        #region Constructor
        public SearchViewModel()
        {
            Progress = 0;
            GIMPC_PropertyChanged(null, new PropertyChangedEventArgs("Plugins"));
            _SQWEvents();
            Global_IMangaPluginCollection.Instance.PropertyChanged += GIMPC_PropertyChanged;
        }
        private void _SQWEvents()
        {
            SearchQueueWorker.WorkerReportsProgress = true;
            SearchQueueWorker.DoWork += SearchQueueWorker_DoWork;
            SearchQueueWorker.RunWorkerCompleted += SearchQueueWorker_RunWorkerCompleted;
            SearchQueueWorker.ProgressChanged += (s, e) => UpdateProgress(e.ProgressPercentage);
        }
        #endregion

        #region Methods
        private void GIMPC_PropertyChanged(Object s, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("Plugins"))
                if (Global_IMangaPluginCollection.Instance.Plugins.Count > 0)
                    Plugin = Global_IMangaPluginCollection.Instance.Plugins[0];
        }

        #region SearchQueueWorker
        private void SearchQueueWorker_DoWork(Object s, DoWorkEventArgs e)
        {
            QueuedTask<WorkerTask> Task = e.Argument as QueuedTask<WorkerTask>;
            if (_Plugin != null)
            {
                if (Task.Data.Work != String.Empty && Task.Data.Work.Length >= 2)
                    switch (Task.Data.wType)
                    {
                        default:
                        case WorkerTask.WorkType.Search:
                            if (_Plugin.SupportedMethods.Has(SupportedMethods.Search))
                            {
                                _Plugin.ProgressChanged += _Plugin_ProgressChanged;
                                e.Result = Plugin.Search(Task.Data.Work, 50);
                                _Plugin.ProgressChanged -= _Plugin_ProgressChanged;
                            }
                            else
                                e.Result = "Search is not supported by this site.";
                            break;

                        case WorkerTask.WorkType.Details:
                            if (_Plugin.SupportedMethods.Has(SupportedMethods.MangaInfo))
                            {
                                _Plugin.ProgressChanged += _Plugin_ProgressChanged;
                                MangaInfo tmpMangaInfo = Plugin.LoadMangaInformation(Task.Data.Work);
                                switch (Settings.Default.ChapterListOrder)
                                {
                                    default:
                                    case Base.ChapterOrder.Ascending:
                                        break;

                                    case Base.ChapterOrder.Auto:
                                    case Base.ChapterOrder.Descending:
                                        tmpMangaInfo.ChapterEntries.Reverse();
                                        break;
                                }
                                e.Result = tmpMangaInfo;
                                _Plugin.ProgressChanged -= _Plugin_ProgressChanged;
                            }
                            else
                                e.Result = "MangaInfo is not supported by this site.\nDO NOT USE IT!";
                            break;
                    }
            }
        }
        private void SearchQueueWorker_RunWorkerCompleted(Object s, RunWorkerCompletedEventArgs e)
        {
            UpdateProgress(0);
            if (e.Result is SearchInfoCollection)
                UpdateSearchCollection(e.Result as SearchInfoCollection);
            else if (e.Result is MangaInfo)
                UpdateDetailsInfo(e.Result as MangaInfo);
            else if (e.Result is String)
                SendViewModelToastNotification(this, e.Result as String, UI.ToastNotification.DisplayLength.Normal);
        }
        #endregion

        #region Private
        private void Search(String SearchText)
        {
            if (Plugin != null)
                SearchQueueWorker.AddToQueue(new WorkerTask(SearchText, WorkerTask.WorkType.Search));
        }
        private void Details(String DetailsURL)
        {
            if (Plugin != null)
                SearchQueueWorker.AddToQueue(new WorkerTask(DetailsURL, WorkerTask.WorkType.Details));
        }

        private void _Plugin_ProgressChanged(object Sender, int Progress, object Data)
        {
            if (SearchQueueWorker != null)
                SearchQueueWorker.ReportProgress(1 + Progress, Data);
        }
        #endregion

        #region Public
        #endregion
        #endregion

        private class WorkerTask
        {
            public enum WorkType { Search, Details }
            public WorkType wType { get; set; }
            public String Work { get; set; }

            public WorkerTask()
                : this(String.Empty, WorkType.Search)
            { }
            public WorkerTask(String Work, WorkType wType)
            { this.Work = Work; this.wType = wType; }
        }
    }
}
