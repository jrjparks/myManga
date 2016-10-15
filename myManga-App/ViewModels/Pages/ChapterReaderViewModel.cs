using myManga_App.IO.Local.Object;
using myManga_App.Objects.Cache;
using myManga_App.IO.StreamExtensions;
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
using DataVirtualization;
using myManga_App.ViewModels.Objects;

namespace myManga_App.ViewModels.Pages
{
    internal class ChapterImageProvider : IItemsProvider<ChapterPageViewModel>
    {
        private ChapterObject chapter;
        private string chapterArchiveFilePath;
        private readonly TimeSpan TIMEOUT = TimeSpan.FromSeconds(30);
        protected readonly App App = App.Current as App;

        public ChapterImageProvider(ChapterObject obj, string chArchiveFp)
        {
            chapter = obj;
            chapterArchiveFilePath = chArchiveFp;
        }

        public int FetchCount()
        {
            return chapter.Pages.Count;
        }

        public IList<ChapterPageViewModel> FetchRange(int startIndex, int count)
        {                        
            count = Math.Min(count, chapter.Pages.Count - startIndex);
            var tasks = new Task<BitmapImage>[count];
            for (int i = 0; i < count; i++)
                tasks[i] = LoadPageImageAsync(chapter.Pages[startIndex + i].Name);

            Task.WaitAll(tasks);

            var list = new List<ChapterPageViewModel>();
            for (int i = 0; i < count; i++)
            {
                list.Add(new ChapterPageViewModel(tasks[i].Result, chapter.Pages[i + startIndex]));
            }
            return list;
        }

