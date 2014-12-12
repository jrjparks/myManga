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
using myManga_App.IO.Network;
using myManga_App.Objects.MVVM;

namespace myManga_App.ViewModels
{
    public sealed class HomeViewModel : BaseViewModel
    {
        #region MangaList
        private ObservableCollection<MangaObject> mangaObjectItems;
        public ObservableCollection<MangaObject> MangaObjectItems
        {
            get { return mangaObjectItems ?? (mangaObjectItems = new ObservableCollection<MangaObject>()); }
            set
            {
                OnPropertyChanging();
                mangaObjectItems = value;
                OnPropertyChanged();
            }
        }

        private MangaObject selectedMangaObject;
        public MangaObject SelectedMangaObject
        {
            get { return selectedMangaObject; }
            set
            {
                OnPropertyChanging();
                selectedMangaObject = value ?? MangaObjectItems.FirstOrDefault();
                OnPropertyChanged();
                LoadBookmarkObject();
            }
        }

        private BookmarkObject _BookmarkObject;
        public BookmarkObject BookmarkObject
        {
            get { return _BookmarkObject; }
            set
            {
                OnPropertyChanging();
                _BookmarkObject = value;
                OnPropertyChanged();
            }
        }

        private ChapterObject _SelectedChapter;
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

        private String searchFilter;
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

        private ICollectionView mangaListView;

        private DelegateCommand clearSearchCommand;
        public ICommand ClearSearchCommand
        {
            get { return clearSearchCommand ?? (clearSearchCommand = new DelegateCommand(ClearSearch, CanClearSearch)); }
        }
        private void ClearSearch()
        { SearchFilter = String.Empty; }
        private Boolean CanClearSearch()
        { return !String.IsNullOrWhiteSpace(SearchFilter); }
        #endregion

        #region SearchSites
        private DelegateCommand searchSitesCommand;
        public ICommand SearchSiteCommand
        { get { return searchSitesCommand ?? (searchSitesCommand = new DelegateCommand(SearchSites, CanSearchSite)); } }

        private Boolean CanSearchSite()
        { return !String.IsNullOrWhiteSpace(SearchFilter) && (SearchFilter.Trim().Length >= 3); }

        private void SearchSites()
        { Messenger.Default.Send(SearchFilter.Trim(), "SearchRequest"); }
        #endregion

        #region DownloadChapter
        private DelegateCommand<ChapterObject> downloadChapterCommand;
        public ICommand DownloadChapterCommand
        { get { return downloadChapterCommand ?? (downloadChapterCommand = new DelegateCommand<ChapterObject>(DownloadChapter)); } }

        private void DownloadChapter(ChapterObject ChapterObj)
        { DownloadManager.Default.Download(SelectedMangaObject, ChapterObj); }
        #endregion

        #region ReadChapter
        private DelegateCommand<ChapterObject> readChapterCommand;
        public ICommand ReadChapterCommand
        { get { return readChapterCommand ?? (readChapterCommand = new DelegateCommand<ChapterObject>(ReadChapter)); } }

        private void ReadChapter(ChapterObject ChapterObj)
        { Messenger.Default.Send(new ReadChapterRequestObject(this.SelectedMangaObject, ChapterObj), "ReadChapterRequest"); }

        private DelegateCommand resumeReadingCommand;
        public ICommand ResumeReadingCommand
        { get { return resumeReadingCommand ?? (resumeReadingCommand = new DelegateCommand(ResumeReading, CanResumeReading)); } }

        private void ResumeReading()
        {
            String bookmark_chapter_path = Path.Combine(App.CHAPTER_ARCHIVE_DIRECTORY, this.SelectedMangaObject.MangaFileName());
            MangaObject SelectedMangaObject = this.SelectedMangaObject;
            ChapterObject ResumeChapterObject = (this.BookmarkObject != null) ? 
                SelectedMangaObject.ChapterObjectOfBookmarkObject(this.BookmarkObject) : 
                SelectedMangaObject.Chapters.FirstOrDefault();
            BookmarkObject SelectedBookmarkObject = this.BookmarkObject ?? new myMangaSiteExtension.Objects.BookmarkObject()
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
                Singleton<ZipStorage>.Instance.Write(
                    Path.Combine(App.MANGA_ARCHIVE_DIRECTORY, SelectedMangaObject.MangaArchiveName(App.MANGA_ARCHIVE_EXTENSION)),
                    typeof(BookmarkObject).Name,
                    SelectedBookmarkObject.Serialize(SaveType: App.UserConfig.SaveType)
                );
                DownloadManager.Default.Download(SelectedMangaObject, ResumeChapterObject);
            }
        }
        private Boolean CanResumeReading()
        { return this.SelectedMangaObject != null; }
        #endregion

