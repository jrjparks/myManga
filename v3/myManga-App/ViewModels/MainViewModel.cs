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
        public static DependencyProperty ContentViewModelProperty = DependencyProperty.Register(
            "ContentViewModel", 
            typeof(BaseViewModel), 
            typeof(MainViewModel));
        public BaseViewModel ContentViewModel
        {
            get { return GetValue(ContentViewModelProperty) as BaseViewModel; }
            set { SetValue(ContentViewModelProperty, value); }
        }

        private HomeViewModel _HomeViewModel;
        public HomeViewModel HomeViewModel
        { get { return _HomeViewModel ?? (_HomeViewModel = new HomeViewModel()); } }

        private SettingsViewModel _SettingsViewModel;
        public SettingsViewModel SettingsViewModel
        { get { return _SettingsViewModel ?? (_SettingsViewModel = new SettingsViewModel()); } }

        private SearchViewModel _SearchViewModel;
        public SearchViewModel SearchViewModel
        { get { return _SearchViewModel ?? (_SearchViewModel = new SearchViewModel()); } }

        private ReaderViewModel _ReaderViewModel;
        public ReaderViewModel ReaderViewModel
        { get { return _ReaderViewModel ?? (_ReaderViewModel = new ReaderViewModel()); } }
        #endregion

        #region Header Buttons
        private DelegateCommand _HomeCommand;
        public ICommand HomeCommand
        { get { return _HomeCommand ?? (_HomeCommand = new DelegateCommand(HomeViewModel.PullFocus)); } }

        private DelegateCommand _SearchCommand;
        public ICommand SearchCommand
        { get { return _SearchCommand ?? (_SearchCommand = new DelegateCommand(SearchViewModel.PullFocus, CanOpenSearch)); } }

        private Boolean CanOpenSearch()
        { return SearchViewModel != null; }

        private DelegateCommand _ReadCommand;
        public ICommand ReadCommand
        { get { return _ReadCommand ?? (_ReadCommand = new DelegateCommand(ReaderViewModel.PullFocus, CanOpenRead)); } }

        private Boolean CanOpenRead()
        { return ReaderViewModel != null && ReaderViewModel.MangaObject != null && ReaderViewModel.ChapterObject != null; }
        #endregion

        #region Settings
        private DelegateCommand _SettingsCommand;
        public ICommand SettingsCommand
        { get { return _SettingsCommand ?? (_SettingsCommand = new DelegateCommand(SettingsViewModel.PullFocus)); } }
        #endregion

        #region Download Active
        private Boolean _IsLoading = false;
        public Boolean IsLoading
        {
            get { return _IsLoading; }
            set { SetProperty(ref this._IsLoading, value); }
        }
        #endregion

        public MainViewModel()
            : base()
        {
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                Messenger.Default.RegisterRecipient<BaseViewModel>(this, v => this.ContentViewModel = v, "FocusRequest");

                SettingsViewModel.CloseEvent += (s, e) => HomeViewModel.PullFocus();

                ServicePointManager.DefaultConnectionLimit = DownloadManager.Default.Concurrency;
                DownloadManager.Default.StatusChange += (s, e) => { IsLoading = !(s as DownloadManager).IsIdle; };

                App.MangaObjectArchiveWatcher.Changed += MangaObjectArchiveWatcher_Event;
                App.MangaObjectArchiveWatcher.Created += MangaObjectArchiveWatcher_Event;
                App.MangaObjectArchiveWatcher.Deleted += MangaObjectArchiveWatcher_Event;
                App.MangaObjectArchiveWatcher.Renamed += MangaObjectArchiveWatcher_Event;

                App.ChapterObjectArchiveWatcher.Changed += ChapterObjectArchiveWatcher_Event;
                App.ChapterObjectArchiveWatcher.Created += ChapterObjectArchiveWatcher_Event;
                App.ChapterObjectArchiveWatcher.Deleted += ChapterObjectArchiveWatcher_Event;
                App.ChapterObjectArchiveWatcher.Renamed += ChapterObjectArchiveWatcher_Event;

                HomeViewModel.PullFocus();
            }
            else ContentViewModel = HomeViewModel;
        }

        void MangaObjectArchiveWatcher_Event(object sender, FileSystemEventArgs e)
        {
            if (App.Dispatcher.Thread == Thread.CurrentThread)
            { Messenger.Default.Send(e, "MangaObjectArchiveWatcher"); }
            else App.Dispatcher.Invoke(DispatcherPriority.Send, new System.Action(() => MangaObjectArchiveWatcher_Event(sender, e)));
        }

        void ChapterObjectArchiveWatcher_Event(object sender, FileSystemEventArgs e)
        {
            if (App.Dispatcher.Thread == Thread.CurrentThread)
            { Messenger.Default.Send(e, "ChapterObjectArchiveWatcher"); }
            else App.Dispatcher.Invoke(DispatcherPriority.Send, new System.Action(() => ChapterObjectArchiveWatcher_Event(sender, e)));
        }

        public override void Dispose()
        {
            base.Dispose();
            App.SiteExtensions.Unload();
        }
    }
}