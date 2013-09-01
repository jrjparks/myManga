using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using BakaBox.MVVM;
using BakaBox.DLL;
using Manga;
using Manga.Core;
using Manga.Info;
using Manga.Manager;
using Manga.Plugin;
using Manga.Zip;
using myManga.UI;
using myManga.UI.Code;
using myManga.Views;
using System.Collections.ObjectModel;
using BakaBox.Controls.Threading;
using Manga.Archive;
using BakaBox.MVVM.Communications;
using BakaBox;
using System.Threading;

namespace myManga.ViewModels
{
    public sealed class MainViewModel : ViewModelBase
    {
        #region Variables
        public Int32 _ViewIndex { get; set; }
        public Int32 ViewIndex
        {
            get { return _ViewIndex; }
            set
            {
                _ViewIndex = value;
                OnPropertyChanged("ViewIndex");
            }
        }
        private delegate void UpdateViewIndexDelegate(Int32 value);
        private void UpdateViewIndex(Int32 value)
        {
            if (Application.Current.Dispatcher.Thread == Thread.CurrentThread)
                ViewIndex = value;
            else
                Application.Current.Dispatcher.Invoke(new UpdateViewIndexDelegate(UpdateViewIndex), value);
        }
        #endregion

        #region Controls
        public ToastNotification toastNotification { get; set; }
        #endregion

        #region Manga Site Plugins
        private void SetupPlugins()
        {
            if (!IsInDesignerMode)
            {
                try
                { Global_IMangaPluginCollection.Instance.LoadPlugins(Path.Combine(Environment.CurrentDirectory, "Plugins")); }
                catch (Exception ex)
                {
                    MessageBox.Show(String.Format("Unable to load plugins.\nPlease check any new plugins and try again.\n{0}", ex.Message), "Err!", MessageBoxButton.OK, MessageBoxImage.Error);
                    if (Application.Current != null)
                        Application.Current.Shutdown(); // Do NOT shutdown Application in design mode. Shutting down the Application in Visual Studio is BAD!
                }
            }
            else
                SendViewModelToastNotification(this, "Did not load plugins.\nIn design mode.", ToastNotification.DisplayLength.Normal);
        }
        #endregion

        #region Manga Manager
        private void SetupMangaManager()
        { DownloadManager.Instance.TaskFaulted += TaskFaulted; }

        private void TaskFaulted(Object Sender, QueuedTask<ManagerData<String, MangaInfo>> Task)
        { toastNotification.ShowToast(String.Format("Error Downloading: {0}\n{1}\n{2}", Task.Data.Title, Task.Guid.ToString(), Sender.ToString()), ToastNotification.DisplayLength.Long); }
        #endregion

        #region Tabs
        private void ViewModel_Toast(Object Sender, String Message, TimeSpan ShowTime)
        { toastNotification.ShowToast(Message, ShowTime); }

        #region Library
        public LibraryViewModel _LibraryViewModel;
        public LibraryViewModel LibraryViewModel
        {
            get
            {
                if (_LibraryViewModel == null)
                {
                    _LibraryViewModel = new LibraryViewModel();
                    SetupLibrary();
                }
                return _LibraryViewModel;
            }
        }

        private void SetupLibrary()
        {
            if (!IsInDesignerMode)
            {
                LibraryViewModel.ViewModelToastNotification += ViewModel_Toast;
                LibraryViewModel.OpenChapter += (f, r) => OpenMZA(f, r ? ReadingViewModel.OpenPage.Resume : ReadingViewModel.OpenPage.First);
            }
        }
        #endregion

        #region Reading
        public ReadingViewModel _ReadingViewModel;
        public ReadingViewModel ReadingViewModel
        {
            get
            {
                if (_ReadingViewModel == null)
                {
                    _ReadingViewModel = new ReadingViewModel();
                    SetupReading();
                }
                return _ReadingViewModel;
            }
        }

