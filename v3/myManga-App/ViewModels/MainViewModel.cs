using System;
using System.ComponentModel;
using System.Net;
using System.Windows;
using myManga_App.IO.Network;
using Core.Other.Singleton;
using Core.MVVM;
using System.Windows.Input;

namespace myManga_App.ViewModels
{
    public sealed class MainViewModel : DependencyObject, IDisposable
    {
        #region Content
        public static DependencyProperty ContentViewModelProperty = DependencyProperty.Register("ContentViewModel", typeof(Object), typeof(MainViewModel));
        public Object ContentViewModel
        {
            get
            {
                return GetValue(ContentViewModelProperty) as Object;
            }
            set
            {
                SetValue(ContentViewModelProperty, value);
            }
        }

        private HomeViewModel homeViewModel;
        public HomeViewModel HomeViewModel
        { get { return homeViewModel ?? (homeViewModel = new HomeViewModel()); } }

        private SettingsViewModel settingsViewModel;
        public SettingsViewModel SettingsViewModel
        { get { return settingsViewModel ?? (settingsViewModel = new SettingsViewModel()); } }
        #endregion

        #region Settings
        private DelegateCommand settingsCommand;
        public ICommand SettingsCommand
        { get { return settingsCommand ?? (settingsCommand = new DelegateCommand(OpenSettings)); } }

        private void OpenSettings()
        { ContentViewModel = SettingsViewModel; }
        #endregion

        private App App = App.Current as App;

        public MainViewModel()
        {
            ContentViewModel = HomeViewModel;
            SettingsViewModel.CloseEvent += (s, e) => ContentViewModel = HomeViewModel;

            ServicePointManager.DefaultConnectionLimit =
                Singleton<SmartMangaDownloader>.Instance.Concurrency +
                Singleton<SmartChapterDownloader>.Instance.Concurrency +
                Singleton<SmartSearch>.Instance.Concurrency;
        }

        public void Dispose() { App.SiteExtensions.Unload(); }
    }
}
