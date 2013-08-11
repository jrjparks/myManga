using System;
using myManga.Properties;
using System.Windows;


namespace myManga.ViewModels
{
    public sealed class SettingsViewModel : ViewModelBase
    {
        #region Window
        public Boolean ChangeLastWindowState
        {
            get { return Settings.Default.ChangeLastWindowState; }
            set
            {
                OnPropertyChanging("ChangeLastWindowState");
                Settings.Default.ChangeLastWindowState = value;
                OnPropertyChanged("ChangeLastWindowState");
                Settings.Default.Save();
            }
        }
        #endregion

        #region Display
        public Boolean DrawShadows
        {
            get { return Settings.Default.DrawShadows; }
            set
            {
                OnPropertyChanging("DrawShadows");
                Settings.Default.DrawShadows = value;
                OnPropertyChanged("DrawShadows");
                Settings.Default.Save();
            }
        }

        public Boolean FadeImages
        {
            get { return Settings.Default.FadeImages; }
            set
            {
                OnPropertyChanging("FadeImages");
                Settings.Default.FadeImages = value;
                OnPropertyChanged("FadeImages");
                Settings.Default.Save();
            }
        }

        public Base.ChapterOrder ChapterListOrder
        {
            get { return Settings.Default.ChapterListOrder; }
            set
            {
                OnPropertyChanging("ChapterListOrder");
                Settings.Default.ChapterListOrder = value;
                OnPropertyChanged("ChapterListOrder");
                Settings.Default.Save();
            }
        }
        #endregion

        #region Reader
        public Boolean AutoDownload
        {
            get { return Settings.Default.AutoDownload; }
            set
            {
                OnPropertyChanging("AutoDownload");
                Settings.Default.AutoDownload = value;
                OnPropertyChanged("AutoDownload");
                Settings.Default.Save();
            }
        }

        public Boolean AutoClean
        {
            get { return Settings.Default.AutoClean; }
            set
            {
                OnPropertyChanging("AutoClean");
                Settings.Default.AutoClean = value;
                OnPropertyChanged("AutoClean");
                Settings.Default.Save();
            }
        }
        #endregion
    }
}