        private async Task<BitmapImage> LoadPageImageAsync(string pageName)
        {
            BitmapImage pageImage = null;
            try
            {
                using (Stream PageImageStream = await App.CORE.ZipManager.ReadAsync(chapterArchiveFilePath, pageName).Retry(TIMEOUT))
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
    }

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
                            App.CORE.MANGA_ARCHIVE_DIRECTORY,
                            MangaObject.MangaArchiveName(App.CORE.MANGA_ARCHIVE_EXTENSION));
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
                                App.CORE.CHAPTER_ARCHIVE_DIRECTORY,
                                MangaObject.MangaFileName(),
                                ChapterObject.ChapterArchiveName(App.CORE.CHAPTER_ARCHIVE_EXTENSION));
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
                                App.CORE.CHAPTER_ARCHIVE_DIRECTORY,
                                MangaObject.MangaFileName(),
                                SiblingChapterObject.ChapterArchiveName(App.CORE.CHAPTER_ARCHIVE_EXTENSION));
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
                                App.CORE.CHAPTER_ARCHIVE_DIRECTORY,
                                MangaObject.MangaFileName(),
                                SiblingChapterObject.ChapterArchiveName(App.CORE.CHAPTER_ARCHIVE_EXTENSION));
                return initialPrevChapterArchiveFilePath;
            }
            set { initialPrevChapterArchiveFilePath = value; }
        }
        #endregion

        #region Constructors
        public ChapterReaderViewModel() : base()
        {
            PageCacheObjects = new ObservableCollection<PageCacheObject>();

            SetupReaderMode();

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

        public override bool CanPullFocus()
        {
            if (Equals(MangaObject, null)) return false;
            if (Equals(ChapterObject, null)) return false;
            return base.CanPullFocus();
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
            BookmarkObject newBookmarkObject = e.NewValue as BookmarkObject;
            control.SaveBookmarkObject(newBookmarkObject);
        }

        private async void SaveBookmarkObject(BookmarkObject BookmarkObject)
        {
            await App.CORE.ZipManager.WriteAsync(
                    MangaArchiveFilePath,
                    typeof(BookmarkObject).Name,
                    BookmarkObject.Serialize(App.CORE.UserConfiguration.SerializeType)).Retry(TIMEOUT);
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
            PageObject newPageObject = e.NewValue as PageObject;
            control.BookmarkObject.Page = newPageObject.PageNumber;
            control.PageImage = await control.LoadPageImageAsync();
            control.PreloadChapterObjects();
            control.SaveBookmarkObject(control.BookmarkObject);
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
                    Stream ThumbnailImageStream = await App.CORE.ZipManager.ReadAsync(ChapterArchiveFilePath, PageCacheObject.PageObject.Name).Retry(TIMEOUT);
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
            null);

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
        { get { return resetPageZoomCommand ?? (resetPageZoomCommand = new DelegateCommand(() => PageZoom = App.CORE.UserConfiguration.DefaultPageZoom)); } }
        #endregion

        #endregion

        #endregion

        #region Page Image Async Load
        private async Task<BitmapImage> LoadPageImageAsync()
        {
            BitmapImage pageImage = null;
            try
            {
                using (Stream PageImageStream = await App.CORE.ZipManager.ReadAsync(ChapterArchiveFilePath, PageObject.Name).Retry(TIMEOUT))
                {
                    if (Equals(PageImageStream, null) || Equals(await PageImageStream.CheckImageFileTypeAsync(), ImageStreamExtensions.ImageFormat.UNKNOWN))
                    { if (CanReloadPageAsync(PageObject)) { ReloadPageAsync(PageObject); } } // Reload the image if the local copy is not valid.
                    else
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

        private static readonly DependencyProperty VirtualImageCollectionProperty = DependencyProperty.RegisterAttached(
            "VirtualImageCollection",
            typeof(AsyncVirtualizingCollection<ChapterPageViewModel>),
            typeof(ChapterReaderViewModel));

        public AsyncVirtualizingCollection<ChapterPageViewModel> VirtualImageCollection
        {
            get { return (AsyncVirtualizingCollection<ChapterPageViewModel>)GetValue(VirtualImageCollectionProperty); }
            set { SetValue(VirtualImageCollectionProperty, value); }
        }

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
            SetupReaderMode();

            this.MangaObject = MangaObject;
            this.ChapterObject = ChapterObject;            

            String MangaChaptersDirectory = Path.Combine(
                App.CORE.CHAPTER_ARCHIVE_DIRECTORY,
                MangaObject.MangaFileName());
            ChapterArchiveFilePath = Path.Combine(
                MangaChaptersDirectory,
                ChapterObject.ChapterArchiveName(App.CORE.CHAPTER_ARCHIVE_EXTENSION));

            PrevChapterPreloading = false;
            ChapterObject PrevChapterObject = MangaObject.PrevChapterObject(ChapterObject);
            if (!Equals(PrevChapterObject, null))   // Check if there is a ChapterObject before the current
            { PrevChapterArchiveFilePath = Path.Combine(MangaChaptersDirectory, PrevChapterObject.ChapterArchiveName(App.CORE.CHAPTER_ARCHIVE_EXTENSION)); }
            else { PrevChapterArchiveFilePath = null; }

            NextChapterPreloading = false;
            ChapterObject NextChapterObject = MangaObject.NextChapterObject(ChapterObject);
            if (!Equals(NextChapterObject, null))   // Check if there is a ChapterObject after the current
            { NextChapterArchiveFilePath = Path.Combine(MangaChaptersDirectory, NextChapterObject.ChapterArchiveName(App.CORE.CHAPTER_ARCHIVE_EXTENSION)); }
            else { NextChapterArchiveFilePath = null; }

            this.ChapterObject = await LoadChapterObjectAsync();
            BookmarkObject = await LoadBookmarkObjectAsync(OpeningPreviousChapter, ResumeChapter);
            PageObject = this.ChapterObject.PageObjectOfBookmarkObject(BookmarkObject);
            PullFocus();

            PageCacheObjects.Clear();
            (await LoadPageCacheObjectsAsync()).ForEach(_ => PageCacheObjects.Add(_));

            ChapterCleanup(MangaObject, ChapterObject);            
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
                    Stream ChapterObjectStream = await App.CORE.ZipManager.ReadAsync(ChapterArchiveFilePath, typeof(ChapterObject).Name).Retry(TIMEOUT);
                    LoadChapterObjectAsyncCTS.Token.ThrowIfCancellationRequested();
                    using (ChapterObjectStream)
                    { ChapterObject = ChapterObjectStream.Deserialize<ChapterObject>(SerializeType: App.CORE.UserConfiguration.SerializeType); }
                }
                catch (OperationCanceledException) { }
                catch (Exception ex) { throw ex; }
                finally { }
            }

            VirtualImageCollection = new AsyncVirtualizingCollection<ChapterPageViewModel>(new ChapterImageProvider(ChapterObject, ChapterArchiveFilePath), 10);
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
                    Stream BookmarkObjectStream = await App.CORE.ZipManager.ReadAsync(MangaArchiveFilePath, typeof(BookmarkObject).Name).Retry(TIMEOUT);
                    LoadBookmarkObjectAsyncCTS.Token.ThrowIfCancellationRequested();
                    using (BookmarkObjectStream)
                    { BookmarkObject = BookmarkObjectStream.Deserialize<BookmarkObject>(SerializeType: App.CORE.UserConfiguration.SerializeType); }

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
            {
                // Lookup MangaCacheObject and ChapterCacheObject
                MangaCacheObject MangaCacheObject = App.MangaCacheObjects.FirstOrDefault(mco => Equals(
                    mco.MangaObject.Name,
                    MangaObject.Name));
                ChapterCacheObject ChapterCacheObject = Equals(MangaCacheObject, null) ? null : MangaCacheObject.ChapterCacheObjects.FirstOrDefault(cco => Equals(
                    cco.ArchiveFileName,
                    ChapterObject.ChapterArchiveName(App.CORE.CHAPTER_ARCHIVE_EXTENSION)));

                // Start Download
                App.ContentDownloadManager.Download(
                    MangaObject,
                    ChapterObject,
                    Equals(ChapterCacheObject, null) ? null : ChapterCacheObject.DownloadProgressReporter);
            }
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
            { PageObject = ChapterObject.NextPageObject(PageObject); }
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
            { PageObject = ChapterObject.PrevPageObject(PageObject); }
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

        #region Chapter Cleanup
        private void ChapterCleanup(MangaObject MangaObject, ChapterObject ChapterObject)
        {
            if (App.CORE.UserConfiguration.RemoveBackChapters)
            {
                Int32 Idx = MangaObject.IndexOfChapterObject(ChapterObject) - App.CORE.UserConfiguration.BackChaptersToKeep;
                if (--Idx > 0)
                {
                    for (; Idx >= 0; --Idx)
                    {
                        String ChapterPath = Path.Combine(
                            App.CORE.CHAPTER_ARCHIVE_DIRECTORY,
                            MangaObject.MangaFileName(),
                            MangaObject.Chapters[Idx].ChapterArchiveName(App.CORE.CHAPTER_ARCHIVE_EXTENSION));
                        if (File.Exists(ChapterPath)) File.Delete(ChapterPath);
                    }
                }
            }
        }
        #endregion

        #region Control Visibility
        private static readonly DependencyProperty InfiniteScrollingChapterViewerVisibilityProperty = DependencyProperty.RegisterAttached(
            "InfiniteScrollingChapterViewerVisibility",
            typeof(Visibility),
            typeof(ChapterReaderViewModel),
            null);

        public Visibility InfiniteScrollingChapterViewerVisibility
        {
            get { return (Visibility)GetValue(InfiniteScrollingChapterViewerVisibilityProperty); }
            set { SetValue(InfiniteScrollingChapterViewerVisibilityProperty, value); }
        }

        private static readonly DependencyProperty PageImageContentScrollViewerVisibilityProperty = DependencyProperty.RegisterAttached(
            "PageImageContentScrollViewerVisibility",
            typeof(Visibility),
            typeof(ChapterReaderViewModel),
            null);

        public Visibility PageImageContentScrollViewerVisibility
        {
            get { return (Visibility)GetValue(PageImageContentScrollViewerVisibilityProperty); }
            set { SetValue(PageImageContentScrollViewerVisibilityProperty, value); }
        }

        private void SetupReaderMode()
        {
            InfiniteScrollingChapterViewerVisibility = App.CORE.UserConfiguration.EnableInfiniteScrolling ? Visibility.Visible: Visibility.Hidden;
            PageImageContentScrollViewerVisibility = App.CORE.UserConfiguration.EnableInfiniteScrolling ? Visibility.Hidden : Visibility.Visible;            
        }

        #endregion
    }
}
