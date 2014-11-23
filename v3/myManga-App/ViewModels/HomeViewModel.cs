using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using Amib.Threading;
using Core.IO;
using Core.MVVM;
using Core.Other.Singleton;
using myManga_App.IO;
using myMangaSiteExtension;
using myMangaSiteExtension.Attributes;
using myMangaSiteExtension.Interfaces;
using myMangaSiteExtension.Objects;
using myMangaSiteExtension.Utilities;
using myManga_App.Properties;
using Core.IO.Storage.Manager.BaseInterfaceClasses;

namespace myManga_App.ViewModels
{
    public class HomeViewModel : IDisposable, INotifyPropertyChanging, INotifyPropertyChanged
    {
        #region NotifyPropertyChange
        public event PropertyChangingEventHandler PropertyChanging;
        protected void OnPropertyChanging([CallerMemberName] String caller = "")
        {
            if (PropertyChanging != null)
                PropertyChanging(this, new PropertyChangingEventArgs(caller));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] String caller = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(caller));
        }
        #endregion

        #region MangaList
        protected ObservableCollection<MangaObject> mangaList;
        public ObservableCollection<MangaObject> MangaList
        {
            get { return mangaList ?? (mangaList = new ObservableCollection<MangaObject>()); }
            set
            {
                OnPropertyChanging();
                mangaList = value;
                OnPropertyChanged();
            }
        }

        protected MangaObject mangaObj;
        public MangaObject MangaObj
        {
            get { return mangaObj; }
            set
            {
                OnPropertyChanging();
                mangaObj = value;
                OnPropertyChanged();
            }
        }

        protected String searchFilter;
        public String SearchFilter
        {
            get { return searchFilter; }
            set
            {
                OnPropertyChanging();
                searchFilter = value;
                mangaListView.Refresh();
                mangaListView.MoveCurrentToFirst();
                OnPropertyChanged();
            }
        }

        protected ICollectionView mangaListView;

        protected DelegateCommand clearSearchCommand;
        public ICommand ClearSearchCommand
        {
            get { return clearSearchCommand ?? (clearSearchCommand = new DelegateCommand(ClearSearch, CanClearSearch)); }
        }
        protected void ClearSearch()
        { SearchFilter = String.Empty; }
        protected Boolean CanClearSearch()
        { return !String.IsNullOrWhiteSpace(SearchFilter); }
        #endregion

        #region SearchSites
        public event EventHandler<String> SearchEvent;
        protected void OnSearchEvent(String search_content)
        {
            if (SearchEvent != null)
                SearchEvent(this, search_content);
        }

        protected DelegateCommand searchSitesCommand;
        public ICommand SearchSiteCommand
        { get { return searchSitesCommand ?? (searchSitesCommand = new DelegateCommand(SearchSites, CanSearchSite)); } }

        protected Boolean CanSearchSite()
        { return !String.IsNullOrWhiteSpace(SearchFilter) && (SearchFilter.Trim().Length >= 3); }

        protected void SearchSites()
        {
            OnSearchEvent(SearchFilter.Trim());
        }
        #endregion

        #region DownloadManga
        protected DelegateCommand downloadMangaCommand;
        public ICommand DownloadMangaCommand
        { get { return downloadMangaCommand ?? (downloadMangaCommand = new DelegateCommand(DownloadManga, CanDownloadManga)); } }

        protected Boolean CanDownloadManga()
        { return MangaObj != null; }

        protected void DownloadManga()
        { Singleton<myManga_App.IO.Network.SmartDownloadManager>.Instance.Download(mangaObj); }
        #endregion

        #region DownloadChapter
        protected DelegateCommand<ChapterObject> downloadChapterCommand;
        public ICommand DownloadChapterCommand
        { get { return downloadChapterCommand ?? (downloadChapterCommand = new DelegateCommand<ChapterObject>(DownloadChapter)); } }

        protected void DownloadChapter(ChapterObject ChapterObj)
        { Singleton<myManga_App.IO.Network.SmartDownloadManager>.Instance.Download(ChapterObj); }
        #endregion

        #region RefreshList
        protected DelegateCommand refreshListCommand;
        public ICommand RefreshListCommand
        { get { return refreshListCommand ?? (refreshListCommand = new DelegateCommand(RefreshList, CanRefresh)); } }

        protected Boolean CanRefresh()
        { return MangaList.Count > 0; }

        protected void RefreshList()
        {
            IsLoading = true;
            MangaObject[] MangaObjects = new MangaObject[MangaList.Count];
            MangaList.CopyTo(MangaObjects, 0);
            Singleton<myManga_App.IO.Network.SmartMangaDownloader>.Instance.DownloadMangaObject(MangaObjects.Where(o => o.IsLocal(App.MANGA_ARCHIVE_DIRECTORY, App.MANGA_ARCHIVE_EXTENSION)).ToArray());
            MangaObjects = null;
        }

        private delegate void Instance_MangaObjectCompleteInvoke(object sender, MangaObject e);
        void Instance_MangaObjectComplete(object sender, MangaObject e)
        {
            if (App.Dispatcher.Thread == Thread.CurrentThread)
            {
                IsLoading = !(sender as myManga_App.IO.Network.SmartMangaDownloader).IsIdle;
                if (!MangaList.Any(mo => mo.Name == e.Name)) MangaList.Add(MangaObj);
                else MangaList.First(mo => mo.Name == e.Name).Merge(e);
            }
            else App.Dispatcher.BeginInvoke(new Instance_MangaObjectCompleteInvoke(Instance_MangaObjectComplete), new Object[] { sender, e });
        }
        #endregion

        protected Boolean isLoading;
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

        protected App App = App.Current as App;

        public HomeViewModel()
        {
            ConfigureSearchFilter();
            if (!DesignerProperties.GetIsInDesignMode(new System.Windows.DependencyObject()))
            {
                foreach (String MangaArchiveFilePath in Directory.GetFiles(App.MANGA_ARCHIVE_DIRECTORY, App.MANGA_ARCHIVE_FILTER, SearchOption.AllDirectories))
                {
                    using (Stream archive_manga = Singleton<Core.IO.Storage.Manager.BaseInterfaceClasses.ZipStorage>.Instance.Read(MangaArchiveFilePath, typeof(MangaObject).Name))
                    {
                        try
                        { MangaList.Add(archive_manga.Deserialize<MangaObject>(SaveType: Settings.Default.SaveType)); }
                        catch { }
                    }
                }
                App.MangaObjectArchiveWatcher.Changed += MangaObjectArchiveWatcher_Event;
                App.MangaObjectArchiveWatcher.Created += MangaObjectArchiveWatcher_Event;
                App.MangaObjectArchiveWatcher.Deleted += MangaObjectArchiveWatcher_Event;
                mangaListView.MoveCurrentToFirst();
            }
#if DEBUG
#endif
        }

        void MangaObjectArchiveWatcher_Event(object sender, FileSystemEventArgs e)
        {
            if (App.Dispatcher.Thread == Thread.CurrentThread)
            {
                MangaObject current_manga_object = MangaList.FirstOrDefault(o => o.MangaArchiveName(App.MANGA_ARCHIVE_EXTENSION) == e.Name);
                switch (e.ChangeType)
                {
                    default:
                        break;

                    case WatcherChangeTypes.Changed:
                    case WatcherChangeTypes.Created:
                        using (Stream archive_file = Singleton<ZipStorage>.Instance.Read(e.FullPath, typeof(MangaObject).Name))
                        {
                            if (archive_file.CanRead && archive_file.Length > 0)
                            {
                                MangaObject new_manga_object = archive_file.Deserialize<MangaObject>(SaveType: Settings.Default.SaveType);
                                if (current_manga_object != null)
                                    current_manga_object.Merge(new_manga_object);
                                else
                                    MangaList.Add(new_manga_object);
                            }
                        }
                        break;

                    case WatcherChangeTypes.Deleted:
                        MangaList.Remove(current_manga_object);
                        break;
                }
            }
            else
                App.Dispatcher.Invoke(DispatcherPriority.Send, new System.Action(() => MangaObjectArchiveWatcher_Event(sender, e)));
        }

        protected void ConfigureSearchFilter()
        {
            mangaListView = CollectionViewSource.GetDefaultView(MangaList);
            mangaListView.Filter = mangaObject => String.IsNullOrWhiteSpace(SearchFilter) ? true : (mangaObject as MangaObject).IsNameMatch(SearchFilter);
            if (mangaListView.CanSort)
                mangaListView.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
        }

        public void Dispose() { }
    }
}
