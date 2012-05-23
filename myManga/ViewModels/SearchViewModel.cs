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

        private IMangaPlugin _Plugin;
        public IMangaPlugin Plugin
        {
            get { return _Plugin; }
            set { _Plugin = value; OnPropertyChanged("Plugin"); }
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

        private QueuedBackgroundWorker<String> _SearchQueueWorker;
        private QueuedBackgroundWorker<String> SearchQueueWorker
        {
            get
            {
                if (_SearchQueueWorker == null)
                    _SearchQueueWorker = new QueuedBackgroundWorker<String>();
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
            GIMPC_PropertyChanged(null, new PropertyChangedEventArgs("Plugins"));
            _SQWEvents();
            Global_IMangaPluginCollection.Instance.PropertyChanged += GIMPC_PropertyChanged;
        }
        private void _SQWEvents()
        {
            SearchQueueWorker.WorkerReportsProgress = true;
            SearchQueueWorker.DoWork += SearchQueueWorker_DoWork;
            SearchQueueWorker.RunWorkerCompleted += SearchQueueWorker_RunWorkerCompleted;
            SearchQueueWorker.ProgressChanged += (s, e) =>
                { Progress = (UInt32)e.ProgressPercentage; };
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
            QueuedTask<String> Task = e.Argument as QueuedTask<String>;
            if (_Plugin != null)
            {
                if (_Plugin.SupportedMethods.Has(SupportedMethods.Search))
                {
                    _Plugin.ProgressChanged += _Plugin_ProgressChanged;
                    e.Result = Plugin.Search(Task.Data, 50);
                    _Plugin.ProgressChanged -= _Plugin_ProgressChanged;
                }
                else
                    e.Result = "Search is not supported by this site.";
            }
        }
        private void SearchQueueWorker_RunWorkerCompleted(Object s, RunWorkerCompletedEventArgs e)
        {
            Progress = 0;
            if (e.Result is SearchInfoCollection)
                SearchCollection = e.Result as SearchInfoCollection;
            else if (e.Result is String)
                SendViewModelToastNotification(this, e.Result as String, UI.ToastNotification.DisplayLength.Normal);
        }
        #endregion

        #region Private
        private void Search(String SearchText)
        {
            if (Plugin != null)
                SearchQueueWorker.AddToQueue(SearchText);
        }

        private void _Plugin_ProgressChanged(object Sender, int Progress, object Data)
        {
            if (SearchQueueWorker != null)
                SearchQueueWorker.ReportProgress(Progress, Data);
        }
        #endregion

        #region Public
        #endregion
        #endregion
    }
}