        private String LastLoadedPath { get; set; }

        private void SetupReading()
        {
            ReadingViewModel.ViewModelToastNotification += ViewModel_Toast;
        }
        #endregion

        #region Queue
        private QueueViewModel _QueueViewModel;
        public QueueViewModel QueueViewModel
        {
            get
            {
                if (_QueueViewModel == null)
                {
                    _QueueViewModel = new QueueViewModel();
                    SetupQueueDownloads();
                }
                return _QueueViewModel;
            }
        }

        private void SetupQueueDownloads()
        {
            QueueViewModel.ViewModelToastNotification += ViewModel_Toast;
            QueueViewModel._OpenMZA += (FileName) =>
            {
                String _FileName = String.Format("{0}.mza", FileName),
                        MainPath = MangaDataZip.Instance.MZAPath,
                        TmpPath = ZipNamingExtensions.TempSaveLocation;
                List<String> _PosibleFiles = new List<String>();
                if (Directory.Exists(MainPath))
                    _PosibleFiles.AddRange(Directory.GetFiles(MainPath, _FileName, SearchOption.AllDirectories));
                if (Directory.Exists(TmpPath))
                    _PosibleFiles.AddRange(Directory.GetFiles(TmpPath, _FileName, SearchOption.AllDirectories));

                if (_PosibleFiles.Count > 0)
                    OpenMZA(_PosibleFiles[0], ReadingViewModel.OpenPage.First);
            };
        }
        #endregion

        #region Search
        private SearchViewModel _SearchViewModel;
        public SearchViewModel SearchViewModel
        {
            get
            {
                if (Global_IMangaPluginCollection.Instance.Plugins.Count == 0)
                    SetupPlugins();
                if (_SearchViewModel == null)
                {
                    _SearchViewModel = new SearchViewModel();
                    SetupSearch();
                }
                return _SearchViewModel;
            }
        }

        private void SetupSearch()
        { SearchViewModel.ViewModelToastNotification += ViewModel_Toast; }
        #endregion

        #region Options
        #region About
        public AboutViewModel _AboutViewModel;
        public AboutViewModel AboutViewModel
        {
            get
            {
                if (Global_IMangaPluginCollection.Instance.Plugins.Count == 0)
                    SetupPlugins();
                if (_AboutViewModel == null)
                {
                    _AboutViewModel = new AboutViewModel();
                    SetupAbout();
                }
                return _AboutViewModel;
            }
        }

        private void SetupAbout()
        {
            AboutViewModel.ViewModelToastNotification += ViewModel_Toast;
        }
        #endregion

        #region Settings
        public SettingsViewModel _SettingsViewModel;
        public SettingsViewModel SettingsViewModel
        {
            get
            {
                if (_SettingsViewModel == null)
                {
                    _SettingsViewModel = new SettingsViewModel();
                    SetupSettings();
                }
                return _SettingsViewModel;
            }
        }

        private void SetupSettings()
        {
            SettingsViewModel.ViewModelToastNotification += ViewModel_Toast;
        }
        #endregion

        #region License
        public LicenseViewModel _LicenseViewModel;
        public LicenseViewModel LicenseViewModel
        {
            get
            {
                if (_LicenseViewModel == null)
                {
                    _LicenseViewModel = new LicenseViewModel();
                    SetupLicense();
                }
                return _LicenseViewModel;
            }
        }

        private void SetupLicense()
        {
            LicenseViewModel.ViewModelToastNotification += ViewModel_Toast;
        }
        #endregion
        #endregion
        #endregion

        #region Commands
        private DelegateCommand _OpenFile { get; set; }
        public ICommand OpenFile
        {
            get
            {
                if (_OpenFile == null)
                    _OpenFile = new DelegateCommand(OpenMZA);
                return _OpenFile;
            }
        }

