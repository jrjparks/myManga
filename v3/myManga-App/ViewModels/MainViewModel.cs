using System;
using System.ComponentModel;
using System.Net;
using System.Windows;
using myManga_App.IO.Network;
using Core.Other.Singleton;
using Core.MVVM;
using System.Windows.Input;
using System.Runtime.CompilerServices;
using System.IO;
using System.Threading;
using System.Windows.Threading;
using myManga_App.IO.ViewModel;

namespace myManga_App.ViewModels
{
    public sealed class MainViewModel : BaseViewModel
    {
        #region Content
        public static DependencyProperty ContentViewModelProperty = DependencyProperty.Register("ContentViewModel", typeof(BaseViewModel), typeof(BaseViewModel));
        public BaseViewModel ContentViewModel
        {
            get { return GetValue(ContentViewModelProperty) as BaseViewModel; }
            set { SetValue(ContentViewModelProperty, value); }
        }

        private HomeViewModel homeViewModel;
        public HomeViewModel HomeViewModel
        { get { return homeViewModel ?? (homeViewModel = new HomeViewModel()); } }

        private SettingsViewModel settingsViewModel;
        public SettingsViewModel SettingsViewModel
        { get { return settingsViewModel ?? (settingsViewModel = new SettingsViewModel()); } }

        private SearchViewModel searchViewModel;
        public SearchViewModel SearchViewModel
        { get { return searchViewModel ?? (searchViewModel = new SearchViewModel()); } }

        private ReaderViewModel readerViewModel;
        public ReaderViewModel ReaderViewModel
        { get { return readerViewModel ?? (readerViewModel = new ReaderViewModel()); } }
        #endregion

        #region Header Buttons
        private DelegateCommand homeCommand;
        public ICommand HomeCommand
        { get { return homeCommand ?? (homeCommand = new DelegateCommand(OpenHome)); } }

        private void OpenHome()
        { ContentViewModel = HomeViewModel; }

        private DelegateCommand searchCommand;
        public ICommand SearchCommand
        { get { return searchCommand ?? (searchCommand = new DelegateCommand(OpenSearch, CanOpenSearch)); } }

        private void OpenSearch()
        { ContentViewModel = SearchViewModel; }

        private Boolean CanOpenSearch()
        { return SearchViewModel != null; }

        private DelegateCommand readCommand;
        public ICommand ReadCommand
        { get { return readCommand ?? (readCommand = new DelegateCommand(OpenRead, CanOpenRead)); } }

        private void OpenRead()
        { ContentViewModel = ReaderViewModel; }

        private Boolean CanOpenRead()
        { return ReaderViewModel.MangaObject != null && ReaderViewModel.ChapterObject != null; }
        #endregion

        #region Settings
        private DelegateCommand settingsCommand;
        public ICommand SettingsCommand
        { get { return settingsCommand ?? (settingsCommand = new DelegateCommand(OpenSettings)); } }

        private void OpenSettings()
        { ContentViewModel = SettingsViewModel; }
        #endregion

        #region Download Active
        private Boolean isLoading = false;
        public Boolean IsLoading
        {
            get { return isLoading; }
            set
            {
                OnPropertyChanging();
                isLoading = value;
                OnPropertyChanged();
            }
        }
        #endregion

        public MainViewModel()
        {
            ContentViewModel = HomeViewModel;
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                Messenger.Default.RegisterRecipient<BaseViewModel>(this, ChangeViewModelFocus, "FocusRequest");

                SettingsViewModel.CloseEvent += (s, e) => ContentViewModel = HomeViewModel;

                ServicePointManager.DefaultConnectionLimit = Singleton<DownloadManager>.Instance.Concurrency;
                Singleton<DownloadManager>.Instance.StatusChange += (s, e) => { IsLoading = !(s as DownloadManager).IsIdle; };

                App.MangaObjectArchiveWatcher.Changed += MangaObjectArchiveWatcher_Event;
                App.MangaObjectArchiveWatcher.Created += MangaObjectArchiveWatcher_Event;
                App.MangaObjectArchiveWatcher.Deleted += MangaObjectArchiveWatcher_Event;
                App.MangaObjectArchiveWatcher.Renamed += MangaObjectArchiveWatcher_Event;

                App.ChapterObjectArchiveWatcher.Changed += ChapterObjectArchiveWatcher_Event;
                App.ChapterObjectArchiveWatcher.Created += ChapterObjectArchiveWatcher_Event;
                App.ChapterObjectArchiveWatcher.Deleted += ChapterObjectArchiveWatcher_Event;
                App.ChapterObjectArchiveWatcher.Renamed += ChapterObjectArchiveWatcher_Event;
            }
        }

        void MangaObjectArchiveWatcher_Event(object sender, FileSystemEventArgs e)
        {
            if (App.Dispatcher.Thread == Thread.CurrentThread)
            { Messenger.Default.Send(e, "MangaObjectArchive"); }
            else App.Dispatcher.Invoke(DispatcherPriority.Send, new System.Action(() => MangaObjectArchiveWatcher_Event(sender, e)));
        }

        void ChapterObjectArchiveWatcher_Event(object sender, FileSystemEventArgs e)
        {
            if (App.Dispatcher.Thread == Thread.CurrentThread)
            { Messenger.Default.Send(e, "ChapterObjectArchiveWatcher"); }
            else App.Dispatcher.Invoke(DispatcherPriority.Send, new System.Action(() => MangaObjectArchiveWatcher_Event(sender, e)));
        }

        void ChangeViewModelFocus(BaseViewModel ViewModel)
        { this.ContentViewModel = ViewModel; }

        public void Dispose()
        {
            App.SiteExtensions.Unload();
        }
    }
}
