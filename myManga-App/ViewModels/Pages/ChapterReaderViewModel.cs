using myManga_App.IO.Local.Object;
using myManga_App.Objects.Cache;
using myMangaSiteExtension.Objects;
using myMangaSiteExtension.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Communication;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace myManga_App.ViewModels.Pages
{
    public sealed class ChapterReaderViewModel : BaseViewModel
    {
        private readonly TimeSpan TIMEOUT = TimeSpan.FromSeconds(30);

        #region ArchiveFileNames
        private String initialMangaArchiveFilePath;
        public String MangaArchiveFilePath
        {
            get
            {
                if (Equals(initialMangaArchiveFilePath, null))
                    if (!Equals(MangaObject, null))
                        return Path.Combine(
                            App.MANGA_ARCHIVE_DIRECTORY,
                            MangaObject.MangaArchiveName(App.MANGA_ARCHIVE_EXTENSION));
                return initialMangaArchiveFilePath;
            }
            set { initialMangaArchiveFilePath = value; }
        }

        private String initialChapterArchiveFilePath;
        public String ChapterArchiveFilePath
        {
            get
            {
                if (Equals(initialChapterArchiveFilePath, null))
                    if (!Equals(MangaObject, null))
                        if (!Equals(ChapterObject, null))
                            return Path.Combine(
                                App.CHAPTER_ARCHIVE_DIRECTORY,
                                MangaObject.MangaFileName(),
                                ChapterObject.ChapterArchiveName(App.CHAPTER_ARCHIVE_EXTENSION));
                return initialChapterArchiveFilePath;
            }
            set { initialChapterArchiveFilePath = value; }
        }

        private Boolean NextChapterPreloading;
        private String initialNextChapterArchiveFilePath;
        public String NextChapterArchiveFilePath
        {
            get
            {
                ChapterObject SiblingChapterObject = MangaObject.NextChapterObject(ChapterObject);
                if (Equals(initialNextChapterArchiveFilePath, null))
                    if (!Equals(MangaObject, null))
                        if (!Equals(SiblingChapterObject, null))
                            return Path.Combine(
                                App.CHAPTER_ARCHIVE_DIRECTORY,
                                MangaObject.MangaFileName(),
                                SiblingChapterObject.ChapterArchiveName(App.CHAPTER_ARCHIVE_EXTENSION));
                return initialNextChapterArchiveFilePath;
            }
            set { initialNextChapterArchiveFilePath = value; }
        }

        private Boolean PrevChapterPreloading;
        private String initialPrevChapterArchiveFilePath;
        public String PrevChapterArchiveFilePath
        {
            get
            {
                ChapterObject SiblingChapterObject = MangaObject.PrevChapterObject(ChapterObject);
                if (Equals(initialPrevChapterArchiveFilePath, null))
                    if (!Equals(MangaObject, null))
                        if (!Equals(SiblingChapterObject, null))
                            return Path.Combine(
                                App.CHAPTER_ARCHIVE_DIRECTORY,
                                MangaObject.MangaFileName(),
                                SiblingChapterObject.ChapterArchiveName(App.CHAPTER_ARCHIVE_EXTENSION));
                return initialPrevChapterArchiveFilePath;
            }
            set { initialPrevChapterArchiveFilePath = value; }
        }
        #endregion

        #region Constructors
        public ChapterReaderViewModel() : base()
        {
            PageCacheObjects = new ObservableCollection<PageCacheObject>();
            if (!IsInDesignMode)
            {
                Messenger.Instance.RegisterRecipient<ChapterCacheObject>(this, async ChapterCacheObject =>
                await OpenForReading(ChapterCacheObject), "ReadChapterCacheObject");
                Messenger.Instance.RegisterRecipient<ChapterCacheObject>(this, async ChapterCacheObject =>
                await OpenForReading(ChapterCacheObject, true), "ResumeChapterCacheObject");
                Messenger.Instance.RegisterRecipient<FileSystemEventArgs>(this, async e =>
                {
                    // TODO: Test handle for ChapterArchive file updating with pages
                    if (Equals(e.FullPath, ChapterArchiveFilePath))
                    {
                        if (Equals(PageImage, null))
                        {
                            PageImage = await LoadPageImageAsync();
                        }
                        for (Int32 idx = 0; idx < PageCacheObjects.Count; ++idx)
                        {
                            if (Equals(PageCacheObjects[idx].ThumbnailImage, null))
                                PageCacheObjects[idx].ThumbnailImage = await LoadPageCacheObjectThumbnailImage(PageCacheObjects[idx]);
                        }
                    }

                }, "ChapterObjectArchiveWatcher");
            }
        }
        #endregion

        #region Reader Objects

        #region MangaObject
        private static readonly DependencyProperty MangaObjectProperty = DependencyProperty.RegisterAttached(
            "MangaObject",
            typeof(MangaObject),
            typeof(ChapterReaderViewModel),
            null);

        public MangaObject MangaObject
        {
            get { return (MangaObject)GetValue(MangaObjectProperty); }
            set { SetValue(MangaObjectProperty, value); }
        }
        #endregion

        #region ChapterObject
        private static readonly DependencyProperty ChapterObjectProperty = DependencyProperty.RegisterAttached(
            "ChapterObject",
            typeof(ChapterObject),
            typeof(ChapterReaderViewModel),
            null);

        public ChapterObject ChapterObject
        {
            get { return (ChapterObject)GetValue(ChapterObjectProperty); }
            set { SetValue(ChapterObjectProperty, value); }
        }
        #endregion

        #region BookmarkObject
        private static readonly DependencyProperty BookmarkObjectProperty = DependencyProperty.RegisterAttached(
            "BookmarkObject",
            typeof(BookmarkObject),
            typeof(ChapterReaderViewModel),
            new PropertyMetadata(OnBookmarkObjectChanged));

        public BookmarkObject BookmarkObject
        {
            get { return (BookmarkObject)GetValue(BookmarkObjectProperty); }
            set { SetValue(BookmarkObjectProperty, value); }
        }

        private static void OnBookmarkObjectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ChapterReaderViewModel control = d as ChapterReaderViewModel;
            BookmarkObject BookmarkObject = e.NewValue as BookmarkObject;
            control.SaveBookmarkObject();
        }

        private async void SaveBookmarkObject()
        {
            await App.ZipManager.Retry(
                () => App.ZipManager.WriteAsync(
                    MangaArchiveFilePath,
                    typeof(BookmarkObject).Name,
                    BookmarkObject.Serialize(App.UserConfig.SerializeType)),
                TIMEOUT);
        }
        #endregion

        #region PageObject
        private static readonly DependencyProperty PageObjectProperty = DependencyProperty.RegisterAttached(
            "PageObject",
            typeof(PageObject),
            typeof(ChapterReaderViewModel),
            new PropertyMetadata(null, OnPageObjectChanged));

        public PageObject PageObject
        {
            get { return (PageObject)GetValue(PageObjectProperty); }
            set { SetValue(PageObjectProperty, value); }
        }

        private async static void OnPageObjectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ChapterReaderViewModel control = d as ChapterReaderViewModel;
            control.PageImage = await control.LoadPageImageAsync();
            control.PreloadChapterObjects();
        }
        #endregion

        #region PageCacheObjects
        private static readonly DependencyProperty ChapterOverviewProperty = DependencyProperty.RegisterAttached(
            "PageCacheObjects",
            typeof(ObservableCollection<PageCacheObject>),
            typeof(ChapterReaderViewModel),
            new PropertyMetadata(default(ObservableCollection<PageCacheObject>)));

        public ObservableCollection<PageCacheObject> PageCacheObjects
        {
            get { return (ObservableCollection<PageCacheObject>)GetValue(ChapterOverviewProperty); }
            set { SetValue(ChapterOverviewProperty, value); }
        }

        private CancellationTokenSource PageCacheObjectsAsyncCTS { get; set; }
        private async Task<List<PageCacheObject>> LoadPageCacheObjectsAsync()
        {
            try
            {
                if (!Equals(PageCacheObjectsAsyncCTS, null))
                    if (PageCacheObjectsAsyncCTS.Token.CanBeCanceled)
                        PageCacheObjectsAsyncCTS.Cancel();
            }
            catch { }
            using (PageCacheObjectsAsyncCTS = new CancellationTokenSource())
            {
                List<PageCacheObject> PageCacheObjects = new List<PageCacheObject>();
                foreach (PageObject PageObject in ChapterObject.Pages)
                {
                    PageCacheObject PageCacheObject = new PageCacheObject(MangaObject, ChapterObject, PageObject);
                    PageCacheObject.ThumbnailImage = await LoadPageCacheObjectThumbnailImage(PageCacheObject);
                    PageCacheObjectsAsyncCTS.Token.ThrowIfCancellationRequested();
                    PageCacheObjects.Add(PageCacheObject);
                }
                return PageCacheObjects;
            }
        }

        private async Task<BitmapImage> LoadPageCacheObjectThumbnailImage(PageCacheObject PageCacheObject)
        {
            BitmapImage ThumbnailImage = null;
            if (Equals(PageCacheObject.ThumbnailImage, null))
            {
                try
                {
                    Stream ThumbnailImageStream = await App.ZipManager.Retry(() =>
                    {
                        return App.ZipManager.ReadAsync(ChapterArchiveFilePath, PageCacheObject.PageObject.Name);
                    }, TIMEOUT);
                    if (!Equals(ThumbnailImageStream, null))
                        using (ThumbnailImageStream)
                        {
                            ThumbnailImage = new BitmapImage();
                            ThumbnailImage.BeginInit();
                            ThumbnailImage.DecodePixelWidth = 200;
                            ThumbnailImage.CacheOption = BitmapCacheOption.OnLoad;
                            ThumbnailImage.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                            using (ThumbnailImage.StreamSource = ThumbnailImageStream)
                            {
                                ThumbnailImage.EndInit();
                                ThumbnailImage.Freeze();
                            }
                        }
                }
                catch (OperationCanceledException) { ThumbnailImage = null; }
                catch (Exception ex) { throw ex; }
                finally { }
            }
            return ThumbnailImage;
        }
        #endregion

        #endregion

        #region Chapter Page Image

        #region Page Image

        #region Image
        private static readonly DependencyProperty PageImageProperty = DependencyProperty.RegisterAttached(
            "PageImage",
            typeof(BitmapImage),
            typeof(ChapterReaderViewModel),
            new PropertyMetadata(null));

        public BitmapImage PageImage
        {
            get { return (BitmapImage)GetValue(PageImageProperty); }
            set { SetValue(PageImageProperty, value); }
        }
        #endregion

        #region Page Zoom

        #region Page Zoom Property
        private static readonly DependencyProperty PageZoomProperty = DependencyProperty.RegisterAttached(
            "PageZoom",
            typeof(Double),
            typeof(ChapterReaderViewModel),
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

        #region Increase Zoom Command
        private DelegateCommand increasePageZoomCommand;
        public ICommand IncreasePageZoomCommand
        { get { return increasePageZoomCommand ?? (increasePageZoomCommand = new DelegateCommand(() => PageZoom += 0.1, () => PageZoom < 2)); } }
        #endregion

        #region Decrease Zoom Command
        private DelegateCommand decreasePageZoomCommand;
        public ICommand DecreasePageZoomCommand
        { get { return decreasePageZoomCommand ?? (decreasePageZoomCommand = new DelegateCommand(() => PageZoom -= 0.1, () => PageZoom > 0.5)); } }
        #endregion

        #region Reset Zoom Command
        private DelegateCommand resetPageZoomCommand;
        public ICommand ResetPageZoomCommand
        { get { return resetPageZoomCommand ?? (resetPageZoomCommand = new DelegateCommand(() => PageZoom = App.UserConfig.DefaultPageZoom)); } }
        #endregion

        #endregion

        #endregion

        #region Page Image Async Load
        private async Task<BitmapImage> LoadPageImageAsync()
        {
            BitmapImage pageImage = null;
            try
            {
                using (Stream PageImageStream = await App.ZipManager.Retry(() =>
                {
                    return App.ZipManager.ReadAsync(ChapterArchiveFilePath, PageObject.Name);
                }, TIMEOUT))
                {
                    if (!Equals(PageImageStream, null))
                    {
                        pageImage = new BitmapImage();
                        pageImage.BeginInit();
                        pageImage.CacheOption = BitmapCacheOption.OnLoad;
                        pageImage.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                        using (pageImage.StreamSource = PageImageStream)
                        {
                            pageImage.EndInit();
                            pageImage.Freeze();
                        }
                    }
                }
            }
            catch (OperationCanceledException) { pageImage = null; }
            catch (Exception ex) { throw ex; }
            finally { }
            return pageImage;
        }
        #endregion

        #endregion

        #region Load Chapter

        private async Task OpenForReading(ChapterCacheObject ChapterCacheObject)
        {
            await OpenForReading(ChapterCacheObject.MangaObject, ChapterCacheObject.ChapterObject, false, false);
        }

        private async Task OpenForReading(ChapterCacheObject ChapterCacheObject, Boolean ResumeChapter)
        {
            await OpenForReading(ChapterCacheObject.MangaObject, ChapterCacheObject.ChapterObject, false, ResumeChapter);
        }

        private async Task OpenForReading(MangaObject MangaObject, ChapterObject ChapterObject, Boolean OpeningPreviousChapter = false, Boolean ResumeChapter = false)
        {
            this.MangaObject = MangaObject;
            this.ChapterObject = ChapterObject;

            String MangaChaptersDirectory = Path.Combine(
                App.CHAPTER_ARCHIVE_DIRECTORY,
                MangaObject.MangaFileName());
            ChapterArchiveFilePath = Path.Combine(
                MangaChaptersDirectory,
                ChapterObject.ChapterArchiveName(App.CHAPTER_ARCHIVE_EXTENSION));

            PrevChapterPreloading = false;
            ChapterObject PrevChapterObject = MangaObject.PrevChapterObject(ChapterObject);
            if (!Equals(PrevChapterObject, null))   // Check if there is a ChapterObject before the current
            { PrevChapterArchiveFilePath = Path.Combine(MangaChaptersDirectory, PrevChapterObject.ChapterArchiveName(App.CHAPTER_ARCHIVE_EXTENSION)); }
            else { PrevChapterArchiveFilePath = null; }

            NextChapterPreloading = false;
            ChapterObject NextChapterObject = MangaObject.NextChapterObject(ChapterObject);
            if (!Equals(NextChapterObject, null))   // Check if there is a ChapterObject after the current
            { NextChapterArchiveFilePath = Path.Combine(MangaChaptersDirectory, NextChapterObject.ChapterArchiveName(App.CHAPTER_ARCHIVE_EXTENSION)); }
            else { NextChapterArchiveFilePath = null; }

            this.ChapterObject = await LoadChapterObjectAsync();
            BookmarkObject = await LoadBookmarkObjectAsync(OpeningPreviousChapter, ResumeChapter);
            PageObject = this.ChapterObject.PageObjectOfBookmarkObject(BookmarkObject);
            PullFocus();

            PageCacheObjects.Clear();
            (await LoadPageCacheObjectsAsync()).ForEach(_ => PageCacheObjects.Add(_));
        }

        private CancellationTokenSource LoadChapterObjectAsyncCTS { get; set; }
        private async Task<ChapterObject> LoadChapterObjectAsync()
        {
            ChapterObject ChapterObject = null;
            try
            {
                if (!Equals(LoadChapterObjectAsyncCTS, null))
                    if (LoadChapterObjectAsyncCTS.Token.CanBeCanceled)
                        LoadChapterObjectAsyncCTS.Cancel();
            }
            catch { }
            using (LoadChapterObjectAsyncCTS = new CancellationTokenSource())
            {
                try
                {
                    Stream ChapterObjectStream = await App.ZipManager.Retry(() =>
                    {
                        LoadChapterObjectAsyncCTS.Token.ThrowIfCancellationRequested();
                        return App.ZipManager.ReadAsync(ChapterArchiveFilePath, typeof(ChapterObject).Name);
                    }, TIMEOUT);
                    LoadChapterObjectAsyncCTS.Token.ThrowIfCancellationRequested();
                    using (ChapterObjectStream)
                    { ChapterObject = ChapterObjectStream.Deserialize<ChapterObject>(SerializeType: App.UserConfig.SerializeType); }
                }
                catch (OperationCanceledException) { }
                catch (Exception ex) { throw ex; }
                finally { }
            }
            return ChapterObject;
        }

        private CancellationTokenSource LoadBookmarkObjectAsyncCTS { get; set; }
        private async Task<BookmarkObject> LoadBookmarkObjectAsync(Boolean OpeningPreviousChapter = false, Boolean ResumeChapter = false)
        {
            BookmarkObject BookmarkObject = null;
            try
            {
                if (!Equals(LoadBookmarkObjectAsyncCTS, null))
                    if (LoadBookmarkObjectAsyncCTS.Token.CanBeCanceled)
                        LoadBookmarkObjectAsyncCTS.Cancel();
            }
            catch { }
            using (LoadBookmarkObjectAsyncCTS = new CancellationTokenSource())
            {
                try
                {
                    Stream BookmarkObjectStream = await App.ZipManager.Retry(() =>
                    {
                        LoadBookmarkObjectAsyncCTS.Token.ThrowIfCancellationRequested();
                        return App.ZipManager.ReadAsync(MangaArchiveFilePath, typeof(BookmarkObject).Name);
                    }, TIMEOUT);
                    LoadBookmarkObjectAsyncCTS.Token.ThrowIfCancellationRequested();
                    using (BookmarkObjectStream)
                    { BookmarkObject = BookmarkObjectStream.Deserialize<BookmarkObject>(SerializeType: App.UserConfig.SerializeType); }

                    if (Equals(BookmarkObject, null)) BookmarkObject = new BookmarkObject();

                    if (!Equals(BookmarkObject.Volume, ChapterObject.Volume))
                    { BookmarkObject.Volume = ChapterObject.Volume; }
                    if (!Equals(BookmarkObject.Chapter, ChapterObject.Chapter))
                    { BookmarkObject.Chapter = ChapterObject.Chapter; }
                    if (!Equals(BookmarkObject.SubChapter, ChapterObject.SubChapter))
                    { BookmarkObject.SubChapter = ChapterObject.SubChapter; }

                    if (OpeningPreviousChapter)
                    {
                        PageObject LastPageObject = ChapterObject.Pages.Last();
                        if (BookmarkObject.Page < LastPageObject.PageNumber)
                        {
                            BookmarkObject.Page = LastPageObject.PageNumber;
                        }
                    }
                    else if (!ResumeChapter || BookmarkObject.Page <= 0)
                    { BookmarkObject.Page = ChapterObject.Pages.First().PageNumber; }

                    if (!Equals(ChapterObject, null))
                    { BookmarkObject.LastPage = ChapterObject.Pages.Last().PageNumber; }
                }
                catch (OperationCanceledException) { BookmarkObject = new BookmarkObject(); }
                catch (Exception ex) { throw ex; }
                finally { }
            }

            return BookmarkObject;
        }

        private void PreloadChapterObjects()
        {
            if (!Equals(PrevChapterArchiveFilePath, null) && !File.Exists(PrevChapterArchiveFilePath))
            {
                if (!PrevChapterPreloading)
                {
                    PreloadChapterObject(MangaObject, MangaObject.PrevChapterObject(ChapterObject));
                    PrevChapterPreloading = true;
                }
            }
            else PrevChapterPreloading = false;
            if (!Equals(NextChapterArchiveFilePath, null) && !File.Exists(NextChapterArchiveFilePath))
            {
                if (!NextChapterPreloading)
                {
                    PreloadChapterObject(MangaObject, MangaObject.NextChapterObject(ChapterObject));
                    NextChapterPreloading = true;
                }
            }
            else NextChapterPreloading = false;
        }

        private void PreloadChapterObject(MangaObject MangaObject, ChapterObject ChapterObject)
        {
            if (!App.ContentDownloadManager.IsCacheKeyActive(App.ContentDownloadManager.CacheKey(MangaObject, ChapterObject)))
            { App.ContentDownloadManager.Download(MangaObject, ChapterObject); }
        }

        #endregion

        #region Page Change Commands

        #region Next Page Change Commands
        private DelegateCommand pageNextCommand;
        public ICommand PageNextCommand
        { get { return pageNextCommand ?? (pageNextCommand = new DelegateCommand(PageNext, CanPageNext)); } }

        private Boolean CanPageNext()
        {
            if (!Equals(NextChapterArchiveFilePath, null))
                if (File.Exists(NextChapterArchiveFilePath))
                    return true;
            if (Equals(PageObject, null)) return false;
            if (PageObject.PageNumber < ChapterObject.Pages.Last().PageNumber)
                return true;
            return false;
        }

        private async void PageNext()
        {
            if (PageObject.PageNumber < ChapterObject.Pages.Last().PageNumber)
            {
                BookmarkObject.Page = (PageObject = ChapterObject.NextPageObject(PageObject)).PageNumber;
                SaveBookmarkObject();

            }
            else
            { await OpenForReading(MangaObject, MangaObject.NextChapterObject(ChapterObject), false); }
        }
        #endregion

        #region Prev Page Change Commands
        private DelegateCommand pagePrevCommand;
        public ICommand PagePrevCommand
        { get { return pagePrevCommand ?? (pagePrevCommand = new DelegateCommand(PagePrev, CanPagePrev)); } }

        private Boolean CanPagePrev()
        {
            if (!Equals(PrevChapterArchiveFilePath, null))
                if (File.Exists(PrevChapterArchiveFilePath))
                    return true;
            if (Equals(PageObject, null)) return false;
            if (PageObject.PageNumber > ChapterObject.Pages.First().PageNumber)
                return true;
            return false;
        }

        private async void PagePrev()
        {
            if (PageObject.PageNumber > ChapterObject.Pages.First().PageNumber)
            {
                BookmarkObject.Page = (PageObject = ChapterObject.PrevPageObject(PageObject)).PageNumber;
                SaveBookmarkObject();
            }
            else
            { await OpenForReading(MangaObject, MangaObject.PrevChapterObject(ChapterObject), true); }
        }
        #endregion

        #endregion

        #region Context Menu Commands

        #region Reload Page Command
        private DelegateCommand<PageObject> reloadPageAsyncCommand;
        public ICommand ReloadPageAsyncCommand
        { get { return reloadPageAsyncCommand ?? (reloadPageAsyncCommand = new DelegateCommand<PageObject>(ReloadPageAsync, CanReloadPageAsync)); } }

        private Boolean CanReloadPageAsync(PageObject PageObject)
        {
            if (Equals(PageObject, null)) return false;
            return true;
        }

        private async void ReloadPageAsync(PageObject PageObject)
        {
            await App.ContentDownloadManager.DownloadAsync(MangaObject, ChapterObject, PageObject);
        }
        #endregion

        #endregion
    }
}
