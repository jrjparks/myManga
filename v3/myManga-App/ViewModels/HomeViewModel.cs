using Core.IO;
using Core.IO.Storage.Manager.BaseInterfaceClasses;
using Core.MVVM;
using Core.Other.Singleton;
using myManga_App.IO.Local;
using myManga_App.IO.Network;
using myManga_App.Objects;
using myManga_App.Objects.Cache;
using myManga_App.Objects.UserInterface;
using myMangaSiteExtension.Objects;
using myMangaSiteExtension.Utilities;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace myManga_App.ViewModels
{
    public sealed class HomeViewModel : BaseViewModel
    {
        #region SelectedMangaArchive
        private static readonly DependencyProperty SelectedMangaArchiveProperty = DependencyProperty.RegisterAttached(
            "SelectedMangaArchive",
            typeof(MangaArchiveCacheObject),
            typeof(HomeViewModel));
        public MangaArchiveCacheObject SelectedMangaArchive
        {
            get { return GetValue(SelectedMangaArchiveProperty) as MangaArchiveCacheObject; }
            set { SetValue(SelectedMangaArchiveProperty, value); }
        }

        private ICollectionView MangaListView;
        #endregion

        #region Search Filter
        private static readonly DependencyProperty SearchFilterProperty = DependencyProperty.RegisterAttached(
            "SearchFilter",
            typeof(String),
            typeof(HomeViewModel),
            new PropertyMetadata(OnSearchFilterChanged));
        public String SearchFilter
        {
            get { return (String)GetValue(SearchFilterProperty); }
            set { SetValue(SearchFilterProperty, value); }
        }

        private static void OnSearchFilterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            HomeViewModel _this = (d as HomeViewModel);
            _this.MangaListView.Refresh();
            _this.MangaListView.MoveCurrentToFirst();
        }

        private DelegateCommand _ClearSearchCommand;
        public ICommand ClearSearchCommand
        { get { return _ClearSearchCommand ?? (_ClearSearchCommand = new DelegateCommand(ClearSearch, CanClearSearch)); } }
        private void ClearSearch()
        { SearchFilter = String.Empty; }
        private Boolean CanClearSearch()
        { return !String.IsNullOrWhiteSpace(SearchFilter); }
        #endregion

        #region SearchSites
        private DelegateCommand _SearchSiteCommand;
        public ICommand SearchSiteCommand
        { get { return _SearchSiteCommand ?? (_SearchSiteCommand = new DelegateCommand(SearchSites, CanSearchSite)); } }

        private Boolean CanSearchSite()
        { return !String.IsNullOrWhiteSpace(SearchFilter) && (SearchFilter.Trim().Length >= 3); }

        private void SearchSites()
        { Messenger.Default.Send(SearchFilter.Trim(), "SearchRequest"); }
        #endregion

        #region DownloadChapter
        private DelegateCommand<ChapterObject> _DownloadChapterCommand;
        public ICommand DownloadChapterCommand
        { get { return _DownloadChapterCommand ?? (_DownloadChapterCommand = new DelegateCommand<ChapterObject>(DownloadChapter)); } }

        private void DownloadChapter(ChapterObject ChapterObj)
        { App.DownloadManager.Download(SelectedMangaArchive.MangaObject, ChapterObj); }
        #endregion

        #region ReadChapter
        private DelegateCommand<ChapterObject> _ReadChapterCommand;
        public ICommand ReadChapterCommand
        { get { return _ReadChapterCommand ?? (_ReadChapterCommand = new DelegateCommand<ChapterObject>(ReadChapter)); } }

        private void ReadChapter(ChapterObject ChapterObj)
        {
            String bookmark_chapter_path = Path.Combine(App.CHAPTER_ARCHIVE_DIRECTORY, this.SelectedMangaArchive.MangaObject.MangaFileName());
            MangaObject SelectedMangaObject = this.SelectedMangaArchive.MangaObject;
            if (ChapterObj.IsLocal(bookmark_chapter_path, App.CHAPTER_ARCHIVE_EXTENSION))
                Messenger.Default.Send(new ReadChapterRequestObject(this.SelectedMangaArchive.MangaObject, ChapterObj), "ReadChapterRequest");
            else
                App.DownloadManager.Download(SelectedMangaObject, ChapterObj);
        }

        private DelegateCommand _ResumeReadingCommand;
        public ICommand ResumeReadingCommand
        { get { return _ResumeReadingCommand ?? (_ResumeReadingCommand = new DelegateCommand(ResumeReading, CanResumeReading)); } }

        private void ResumeReading()
        {
            String bookmark_chapter_path = Path.Combine(App.CHAPTER_ARCHIVE_DIRECTORY, this.SelectedMangaArchive.MangaObject.MangaFileName());
            MangaObject SelectedMangaObject = this.SelectedMangaArchive.MangaObject;
            ChapterObject ResumeChapterObject = (this.SelectedMangaArchive.BookmarkObject != null) ?
                SelectedMangaObject.ChapterObjectOfBookmarkObject(this.SelectedMangaArchive.BookmarkObject) : 
                SelectedMangaObject.Chapters.FirstOrDefault();
            BookmarkObject SelectedBookmarkObject = this.SelectedMangaArchive.BookmarkObject ?? new myMangaSiteExtension.Objects.BookmarkObject()
            {
                Volume = ResumeChapterObject.Volume,
                Chapter = ResumeChapterObject.Chapter,
                SubChapter = ResumeChapterObject.SubChapter,
                Page = 1,
            };
            if (ResumeChapterObject.IsLocal(bookmark_chapter_path, App.CHAPTER_ARCHIVE_EXTENSION))
                Messenger.Default.Send(new ReadChapterRequestObject(SelectedMangaObject, ResumeChapterObject), "ReadChapterRequest");
            else
            {
                App.ZipStorage.Write(
                    Path.Combine(App.MANGA_ARCHIVE_DIRECTORY, SelectedMangaObject.MangaArchiveName(App.MANGA_ARCHIVE_EXTENSION)),
                    typeof(BookmarkObject).Name,
                    SelectedBookmarkObject.Serialize(SaveType: App.UserConfig.SaveType)
                );
                App.DownloadManager.Download(SelectedMangaObject, ResumeChapterObject);
            }
        }
        private Boolean CanResumeReading()
        { return !MangaArchiveInformationObject.Equals(this.SelectedMangaArchive, null) && !this.SelectedMangaArchive.Empty(); }
        #endregion

        #region RefreshManga
        private DelegateCommand _RefreshMangaCommand;
        public ICommand RefreshMangaCommand
        { get { return _RefreshMangaCommand ?? (_RefreshMangaCommand = new DelegateCommand(RefreshManga, CanRefreshManga)); } }

        private Boolean CanRefreshManga()
        { return !MangaArchiveInformationObject.Equals(this.SelectedMangaArchive, null) && !this.SelectedMangaArchive.Empty(); }

        private void RefreshManga()
        { App.DownloadManager.Download(SelectedMangaArchive.MangaObject, new Core.IO.KeyValuePair<String, Object>("IsRefresh", true)); }
        #endregion

        #region RefreshMangaList
        private DelegateCommand _RefreshMangaListCommand;
        public ICommand RefreshMangaListCommand
        { get { return _RefreshMangaListCommand ?? (_RefreshMangaListCommand = new DelegateCommand(RefreshMangaList, CanRefreshMangaList)); } }

        private Boolean CanRefreshMangaList()
        { return App.MangaArchiveCacheCollection.Count > 0; }

        private void RefreshMangaList()
        { foreach (MangaArchiveCacheObject manga_archive in App.MangaArchiveCacheCollection)
            App.DownloadManager.Download(manga_archive.MangaObject, new Core.IO.KeyValuePair<String, Object>("IsRefresh", true));
        }
        #endregion

        #region DeleteManga
        private DelegateCommand _DeleteMangaCommand;
        public ICommand DeleteMangaCommand
        { get { return _DeleteMangaCommand ?? (_DeleteMangaCommand = new DelegateCommand(DeleteManga, CanDeleteManga)); } }

        private Boolean CanDeleteManga()
        { return !MangaArchiveInformationObject.Equals(this.SelectedMangaArchive, null) && !this.SelectedMangaArchive.Empty(); }

        private void DeleteManga()
        {
            String save_path = Path.Combine(App.MANGA_ARCHIVE_DIRECTORY, this.SelectedMangaArchive.MangaObject.MangaArchiveName(App.MANGA_ARCHIVE_EXTENSION));
            MessageBoxResult msgbox_result = MessageBox.Show(String.Format("Are you sure you wish to delete \"{0}\"?", this.SelectedMangaArchive.MangaObject.Name), "Delete Manga?", MessageBoxButton.YesNo);
            if (msgbox_result.Equals(MessageBoxResult.Yes))
                File.Delete(save_path);
        }
        #endregion

        private Boolean _IsLoading;
        public Boolean IsLoading
        {
            get { return _IsLoading; }
            set { SetProperty(ref this._IsLoading, value); }
        }

        public HomeViewModel()
            : base(SupportsViewTypeChange: true)
        {
            if (!IsInDesignMode)
            {
                foreach (String MangaArchiveFilePath in Directory.GetFiles(App.MANGA_ARCHIVE_DIRECTORY, App.MANGA_ARCHIVE_FILTER, SearchOption.AllDirectories))
                {
                    VerifyArchiveFile.VerifyArchive(App.ZipStorage, MangaArchiveFilePath);
                    CacheMangaObject(MangaArchiveFilePath);
                }
                ConfigureSearchFilter();
                MangaListView.MoveCurrentToFirst();
                this.SelectedMangaArchive = App.MangaArchiveCacheCollection.FirstOrDefault();

                Messenger.Default.RegisterRecipient<FileSystemEventArgs>(this, MangaObjectArchiveWatcher_Event, "MangaObjectArchiveWatcher");
                Messenger.Default.RegisterRecipient<FileSystemEventArgs>(this, ChapterObjectArchiveWatcher_Event, "ChapterObjectArchiveWatcher");
            }
#if DEBUG
#endif
        }

        private void MangaObjectArchiveWatcher_Event(FileSystemEventArgs e)
        {
            // Lookup by filename
            MangaArchiveCacheObject current_manga_archive = App.MangaArchiveCacheCollection.FirstOrDefault(
                maco => maco.MangaObject.MangaArchiveName(App.MANGA_ARCHIVE_EXTENSION).Equals(e.Name));

            Boolean ViewingSelectedMangaObject = this.SelectedMangaArchive != null && this.SelectedMangaArchive.Equals(current_manga_archive);
            switch (e.ChangeType)
            {
                case WatcherChangeTypes.Changed:
                case WatcherChangeTypes.Created:
                    // (Re)Cache if creaded or changed
                    current_manga_archive = CacheMangaObject(e.FullPath);
                    if (!current_manga_archive.Empty() && ViewingSelectedMangaObject)
                    { this.SelectedMangaArchive = current_manga_archive; }
                    break;

                case WatcherChangeTypes.Deleted:
                    // Reselect nearest neighbor after delete
                    Int32 index = App.MangaArchiveCacheCollection.IndexOf(current_manga_archive);
                    App.MangaArchiveCacheCollection.Remove(current_manga_archive);
                    // If delete was the last item subtract from index
                    if (index >= App.MangaArchiveCacheCollection.Count) --index;
                    this.SelectedMangaArchive = App.MangaArchiveCacheCollection[index];
                    break;

                default:
                    break;
            }
        }

        private void ChapterObjectArchiveWatcher_Event(FileSystemEventArgs e)
        {
            FileInfo fileInfo = new FileInfo(e.FullPath);
            MangaArchiveCacheObject current_manga_archive = App.MangaArchiveCacheCollection.FirstOrDefault(o =>
            { return String.Equals(fileInfo.Directory.FullName, Path.Combine(App.CHAPTER_ARCHIVE_DIRECTORY, o.MangaObject.MangaFileName())); });
            switch (e.ChangeType)
            {
                case WatcherChangeTypes.Created:
                case WatcherChangeTypes.Deleted:
                    if (!MangaArchiveInformationObject.Equals(current_manga_archive, null))
                        current_manga_archive.LastUpdate = fileInfo.LastAccessTime;
                    break;
            }
        }

        private MangaArchiveCacheObject CacheMangaObject(String ArchivePath)
        {
            Stream archive_file;
            MangaArchiveCacheObject manga_archive_cache_object = new MangaArchiveCacheObject();
            if (App.ZipStorage.TryRead(ArchivePath, out archive_file, typeof(MangaObject).Name))
            {
                try
                {
                    if (archive_file.CanRead && archive_file.Length > 0)
                    { manga_archive_cache_object.MangaObject = archive_file.Deserialize<MangaObject>(SaveType: App.UserConfig.SaveType); }
                }
                catch { }
                archive_file.Close();
            }
            if (App.ZipStorage.TryRead(ArchivePath, out archive_file, typeof(BookmarkObject).Name))
            {
                try
                {
                    if (archive_file.CanRead && archive_file.Length > 0)
                    { manga_archive_cache_object.BookmarkObject = archive_file.Deserialize<BookmarkObject>(SaveType: App.UserConfig.SaveType); }
                }
                catch { }
                archive_file.Close();
            }
            if (!manga_archive_cache_object.Empty())
            {
                MangaArchiveCacheObject existing_manga_archive_cache_object = App.MangaArchiveCacheCollection.FirstOrDefault(maco => maco.MangaObject.Name == manga_archive_cache_object.MangaObject.Name);
                if (existing_manga_archive_cache_object == null)
                    App.MangaArchiveCacheCollection.Add(manga_archive_cache_object);
                else
                    existing_manga_archive_cache_object.Merge(manga_archive_cache_object);
            }
            return manga_archive_cache_object;
        }

        private void ConfigureSearchFilter()
        {
            this.MangaListView = CollectionViewSource.GetDefaultView(App.MangaArchiveCacheCollection);
            MangaListView.Filter = mangaArchive =>
            {
                // Show all items if search is empty
                if (String.IsNullOrWhiteSpace(SearchFilter)) return true;
                return (mangaArchive as MangaArchiveCacheObject).MangaObject.IsNameMatch(SearchFilter);
            };
            if (MangaListView.CanGroup)
            {
                MangaListView.GroupDescriptions.Clear();
                MangaListView.GroupDescriptions.Add(new PropertyGroupDescription("HasMoreToRead"));
            }
            if (MangaListView.CanSort)
            {
                MangaListView.SortDescriptions.Clear();
                MangaListView.SortDescriptions.Add(new SortDescription("HasMoreToRead", ListSortDirection.Descending));
                MangaListView.SortDescriptions.Add(new SortDescription("MangaObject.Name", ListSortDirection.Ascending));
            }
        }
    }
}