        private DelegateCommand _UpdateLibraryManga { get; set; }
        public ICommand UpdateLibraryManga
        {
            get
            {
                if (_UpdateLibraryManga == null)
                    _UpdateLibraryManga = new DelegateCommand(LibraryViewModel.UpdateLibraryManga);
                return _UpdateLibraryManga;
            }
        }
        #endregion

        public MainViewModel()
        {
            // Add Toast Notification to MainViewModel first. Herp Derp!
            this.ViewModelToastNotification += ViewModel_Toast;

            Messenger.Instance.BroadcastMessage += Instance_BroadcastMessage;
            Application.Current.Exit += Current_Exit;

            toastNotification = new ToastNotification();
            toastNotification.PauseToasts();
            SendViewModelToastNotification(this, "Welcome to myManga\nCheckout myManga.codeplex.com for the latest updates.", ToastNotification.DisplayLength.Long);

            SetupMangaManager();

            if (IsInDesignerMode)
            {
                SendViewModelToastNotification(this, "myManga is running in the designer.", ToastNotification.DisplayLength.Long);
                toastNotification.ResumeToasts();
            }
        }

        void Current_Exit(object sender, ExitEventArgs e)
        {
            DownloadManager.Instance.CancelAll();

            for (Byte i = 0; i < 3; ++i)
            {
                if (Directory.Exists(ZipNamingExtensions.TempSaveLocation))
                {
                    try
                    {
                        Directory.Delete(ZipNamingExtensions.TempSaveLocation, true);
                        break;
                    }
                    catch { Thread.Sleep(5); }
                }
            }
        }

        #region Methods
        private void Instance_BroadcastMessage(object Sender, object Data)
        {
            if (Data == null) { }
            else if (Data is String)
            {
                String sData = Data as String;
                if (sData.StartsWith("!^SetViewIndex"))
                {
                    sData = sData.Substring("!^SetViewIndex".Length);
                    UpdateViewIndex(Parse.TryParse<Int32>(sData, 0));
                }
                else if (sData.Equals("!^ResumeToast"))
                    toastNotification.ResumeToasts();
                else if (sData.StartsWith("#^Manga Licensed|"))
                {
                    String MangaTitle = sData.Substring("#^Manga Licensed|".Length), tmpPath = Path.Combine(MangaDataZip.Instance.MIZAPath, String.Format("{0}.miza", MangaTitle));
                    MangaInfo tmpMI = MangaDataZip.Instance.GetMangaInfo(tmpPath);
                    tmpMI.Licensed = true;
                    LibraryViewModel.UpdateLibraryMangaInfo(null, tmpMI, tmpPath);
                    MangaDataZip.Instance.MIZA(tmpMI);

                    SendViewModelToastNotification(this, String.Format("Unable to download: {0}\nThe manga is licensed, and not available from the current site.", MangaTitle, ToastNotification.DisplayLength.Long));
                }
            }
        }

        private void OpenMZA()
        {
            using (System.Windows.Forms.OpenFileDialog ofd = new System.Windows.Forms.OpenFileDialog())
            {
                ofd.Filter = "Manga Archives|*.mza";
                ofd.InitialDirectory = ZipNamingExtensions.TempSaveLocation;
                if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    OpenMZA(ofd.FileName, ReadingViewModel.OpenPage.First);
            }
        }

        private void OpenMZA(String _Path, ReadingViewModel.OpenPage _OPage)
        { OpenMZA(_Path, _OPage, null); }
        private void OpenMZA(String _Path, ReadingViewModel.OpenPage _OPage, MangaInfo MI)
        {
            if (_Path.EndsWith(".mza"))
            {
                LastLoadedPath = Path.GetDirectoryName(_Path);
                ReadingViewModel.OpenMZA(_Path, _OPage);
                ViewIndex = 1;
            }
            else
                toastNotification.ShowToast(String.Format("Unable to open {0}", Path.GetFileName(_Path)), ToastNotification.DisplayLength.Short);
        }
        #endregion
    }
}
