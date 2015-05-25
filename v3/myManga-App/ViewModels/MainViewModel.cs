using Core.MVVM;
using myManga_App.IO.Network;
using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace myManga_App.ViewModels
{
    public sealed class MainViewModel : BaseViewModel
    {
        #region Content
        private BaseViewModel PreviousContentViewModel;
        private static readonly DependencyProperty ContentViewModelProperty = DependencyProperty.RegisterAttached(
            "ContentViewModel", 
            typeof(BaseViewModel), 
            typeof(MainViewModel));
        public BaseViewModel ContentViewModel
        {
            get { return GetValue(ContentViewModelProperty) as BaseViewModel; }
            set { PreviousContentViewModel = ContentViewModel; SetValue(ContentViewModelProperty, value); }
        }

        #region HomeViewModelProperty
        private static readonly DependencyPropertyKey HomeViewModelPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            "HomeViewModel",
            typeof(HomeViewModel),
            typeof(MainViewModel),
            null);
        private static readonly DependencyProperty HomeViewModelProperty = HomeViewModelPropertyKey.DependencyProperty;
        public HomeViewModel HomeViewModel
        { get { return (HomeViewModel)GetValue(HomeViewModelProperty); } }
        #endregion

        #region ReaderViewModelProperty
        private static readonly DependencyPropertyKey ReaderViewModelPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            "ReaderViewModel",
            typeof(ReaderViewModel),
            typeof(MainViewModel),
            null);
        private static readonly DependencyProperty ReaderViewModelProperty = ReaderViewModelPropertyKey.DependencyProperty;
        public ReaderViewModel ReaderViewModel
        { get { return (ReaderViewModel)GetValue(ReaderViewModelProperty); } }
        #endregion

        #region SearchViewModelProperty
        private static readonly DependencyPropertyKey SearchViewModelPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            "SearchViewModel",
            typeof(SearchViewModel),
            typeof(MainViewModel),
            null);
        private static readonly DependencyProperty SearchViewModelProperty = SearchViewModelPropertyKey.DependencyProperty;
        public SearchViewModel SearchViewModel
        { get { return (SearchViewModel)GetValue(SearchViewModelProperty); } }
        #endregion

        #region SettingsViewModelProperty
        private static readonly DependencyPropertyKey SettingsViewModelPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            "SettingsViewModel",
            typeof(SettingsViewModel),
            typeof(MainViewModel),
            null);
        private static readonly DependencyProperty SettingsViewModelProperty = SettingsViewModelPropertyKey.DependencyProperty;
        public SettingsViewModel SettingsViewModel
        { get { return (SettingsViewModel)GetValue(SettingsViewModelProperty); } }
        #endregion
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
            SetValue(HomeViewModelPropertyKey, new HomeViewModel());
            SetValue(ReaderViewModelPropertyKey, new ReaderViewModel());
            SetValue(SearchViewModelPropertyKey, new SearchViewModel());
            SetValue(SettingsViewModelPropertyKey, new SettingsViewModel());

            if (!IsInDesignMode)
            {
                Messenger.Default.RegisterRecipient<BaseViewModel>(this, v => {
                    if (this.ContentViewModel != v)
                        this.ContentViewModel = v;
                }, "FocusRequest");

                SettingsViewModel.CloseEvent += (s, e) => this.PreviousContentViewModel.PullFocus();

                ServicePointManager.DefaultConnectionLimit = App.DownloadManager.Concurrency;
                App.DownloadManager.StatusChange += (s, e) => { IsLoading = !(s as DownloadManager).IsIdle; };

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