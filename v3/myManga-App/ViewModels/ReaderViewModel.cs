using Core.IO;
using Core.IO.Storage.Manager.BaseInterfaceClasses;
using Core.MVVM;
using Core.Other.Singleton;
using myManga_App.IO.Network;
using Core.MVVM;
using myManga_App.Objects;
using myMangaSiteExtension.Objects;
using myMangaSiteExtension.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace myManga_App.ViewModels
{
    public sealed class ReaderViewModel : BaseViewModel
    {
        #region Variables
        private Boolean ContinueReading { get; set; }
        private String ArchiveFilePath { get; set; }
        private String NextArchiveFilePath { get; set; }
        private String PrevArchiveFilePath { get; set; }

        #region ChapterObjectPreloadDictionary
        private readonly Dictionary<String, ChapterObject> _ChapterObjectPreloadDictionary = new Dictionary<String, ChapterObject>();
        private Dictionary<String, ChapterObject> ChapterObjectPreloadDictionary
        { get { return _ChapterObjectPreloadDictionary; } }
        #endregion

        #region MangaObjectProperty
        private static readonly DependencyProperty MangaObjectProperty = DependencyProperty.RegisterAttached(
            "MangaObject",
            typeof(MangaObject),
            typeof(ReaderViewModel));
        public MangaObject MangaObject
        {
            get { return (MangaObject)GetValue(MangaObjectProperty); }
            set { SetValue(MangaObjectProperty, value); }
        }
        #endregion

        #region ChapterObjectProperty
        private static readonly DependencyProperty ChapterObjectProperty = DependencyProperty.RegisterAttached(
            "ChapterObject",
            typeof(ChapterObject),
            typeof(ReaderViewModel));
        public ChapterObject ChapterObject
        {
            get { return (ChapterObject)GetValue(ChapterObjectProperty); }
            set { SetValue(ChapterObjectProperty, value); }
        }
        #endregion

        #region PageObjectProperty
        private static readonly DependencyProperty SelectedPageObjectProperty = DependencyProperty.RegisterAttached(
            "SelectedPageObject",
            typeof(PageObject),
            typeof(ReaderViewModel),
            new PropertyMetadata(OnPageObjectChanged));
        public PageObject SelectedPageObject
        {
            get { return (PageObject)GetValue(SelectedPageObjectProperty); }
            set { SetValue(SelectedPageObjectProperty, value); }
        }

        private static void OnPageObjectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ReaderViewModel _this = (d as ReaderViewModel);
            _this.SaveBookmarkObject();
            _this.PreloadChapters();

        }
        #endregion

        #region BookmarkObjectProperty
        private static readonly DependencyProperty BookmarkObjectProperty = DependencyProperty.RegisterAttached(
            "BookmarkObject",
            typeof(BookmarkObject),
            typeof(ReaderViewModel));
        public BookmarkObject BookmarkObject
        {
            get { return (BookmarkObject)GetValue(BookmarkObjectProperty); }
            set { SetValue(BookmarkObjectProperty, value); }
        }
        #endregion

        #region PageZoomProperty
        private static readonly DependencyProperty PageZoomProperty = DependencyProperty.RegisterAttached(
            "PageZoom",
            typeof(Double),
            typeof(ReaderViewModel),
            new PropertyMetadata(1D, OnPageZoomChanged, OnPageZoomCoerce));
        public Double PageZoom
        {
            get { return (Double)GetValue(PageZoomProperty); }
            set { SetValue(PageZoomProperty, value); }
        }

        private static void OnPageZoomChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        { d.CoerceValue(PageZoomProperty); }

        private static object OnPageZoomCoerce(DependencyObject d, Object baseValue)
        { Double value = (Double)baseValue; return value < 0.5 ? 0.5 : value; }
        #endregion
        #endregion

        #region Commands
        #region NextPage
        private DelegateCommand _NextPageCommand;
        public ICommand NextPageCommand
        { get { return _NextPageCommand ?? (_NextPageCommand = new DelegateCommand(NextPage, CanNextPage)); } }

        private void NextPage()
        {
            if (this.BookmarkObject.Page < this.ChapterObject.Pages.Last().PageNumber) ++this.BookmarkObject.Page;
            else { this.ContinueReading = true; OpenChapter(this.MangaObject, ChapterObjectPreloadDictionary[this.NextArchiveFilePath]); }
            this.SelectedPageObject = this.ChapterObject.PageObjectOfBookmarkObject(this.BookmarkObject);
        }
        private Boolean CanNextPage()
        {
            Boolean n_page = this.BookmarkObject.Page < this.ChapterObject.Pages.Last().PageNumber;
            if (this.NextArchiveFilePath != null && File.Exists(this.NextArchiveFilePath)) n_page = true;
            return n_page;
        }
        #endregion

        #region PrevPage
        private DelegateCommand _PrevPageCommand;
        public ICommand PrevPageCommand
        { get { return _PrevPageCommand ?? (_PrevPageCommand = new DelegateCommand(PrevPage, CanPrevPage)); } }

        private void PrevPage()
        {
            if (this.BookmarkObject.Page > this.ChapterObject.Pages.First().PageNumber) --this.BookmarkObject.Page;
            else { this.ContinueReading = true; OpenChapter(this.MangaObject, ChapterObjectPreloadDictionary[this.PrevArchiveFilePath]); }
            this.SelectedPageObject = this.ChapterObject.PageObjectOfBookmarkObject(this.BookmarkObject);
        }
        private Boolean CanPrevPage()
        {
            Boolean p_page = this.BookmarkObject.Page > this.ChapterObject.Pages.First().PageNumber;
            if (this.PrevArchiveFilePath != null && File.Exists(this.PrevArchiveFilePath)) p_page = true;
            return p_page;
        }
        #endregion

        #region Reset PageZoom
        private DelegateCommand _ResetPageZoomCommand;
        public ICommand ResetPageZoomCommand
        { get { return _ResetPageZoomCommand ?? (_ResetPageZoomCommand = new DelegateCommand(ResetPageZoom)); } }

        private void ResetPageZoom()
        { this.PageZoom = App.UserConfig.DefaultPageZoom; }
        #endregion
        #endregion

        private void OpenChapter(ReadChapterRequestObject ReadChapterRequest)
        {
            this.PageZoom = App.UserConfig.DefaultPageZoom;
            OpenChapter(ReadChapterRequest.MangaObject, ReadChapterRequest.ChapterObject);
        }

        private void OpenChapter(MangaObject MangaObject, ChapterObject ChapterObject)
        {
            this.MangaObject = MangaObject;
            this.ChapterObject = ChapterObject;

            String base_path = Path.Combine(App.CHAPTER_ARCHIVE_DIRECTORY, MangaObject.MangaFileName());
            this.ArchiveFilePath = Path.Combine(base_path, ChapterObject.ChapterArchiveName(App.CHAPTER_ARCHIVE_EXTENSION));
            ChapterObjectPreloadDictionary.Clear();
            try
            {
                ChapterObject PrevChapter = this.MangaObject.PrevChapterObject(this.ChapterObject);
                ChapterObjectPreloadDictionary.Add(this.PrevArchiveFilePath = Path.Combine(base_path, PrevChapter.ChapterArchiveName(App.CHAPTER_ARCHIVE_EXTENSION)), PrevChapter);
            }
            catch { this.PrevArchiveFilePath = null; }
            try
            {
                ChapterObject NextChapter = this.MangaObject.NextChapterObject(this.ChapterObject);
                ChapterObjectPreloadDictionary.Add(this.NextArchiveFilePath = Path.Combine(base_path, NextChapter.ChapterArchiveName(App.CHAPTER_ARCHIVE_EXTENSION)), NextChapter);
            }
            catch { this.NextArchiveFilePath = null; }

            LoadChapterObject();
            LoadBookmarkObject();
            this.ContinueReading = false;

            ChapterGarbageCollector();
            this.PullFocus();
        }

        public ReaderViewModel()
            : base()
        {
            this.ContinueReading = false;
            if (!IsInDesignMode)
            {
                this.PageZoom = App.UserConfig.DefaultPageZoom;
                Messenger.Default.RegisterRecipient<FileSystemEventArgs>(this, ChapterObjectArchiveWatcher_Event, "ChapterObjectArchiveWatcher");
                Messenger.Default.RegisterRecipient<ReadChapterRequestObject>(this, OpenChapter, "ReadChapterRequest");
            }
        }

        #region Methods
        public void PreloadChapters()
        {
            foreach (System.Collections.Generic.KeyValuePair<String, ChapterObject> ChapterObjectPreload in ChapterObjectPreloadDictionary)
                if (!String.Equals(ChapterObjectPreload.Key, null) && !File.Exists(ChapterObjectPreload.Key))
                {
                    using (File.Open(ChapterObjectPreload.Key, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
                    { /* Touch Chapter File*/ }
                    DownloadManager.Default.Download(this.MangaObject, ChapterObjectPreload.Value);
                }
        }

        private void LoadChapterObject()
        {
            Stream archive_file;
            if (Singleton<ZipStorage>.Instance.TryRead(this.ArchiveFilePath, out archive_file, typeof(ChapterObject).Name))
            { using (archive_file) this.ChapterObject = archive_file.Deserialize<ChapterObject>(SaveType: App.UserConfig.SaveType); }
        }

        private void LoadBookmarkObject()
        {
            Stream bookmark_file;
            if (Singleton<ZipStorage>.Instance.TryRead(Path.Combine(App.MANGA_ARCHIVE_DIRECTORY, this.MangaObject.MangaArchiveName(App.MANGA_ARCHIVE_EXTENSION)), out bookmark_file, typeof(BookmarkObject).Name))
            { using (bookmark_file) this.BookmarkObject = bookmark_file.Deserialize<BookmarkObject>(SaveType: App.UserConfig.SaveType); }
            if (this.BookmarkObject == null) { this.BookmarkObject = new BookmarkObject(); }
            else if (this.BookmarkObject.Volume != this.ChapterObject.Volume || this.BookmarkObject.Chapter != this.ChapterObject.Chapter || this.BookmarkObject.SubChapter != this.ChapterObject.SubChapter)
            {
                this.BookmarkObject.Volume = this.ChapterObject.Volume;
                this.BookmarkObject.Chapter = this.ChapterObject.Chapter;
                this.BookmarkObject.SubChapter = this.ChapterObject.SubChapter;
                if (this.ContinueReading && this.BookmarkObject.Page <= 1) this.BookmarkObject.Page = this.ChapterObject.Pages.Last().PageNumber;
                else this.BookmarkObject.Page = this.ChapterObject.Pages.First().PageNumber;
            }
            this.SelectedPageObject = this.ChapterObject.PageObjectOfBookmarkObject(this.BookmarkObject);
        }

        private void SaveBookmarkObject()
        {
            if (this.ChapterObject != null)
            {
                this.BookmarkObject.Volume = this.ChapterObject.Volume;
                this.BookmarkObject.Chapter = this.ChapterObject.Chapter;
                this.BookmarkObject.SubChapter = this.ChapterObject.SubChapter;
                this.BookmarkObject.Page = this.ChapterObject.Pages.Count > 0 ? this.ChapterObject.Pages.First().PageNumber : 0;
            }
            if (this.SelectedPageObject != null) this.BookmarkObject.Page = this.SelectedPageObject.PageNumber;
            Singleton<ZipStorage>.Instance.Write(
                Path.Combine(App.MANGA_ARCHIVE_DIRECTORY, this.MangaObject.MangaArchiveName(App.MANGA_ARCHIVE_EXTENSION)),
                typeof(BookmarkObject).Name,
                this.BookmarkObject.Serialize(SaveType: App.UserConfig.SaveType));
        }

        private void ChapterGarbageCollector()
        {
            // TODO: Add more about bookmarks
            if (App.UserConfig.RemoveBackChapters)
            {
                Int32 index = this.MangaObject.IndexOfChapterObject(this.ChapterObject);
                index -= this.App.UserConfig.BackChaptersToKeep;
                if (--index > 0)
                {
                    for (; index >= 0; --index)
                    {
                        ChapterObject co = this.MangaObject.Chapters[index];
                        String save_path = Path.Combine(App.CHAPTER_ARCHIVE_DIRECTORY, this.MangaObject.MangaFileName(), co.ChapterArchiveName(App.CHAPTER_ARCHIVE_EXTENSION));
                        if (File.Exists(save_path))
                            File.Delete(save_path);
                    }
                }
            }
        }
        #endregion

        #region Event Handlers
        private void ChapterObjectArchiveWatcher_Event(FileSystemEventArgs e)
        {
            // TODO: This needs to be fixed
            if (e.FullPath.Equals(ArchiveFilePath))
                switch (e.ChangeType)
                {
                    case WatcherChangeTypes.Created:
                    case WatcherChangeTypes.Changed:
                        Stream archive_file;
                        if (Singleton<ZipStorage>.Instance.TryRead(e.FullPath, out archive_file, typeof(ChapterObject).Name))
                        { using (archive_file) { this.ChapterObject = archive_file.Deserialize<ChapterObject>(SaveType: App.UserConfig.SaveType); } }
                        break;

                    case WatcherChangeTypes.Deleted:
                        DownloadManager.Default.Download(this.MangaObject, this.ChapterObject);
                        break;
                }
        }
        #endregion
    }
}
