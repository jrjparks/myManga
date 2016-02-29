using System;
using System.Communication;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Input;

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

        #region Pages

        #region PagesHomeViewModelProperty
        private static readonly DependencyPropertyKey PagesHomeViewModelPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            "PagesHomeViewModel",
            typeof(Pages.HomeViewModel),
            typeof(MainViewModel),
            null);
        private static readonly DependencyProperty PagesHomeViewModelProperty = PagesHomeViewModelPropertyKey.DependencyProperty;
        public Pages.HomeViewModel PagesHomeViewModel
        {
            get { return (Pages.HomeViewModel)GetValue(PagesHomeViewModelProperty); }
            private set { SetValue(PagesHomeViewModelPropertyKey, value); }
        }
        #endregion

        #region PagesSearchViewModelProperty
        private static readonly DependencyPropertyKey PagesSearchViewModelPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            "PagesSearchViewModel",
            typeof(Pages.SearchViewModel),
            typeof(MainViewModel),
            null);
        private static readonly DependencyProperty PagesSearchViewModelProperty = PagesSearchViewModelPropertyKey.DependencyProperty;
        public Pages.SearchViewModel PagesSearchViewModel
        {
            get { return (Pages.SearchViewModel)GetValue(PagesSearchViewModelProperty); }
            private set { SetValue(PagesSearchViewModelPropertyKey, value); }
        }
        #endregion

        #region PagesChapterReaderViewModelProperty
        private static readonly DependencyPropertyKey PagesChapterReaderViewModelPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            "PagesChapterReaderViewModel",
            typeof(Pages.ChapterReaderViewModel),
            typeof(MainViewModel),
            null);
        private static readonly DependencyProperty PagesChapterReaderViewModelProperty = PagesChapterReaderViewModelPropertyKey.DependencyProperty;
        public Pages.ChapterReaderViewModel PagesChapterReaderViewModel
        {
            get { return (Pages.ChapterReaderViewModel)GetValue(PagesChapterReaderViewModelProperty); }
            private set { SetValue(PagesChapterReaderViewModelPropertyKey, value); }
        }
        #endregion

        #region PagesSettingsViewModelProperty
        private static readonly DependencyPropertyKey PagesSettingsViewModelPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            "PagesSettingsViewModel",
            typeof(Pages.SettingsViewModel),
            typeof(MainViewModel),
            null);
        private static readonly DependencyProperty PagesSettingsViewModelProperty = PagesSettingsViewModelPropertyKey.DependencyProperty;
        public Pages.SettingsViewModel PagesSettingsViewModel
        {
            get { return (Pages.SettingsViewModel)GetValue(PagesSettingsViewModelProperty); }
            private set { SetValue(PagesSettingsViewModelPropertyKey, value); }
        }
        #endregion

        #endregion

        #endregion

        #region Header Buttons

        #region Home
        private DelegateCommand homeCommand;
        public ICommand HomeCommand
        { get { return homeCommand ?? (homeCommand = new DelegateCommand(PagesHomeViewModel.PullFocus)); } }
        #endregion

        #region Search
        private DelegateCommand searchCommand;
        public ICommand SearchCommand
        { get { return searchCommand ?? (searchCommand = new DelegateCommand(PagesSearchViewModel.PullFocus)); } }
        #endregion

        #region Settings
        private DelegateCommand _SettingsCommand;
        public ICommand SettingsCommand
        { get { return _SettingsCommand ?? (_SettingsCommand = new DelegateCommand(PagesSettingsViewModel.PullFocus)); } }
        #endregion

        #region Read
        private DelegateCommand readCommand;
        public ICommand ReadCommand
        { get { return readCommand ?? (readCommand = new DelegateCommand(PagesChapterReaderViewModel.PullFocus, CanOpenRead)); } }

        private Boolean CanOpenRead()
        {
            if (Equals(PagesChapterReaderViewModel, null)) return false;
            if (Equals(PagesChapterReaderViewModel.MangaObject, null)) return false;
            if (Equals(PagesChapterReaderViewModel.ChapterObject, null)) return false;
            return true;
        }
        #endregion

        #endregion

        #region Download Active
        private Timer ActiveDownloadsTime
        { get; set; }

        private static readonly DependencyProperty DownloadsActiveProperty = DependencyProperty.RegisterAttached(
            "DownloadsActive",
            typeof(Boolean),
            typeof(MainViewModel),
            new PropertyMetadata(false));

        public Boolean DownloadsActive
        {
            get { return (Boolean)GetValue(DownloadsActiveProperty); }
            set { SetValue(DownloadsActiveProperty, value); }
        }
        #endregion

        public MainViewModel()
            : base()
        {
            if (!IsInDesignMode)
            {
                PagesHomeViewModel = new Pages.HomeViewModel();
                PagesSearchViewModel = new Pages.SearchViewModel();
                PagesChapterReaderViewModel = new Pages.ChapterReaderViewModel();
                PagesSettingsViewModel = new Pages.SettingsViewModel();

                // SetValue(SettingsViewModelPropertyKey, new SettingsViewModel());

                Messenger.Instance.RegisterRecipient<BaseViewModel>(this, v =>
                {
                    if (ContentViewModel != v)
                    {
                        if (!Equals(ContentViewModel, null)) { ContentViewModel.LostFocus(); }
                        ContentViewModel = v;
                    }
                }, "FocusRequest");

                Messenger.Instance.RegisterRecipient<Boolean>(this, b =>
                {
                    if (!Equals(PreviousContentViewModel, null) && b)
                        PreviousContentViewModel.PullFocus();
                }, "PreviousFocusRequest");

                ServicePointManager.DefaultConnectionLimit = App.ContentDownloadManager.DownloadConcurrency;

                ActiveDownloadsTime = new Timer(state =>
                {   // Monitor the ContentDownloadManager IsActive property
                    App.RunOnUiThread(new Action(() =>
                    {
                        if (!Equals(App.ContentDownloadManager.IsActive, DownloadsActive))
                            DownloadsActive = App.ContentDownloadManager.IsActive;
                    }));
                }, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

                PagesHomeViewModel.PullFocus();
            }
            else ContentViewModel = PagesHomeViewModel;
        }

        protected override void SubDispose()
        {
            PagesHomeViewModel.Dispose();
            PagesSearchViewModel.Dispose();
            PagesChapterReaderViewModel.Dispose();
            PagesSettingsViewModel.Dispose();
        }
    }
}