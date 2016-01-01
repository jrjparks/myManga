using Core.MVVM;
using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
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

        #region Pages

        #region PagesHomeViewModelProperty
        private static readonly DependencyPropertyKey PagesHomeViewModelPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            "PagesHomeViewModel",
            typeof(Pages.HomeViewModel),
            typeof(MainViewModel),
            null);
        private static readonly DependencyProperty PagesHomeViewModelProperty = PagesHomeViewModelPropertyKey.DependencyProperty;
        public Pages.HomeViewModel PagesHomeViewModel
        { get { return (Pages.HomeViewModel)GetValue(PagesHomeViewModelProperty); } }
        #endregion

        #region PagesSearchViewModelProperty
        private static readonly DependencyPropertyKey PagesSearchViewModelPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            "PagesSearchViewModel",
            typeof(Pages.SearchViewModel),
            typeof(MainViewModel),
            null);
        private static readonly DependencyProperty PagesSearchViewModelProperty = PagesSearchViewModelPropertyKey.DependencyProperty;
        public Pages.SearchViewModel PagesSearchViewModel
        { get { return (Pages.SearchViewModel)GetValue(PagesSearchViewModelProperty); } }
        #endregion

        #endregion

        #region Header Buttons
        private DelegateCommand homeCommand;
        public ICommand HomeCommand
        { get { return homeCommand ?? (homeCommand = new DelegateCommand(PagesHomeViewModel.PullFocus)); } }

        private DelegateCommand searchCommand;
        public ICommand SearchCommand
        { get { return searchCommand ?? (searchCommand = new DelegateCommand(PagesSearchViewModel.PullFocus)); } }

        private DelegateCommand readCommand;
        public ICommand ReadCommand
        { get { return readCommand ?? (readCommand = new DelegateCommand(ReaderViewModel.PullFocus, CanOpenRead)); } }

        private Boolean CanOpenRead()
        { return ReaderViewModel != null && ReaderViewModel.MangaObject != null && ReaderViewModel.ChapterObject != null; }
        #endregion

        #region Settings
        private DelegateCommand _SettingsCommand;
        public ICommand SettingsCommand
        { get { return _SettingsCommand ?? (_SettingsCommand = new DelegateCommand(SettingsViewModel.PullFocus)); } }
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
                SetValue(PagesHomeViewModelPropertyKey, new Pages.HomeViewModel());
                SetValue(PagesSearchViewModelPropertyKey, new Pages.SearchViewModel());

                //SetValue(HomeViewModelPropertyKey, new HomeViewModel());
                //SetValue(SearchViewModelPropertyKey, new SearchViewModel());
                SetValue(ReaderViewModelPropertyKey, new ReaderViewModel());
                SetValue(SettingsViewModelPropertyKey, new SettingsViewModel());

                Messenger.Default.RegisterRecipient<BaseViewModel>(this, v =>
                {
                    if (ContentViewModel != v)
                        ContentViewModel = v;
                }, "FocusRequest");

                SettingsViewModel.CloseEvent += (s, e) => PreviousContentViewModel.PullFocus();

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
            HomeViewModel.Dispose();
            ReaderViewModel.Dispose();
            SearchViewModel.Dispose();
            SettingsViewModel.Dispose();

            PagesHomeViewModel.Dispose();
            PagesSearchViewModel.Dispose();
        }
    }
}