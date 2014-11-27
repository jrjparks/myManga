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
    public class HomeViewModel : DependencyObject, IDisposable, INotifyPropertyChanging, INotifyPropertyChanged
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
                LoadBookmarkObject();
            }
        }

        protected ChapterObject _SelectedChapter;
        public ChapterObject SelectedChapter
        {
            get { return _SelectedChapter; }
            set
            {
                OnPropertyChanging();
                _SelectedChapter = value;
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

        #region DownloadChapter
        protected DelegateCommand<ChapterObject> downloadChapterCommand;
        public ICommand DownloadChapterCommand
        { get { return downloadChapterCommand ?? (downloadChapterCommand = new DelegateCommand<ChapterObject>(DownloadChapter)); } }

        protected void DownloadChapter(ChapterObject ChapterObj)
        { Singleton<myManga_App.IO.Network.DownloadManager>.Instance.Download(MangaObj, ChapterObj); }
        #endregion

        #region ReadChapter
        public delegate void ReadChapterDelegate(Object sender, MangaObject MangaObject, ChapterObject ChapterObject);
        public event ReadChapterDelegate ReadChapterEvent;
        protected void OnReadChapterEvent(MangaObject MangaObject, ChapterObject ChapterObject)
        {
            if (ReadChapterEvent != null)
                ReadChapterEvent(this, MangaObject, ChapterObject);
        }

        protected DelegateCommand<ChapterObject> readChapterCommand;
        public ICommand ReadChapterCommand
        { get { return readChapterCommand ?? (readChapterCommand = new DelegateCommand<ChapterObject>(ReadChapter)); } }

        protected void ReadChapter(ChapterObject ChapterObj)
        { OnReadChapterEvent(this.MangaObj, ChapterObj); }

        protected DelegateCommand resumeReadingCommand;
        public ICommand ResumeReadingCommand
        { get { return resumeReadingCommand ?? (resumeReadingCommand = new DelegateCommand(ResumeReading, CanResumeReading)); } }

        protected void ResumeReading()
        { OnReadChapterEvent(this.MangaObj, this.SelectedChapter); }
        protected Boolean CanResumeReading()
        { return this.MangaObj != null && this.SelectedChapter != null; }
        #endregion

        #region RefreshManga
        protected DelegateCommand refreshMangaCommand;
        public ICommand RefreshMangaCommand
        { get { return refreshMangaCommand ?? (refreshMangaCommand = new DelegateCommand(RefreshManga, CanRefreshManga)); } }

        protected Boolean CanRefreshManga()
        { return MangaObj != null; }

        protected void RefreshManga()
        { Singleton<myManga_App.IO.Network.DownloadManager>.Instance.Download(mangaObj); }
        #endregion

        #region RefreshMangaList
        protected DelegateCommand refreshMangaListCommand;
        public ICommand RefreshMangaListCommand
        { get { return refreshMangaListCommand ?? (refreshMangaListCommand = new DelegateCommand(RefreshMangaList, CanRefreshMangaList)); } }

        protected Boolean CanRefreshMangaList()
        { return MangaList.Count > 0; }

        protected void RefreshMangaList()
        { foreach (MangaObject manga_object in MangaList) Singleton<myManga_App.IO.Network.DownloadManager>.Instance.Download(manga_object); }
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

        protected readonly App App = App.Current as App;

        public HomeViewModel()
        {
            ConfigureSearchFilter();
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                foreach (String MangaArchiveFilePath in Directory.GetFiles(App.MANGA_ARCHIVE_DIRECTORY, App.MANGA_ARCHIVE_FILTER, SearchOption.AllDirectories))
                {
                    Stream archive_file;
                    if (Singleton<ZipStorage>.Instance.TryRead(MangaArchiveFilePath, out archive_file, typeof(MangaObject).Name))
                    {
                        if (archive_file.CanRead && archive_file.Length > 0)
                        {
                            MangaList.Add(archive_file.Deserialize<MangaObject>(SaveType: App.UserConfig.SaveType));
                        }
                        archive_file.Close();
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
                        Stream archive_file;
                        if (Singleton<ZipStorage>.Instance.TryRead(e.FullPath, out archive_file, typeof(MangaObject).Name))
                        {
                            MangaObject new_manga_object = archive_file.Deserialize<MangaObject>(SaveType: App.UserConfig.SaveType);
                            if (current_manga_object != null)
                                current_manga_object.Merge(new_manga_object);
                            else
                                MangaList.Add(new_manga_object);
                            archive_file.Close();
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

        private void LoadBookmarkObject()
        {
            Stream bookmark_file;
            BookmarkObject BookmarkObject = null;
            if (Singleton<ZipStorage>.Instance.TryRead(Path.Combine(App.MANGA_ARCHIVE_DIRECTORY, this.MangaObj.MangaArchiveName(App.MANGA_ARCHIVE_EXTENSION)), out bookmark_file, typeof(BookmarkObject).Name))
            { using (bookmark_file) BookmarkObject = bookmark_file.Deserialize<BookmarkObject>(SaveType: App.UserConfig.SaveType); }
            if (BookmarkObject != null){ this.SelectedChapter = this.MangaObj.ChapterObjectOfBookmarkObject(BookmarkObject); }
        }

        public void Dispose() { }
    }
}
