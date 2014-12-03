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
using myManga_App.IO.ViewModel;
using myManga_App.Objects;

namespace myManga_App.ViewModels
{
    public class HomeViewModel : BaseViewModel
    {
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
        protected DelegateCommand searchSitesCommand;
        public ICommand SearchSiteCommand
        { get { return searchSitesCommand ?? (searchSitesCommand = new DelegateCommand(SearchSites, CanSearchSite)); } }

        protected Boolean CanSearchSite()
        { return !String.IsNullOrWhiteSpace(SearchFilter) && (SearchFilter.Trim().Length >= 3); }

        protected void SearchSites()
        { Messenger.Default.Send(SearchFilter.Trim(), "SearchRequest"); }
        #endregion

        #region DownloadChapter
        protected DelegateCommand<ChapterObject> downloadChapterCommand;
        public ICommand DownloadChapterCommand
        { get { return downloadChapterCommand ?? (downloadChapterCommand = new DelegateCommand<ChapterObject>(DownloadChapter)); } }

        protected void DownloadChapter(ChapterObject ChapterObj)
        { Singleton<myManga_App.IO.Network.DownloadManager>.Instance.Download(MangaObj, ChapterObj); }
        #endregion

        #region ReadChapter
        protected DelegateCommand<ChapterObject> readChapterCommand;
        public ICommand ReadChapterCommand
        { get { return readChapterCommand ?? (readChapterCommand = new DelegateCommand<ChapterObject>(ReadChapter)); } }

        protected void ReadChapter(ChapterObject ChapterObj)
        { Messenger.Default.Send(new ReadChapterRequestObject(this.MangaObj, ChapterObj), "ReadChapterRequest"); }

        protected DelegateCommand resumeReadingCommand;
        public ICommand ResumeReadingCommand
        { get { return resumeReadingCommand ?? (resumeReadingCommand = new DelegateCommand(ResumeReading, CanResumeReading)); } }

        protected void ResumeReading()
        { Messenger.Default.Send(new ReadChapterRequestObject(this.MangaObj, this.SelectedChapter), "ReadChapterRequest"); }
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
                        try
                        {
                            if (archive_file.CanRead && archive_file.Length > 0)
                            { MangaList.Add(archive_file.Deserialize<MangaObject>(SaveType: App.UserConfig.SaveType)); }
                        }
                        catch { }
                        archive_file.Close();
                    }
                }
                mangaListView.MoveCurrentToFirst();

                Messenger.Default.RegisterRecipient<FileSystemEventArgs>(this, MangaObjectArchiveWatcher_Event, "MangaObjectArchive");
            }
#if DEBUG
#endif
        }

        void MangaObjectArchiveWatcher_Event(FileSystemEventArgs e)
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
    }
}
