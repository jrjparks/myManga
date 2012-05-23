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

namespace myManga.ViewModels
{
    public sealed class MainViewModel : ViewModelBase
    {
        #region Variables
        #endregion

        #region Controls
        public ToastNotification toastNotification { get; set; }

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

        #endregion

        #region Manga Site Plugins
        private void SetupPlugins()
        {
            try
            {
                Global_IMangaPluginCollection.Instance.AddPlugins(PluginLoader<IMangaPlugin>.LoadPluginDirectory(
                     System.IO.Path.Combine(Environment.CurrentDirectory, "Plugins"),
                     "*.manga.dll").ToArray());
            }
            catch
            {
                if (!IsInDesignerMode)
                {
                    MessageBox.Show("Unable to load plugins.\nPlease check any new plugins and try again.", "Err!", MessageBoxButton.OK, MessageBoxImage.Error);
                    Application.Current.Shutdown(); // Do NOT shutdown Application in design mode.
                }
                else
                    SendViewModelToastNotification(this, "Did not load plugins.\nIn design mode.", ToastNotification.DisplayLength.Normal);
            }
        }
        #endregion

        #region Manga Manager
        private void SetupMangaManager()
        {
            Manager_v1.Instance.TaskFaulted += TaskFaulted;
        }

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
                    _LibraryViewModel = new LibraryViewModel();
                return _LibraryViewModel;
            }
        }

        private void SetupLibrary()
        {
            if (!IsInDesignerMode)
            {
                LibraryViewModel.InitComplete += () => toastNotification.ResumeToasts();
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
                    _ReadingViewModel = new ReadingViewModel();
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
                    _QueueViewModel = new QueueViewModel();
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
                        TmpPath = Manga.Archive.MangaArchiveData.TmpFolder;
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
                    _SearchViewModel = new SearchViewModel();
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
                    _AboutViewModel = new AboutViewModel();
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
                    _SettingsViewModel = new SettingsViewModel();
                return _SettingsViewModel;
            }
        }

        private void SetupSettings()
        {
            SettingsViewModel.ViewModelToastNotification += ViewModel_Toast;
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
            this.ViewModelToastNotification += ViewModel_Toast;

            toastNotification = new ToastNotification();
            toastNotification.PauseToasts();
            SendViewModelToastNotification(this, "Welcome to myManga", ToastNotification.DisplayLength.Normal);
            
            SetupMangaManager();

            #region Tabs
            SetupLibrary();

            SetupReading();

            SetupQueueDownloads();

            SetupSearch();

            #region Options
            SetupAbout();

            SetupSettings();
            #endregion
            #endregion

            if (IsInDesignerMode)
            {
                SendViewModelToastNotification(this, "myManga is running in the designer.", ToastNotification.DisplayLength.Long);
                toastNotification.ResumeToasts();
            }
        }

        #region Methods
        private void OpenMZA()
        {
            using (System.Windows.Forms.OpenFileDialog ofd = new System.Windows.Forms.OpenFileDialog())
            {
                ofd.Filter = "Manga Archives|*.mza";
                ofd.InitialDirectory = Manga.Archive.MangaArchiveData.TmpFolder;
                if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    OpenMZA(ofd.FileName, ReadingViewModel.OpenPage.First);
                }
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
