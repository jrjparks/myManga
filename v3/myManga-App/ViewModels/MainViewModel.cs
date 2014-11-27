using System;
using System.ComponentModel;
using System.Net;
using System.Windows;
using myManga_App.IO.Network;
using Core.Other.Singleton;
using Core.MVVM;
using System.Windows.Input;
using System.Runtime.CompilerServices;

namespace myManga_App.ViewModels
{
    public sealed class MainViewModel : DependencyObject, IDisposable, INotifyPropertyChanging, INotifyPropertyChanged
    {
        #region NotifyPropertyChange
        public event PropertyChangingEventHandler PropertyChanging;
        private void OnPropertyChanging([CallerMemberName] String caller = "")
        {
            if (PropertyChanging != null)
                PropertyChanging(this, new PropertyChangingEventArgs(caller));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] String caller = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(caller));
        }
        #endregion

        #region Content
        public static DependencyProperty ContentViewModelProperty = DependencyProperty.Register("ContentViewModel", typeof(Object), typeof(MainViewModel));
        public Object ContentViewModel
        {
            get { return GetValue(ContentViewModelProperty) as Object; }
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

        private readonly App App = App.Current as App;

        public MainViewModel()
        {
            ContentViewModel = HomeViewModel;
            SettingsViewModel.CloseEvent += (s, e) => ContentViewModel = HomeViewModel;
            HomeViewModel.SearchEvent += (s, e) => { 
                ContentViewModel = SearchViewModel;
                SearchViewModel.StartSearch(e);
            };
            HomeViewModel.ReadChapterEvent += (s, m, c) =>
            {
                ContentViewModel = ReaderViewModel;
                ReaderViewModel.OpenChapter(m, c);
            };

            ServicePointManager.DefaultConnectionLimit = Singleton<DownloadManager>.Instance.Concurrency;
            Singleton<DownloadManager>.Instance.StatusChange += (s, e) => { IsLoading = !(s as DownloadManager).IsIdle; };
        }

        public void Dispose()
        {
            App.SiteExtensions.Unload();
        }
    }
}