        #region RefreshManga
        private DelegateCommand refreshMangaCommand;
        public ICommand RefreshMangaCommand
        { get { return refreshMangaCommand ?? (refreshMangaCommand = new DelegateCommand(RefreshManga, CanRefreshManga)); } }

        private Boolean CanRefreshManga()
        { return SelectedMangaObject != null; }

        private void RefreshManga()
        { DownloadManager.Default.Download(selectedMangaObject); }
        #endregion

        #region RefreshMangaList
        private DelegateCommand refreshMangaListCommand;
        public ICommand RefreshMangaListCommand
        { get { return refreshMangaListCommand ?? (refreshMangaListCommand = new DelegateCommand(RefreshMangaList, CanRefreshMangaList)); } }

        private Boolean CanRefreshMangaList()
        { return MangaObjectItems.Count > 0; }

        private void RefreshMangaList()
        { foreach (MangaObject manga_object in MangaObjectItems) DownloadManager.Default.Download(manga_object); }
        #endregion

        private Boolean isLoading;
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
            : base(SupportsViewTypeChange:true)
        {
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                ConfigureSearchFilter();
                foreach (String MangaArchiveFilePath in Directory.GetFiles(App.MANGA_ARCHIVE_DIRECTORY, App.MANGA_ARCHIVE_FILTER, SearchOption.AllDirectories))
                {
                    Stream archive_file;
                    if (Singleton<ZipStorage>.Instance.TryRead(MangaArchiveFilePath, out archive_file, typeof(MangaObject).Name))
                    {
                        try
                        {
                            if (archive_file.CanRead && archive_file.Length > 0)
                            { MangaObjectItems.Add(archive_file.Deserialize<MangaObject>(SaveType: App.UserConfig.SaveType)); }
                        }
                        catch { }
                        archive_file.Close();
                    }
                }
                this.SelectedMangaObject = this.MangaObjectItems.FirstOrDefault();
                mangaListView.MoveCurrentToFirst();

                Messenger.Default.RegisterRecipient<FileSystemEventArgs>(this, MangaObjectArchiveWatcher_Event, "MangaObjectArchiveWatcher");
            }
#if DEBUG
#endif
        }

        void MangaObjectArchiveWatcher_Event(FileSystemEventArgs e)
        {
            MangaObject current_manga_object = MangaObjectItems.FirstOrDefault(o => o.MangaArchiveName(App.MANGA_ARCHIVE_EXTENSION) == e.Name);
            switch (e.ChangeType)
            {
                default:
                    break;

                case WatcherChangeTypes.Changed:
                case WatcherChangeTypes.Created:
                    Stream archive_file;
                    if (Singleton<ZipStorage>.Instance.TryRead(e.FullPath, out archive_file, typeof(MangaObject).Name))
                    {
                        Boolean ViewingSelectedMangaObject = this.SelectedMangaObject != null && this.SelectedMangaObject.Name.Equals(current_manga_object.Name);
                        MangaObject new_manga_object = archive_file.Deserialize<MangaObject>(SaveType: App.UserConfig.SaveType);
                        if (current_manga_object != null)
                            current_manga_object.Merge(new_manga_object);
                        else
                            MangaObjectItems.Add(new_manga_object);
                        archive_file.Close();
                        if (ViewingSelectedMangaObject) this.SelectedMangaObject = new_manga_object;
                    }
                    break;

                case WatcherChangeTypes.Deleted:
                    MangaObjectItems.Remove(current_manga_object);
                    break;
            }
        }

        private void ConfigureSearchFilter()
        {
            mangaListView = CollectionViewSource.GetDefaultView(MangaObjectItems);
            mangaListView.Filter = mangaObject => String.IsNullOrWhiteSpace(SearchFilter) ? true : (mangaObject as MangaObject).IsNameMatch(SearchFilter);
            if (mangaListView.CanSort)
                mangaListView.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
        }

        private void LoadBookmarkObject()
        {
            Stream bookmark_file;
            if (Singleton<ZipStorage>.Instance.TryRead(Path.Combine(App.MANGA_ARCHIVE_DIRECTORY, this.SelectedMangaObject.MangaArchiveName(App.MANGA_ARCHIVE_EXTENSION)), out bookmark_file, typeof(BookmarkObject).Name))
            { using (bookmark_file) this.BookmarkObject = bookmark_file.Deserialize<BookmarkObject>(SaveType: App.UserConfig.SaveType); }
            else { this.BookmarkObject = null; }
            if (this.SelectedMangaObject != null && this.BookmarkObject != null) { this.SelectedChapter = this.SelectedMangaObject.ChapterObjectOfBookmarkObject(this.BookmarkObject); }
        }
    }
}
