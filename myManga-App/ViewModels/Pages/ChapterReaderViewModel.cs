using Core.IO;
using Core.MVVM;
using myManga_App.Objects.Cache;
using myMangaSiteExtension.Objects;
using myMangaSiteExtension.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
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
                if (!Equals(MangaObject, null))
                    return Path.Combine(App.MANGA_ARCHIVE_DIRECTORY, MangaObject.MangaArchiveName(App.MANGA_ARCHIVE_EXTENSION));
                return initialMangaArchiveFilePath;
            }
            set { initialMangaArchiveFilePath = value; }
        }

        private String initialChapterArchiveFilePath;
        public String ChapterArchiveFilePath
        {
            get
            {
                String ChapterDirectoryPath = Path.Combine(App.CHAPTER_ARCHIVE_DIRECTORY, MangaObject.MangaFileName());
                if (!Equals(ChapterObject, null))
                    return Path.Combine(ChapterDirectoryPath, ChapterObject.ChapterArchiveName(App.CHAPTER_ARCHIVE_EXTENSION));
                return initialChapterArchiveFilePath;
            }
            set { initialChapterArchiveFilePath = value; }
        }
        #endregion

        public ChapterReaderViewModel() : base()
        {
            if (!IsInDesignMode)
            {
                Messenger.Default.RegisterRecipient<ChapterCacheObject>(this, (cco) => { });
            }
        }

        #region Reader Objects

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

        private async static void OnBookmarkObjectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ChapterReaderViewModel control = d as ChapterReaderViewModel;
            BookmarkObject BookmarkObject = e.NewValue as BookmarkObject;
            await control.App.ZipManager.Retry(
                () => control.App.ZipManager.WriteAsync(
                    control.MangaArchiveFilePath, 
                    nameof(myMangaSiteExtension.Objects.BookmarkObject),
                    BookmarkObject.Serialize(control.App.UserConfig.SaveType)), 
                TimeSpan.FromMinutes(1));
        }
        #endregion

        #region MangaObject
        private static readonly DependencyProperty MangaObjectProperty = DependencyProperty.RegisterAttached(
            "MangaObject",
            typeof(MangaObject),
            typeof(ChapterReaderViewModel),
            new PropertyMetadata(null));

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
            new PropertyMetadata(null));

        public ChapterObject ChapterObject
        {
            get { return (ChapterObject)GetValue(ChapterObjectProperty); }
            set { SetValue(ChapterObjectProperty, value); }
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
        { await (d as ChapterReaderViewModel).LoadPageImageAsync(); }
        #endregion

        #region Chapter Overview
        private static readonly DependencyProperty ChapterOverviewProperty = DependencyProperty.RegisterAttached(
            "ChapterOverview",
            typeof(ObservableCollection<BitmapImage>),
            typeof(ChapterReaderViewModel),
            new PropertyMetadata(default(ObservableCollection<BitmapImage>)));

        public ObservableCollection<BitmapImage> ChapterOverview
        {
            get { return (ObservableCollection<BitmapImage>)GetValue(ChapterOverviewProperty); }
            set { SetValue(ChapterOverviewProperty, value); }
        }

        private CancellationTokenSource ChapterOverviewAsyncCTS { get; set; }
        private async Task LoadChapterOverviewAsync()
        {
            try { if (!Equals(ChapterOverviewAsyncCTS, null)) { ChapterOverviewAsyncCTS.Cancel(); } }
            catch { }
            using (ChapterOverviewAsyncCTS = new CancellationTokenSource())
            {
                try
                {
                    ChapterOverview.Clear();
                    String ArchivePath = Path.Combine(
                        App.CHAPTER_ARCHIVE_DIRECTORY,
                        MangaObject.MangaFileName(),
                        ChapterObject.ChapterArchiveName(App.CHAPTER_ARCHIVE_EXTENSION));
                    foreach (PageObject PageObject in ChapterObject.Pages)
                    {
                        BitmapImage ChapterOverviewImage = new BitmapImage();
                        Stream ChapterOverviewImageStream = await App.ZipManager.Retry(() =>
                        {
                            PageAsyncCTS.Token.ThrowIfCancellationRequested();
                            return App.ZipManager.ReadAsync(ArchivePath, PageObject.Name);
                        }, TIMEOUT);
                        using (ChapterOverviewImageStream)
                        {
                            ChapterOverviewImage.BeginInit();
                            ChapterOverviewImage.DecodePixelWidth = 200;
                            using (ChapterOverviewImage.StreamSource = ChapterOverviewImageStream)
                            {
                                ChapterOverviewImage.CacheOption = BitmapCacheOption.OnLoad;
                                ChapterOverviewImage.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                                ChapterOverviewImage.EndInit();
                            }
                        }
                        PageAsyncCTS.Token.ThrowIfCancellationRequested();
                        ChapterOverview.Add(ChapterOverviewImage);
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception ex) { throw ex; }
                finally { }
            }
        }
        #endregion

        #region Archive Paths
        private String ArchiveFilePath
        { get; set; }
        private String NextArchiveFilePath
        { get; set; }
        private String PrevArchiveFilePath
        { get; set; }
        #endregion

        #endregion

        #region Chapter Page Image
        private CancellationTokenSource PageAsyncCTS { get; set; }

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

        #endregion

        #region Page Image Async Load
        private async Task LoadPageImageAsync()
        {
            try { if (!Equals(PageAsyncCTS, null)) { PageAsyncCTS.Cancel(); } }
            catch { }
            using (PageAsyncCTS = new CancellationTokenSource())
            {
                try
                {
                    String ArchivePath = Path.Combine(
                        App.CHAPTER_ARCHIVE_DIRECTORY,
                        MangaObject.MangaFileName(),
                        ChapterObject.ChapterArchiveName(App.CHAPTER_ARCHIVE_EXTENSION));
                    BitmapImage _PageImage = new BitmapImage();
                    Stream PageImageStream = await App.ZipManager.Retry(() =>
                    {
                        PageAsyncCTS.Token.ThrowIfCancellationRequested();
                        return App.ZipManager.ReadAsync(ArchivePath, PageObject.Name);
                    }, TIMEOUT);
                    using (PageImageStream)
                    {
                        _PageImage.BeginInit();
                        using (_PageImage.StreamSource = PageImageStream)
                        {
                            _PageImage.CacheOption = BitmapCacheOption.OnLoad;
                            _PageImage.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                            _PageImage.EndInit();
                        }
                    }
                    PageAsyncCTS.Token.ThrowIfCancellationRequested();
                    PageImage = _PageImage;
                }
                catch (OperationCanceledException) { }
                catch (Exception ex) { throw ex; }
                finally { }
            }
        }
        #endregion

        #endregion

        #region Load Chapter

        private void OpenForReading(MangaObject MangaObject, ChapterObject ChapterObject, Boolean OpeningPreviousChapter = false)
        {
            this.MangaObject = MangaObject;
            this.ChapterObject = ChapterObject;

            String MangaChaptersDirectory = Path.Combine(App.CHAPTER_ARCHIVE_DIRECTORY, MangaObject.MangaFileName());
            ArchiveFilePath = Path.Combine(MangaChaptersDirectory, ChapterObject.ChapterArchiveName(App.CHAPTER_ARCHIVE_EXTENSION));

            try // Check if there is a ChapterObject befor the current
            { PrevArchiveFilePath = Path.Combine(MangaChaptersDirectory, MangaObject.PrevChapterObject(this.ChapterObject).ChapterArchiveName(App.CHAPTER_ARCHIVE_EXTENSION)); }
            catch { PrevArchiveFilePath = null; }

            try // Check if there is a ChapterObject after the current
            { NextArchiveFilePath = Path.Combine(MangaChaptersDirectory, MangaObject.NextChapterObject(this.ChapterObject).ChapterArchiveName(App.CHAPTER_ARCHIVE_EXTENSION)); }
            catch { NextArchiveFilePath = null; }

            Task.Run(() => LoadChapterObjectAsync())
                .ContinueWith(t => LoadBookmarkObjectAsync(OpeningPreviousChapter))
                .ContinueWith(t => LoadPageImageAsync())
                .ContinueWith(t => PullFocus())
                .ContinueWith(t => LoadChapterOverviewAsync());
        }

        private CancellationTokenSource LoadChapterObjectAsyncCTS { get; set; }
        private async Task LoadChapterObjectAsync()
        {
            try { if (!Equals(LoadChapterObjectAsyncCTS, null)) { LoadChapterObjectAsyncCTS.Cancel(); } }
            catch { }
            using (LoadChapterObjectAsyncCTS = new CancellationTokenSource())
            {
                try
                {
                    Stream ChapterObjectStream = await App.ZipManager.Retry(() =>
                    {
                        LoadChapterObjectAsyncCTS.Token.ThrowIfCancellationRequested();
                        return App.ZipManager.ReadAsync(ArchiveFilePath, typeof(ChapterObject).Name);
                    }, TIMEOUT);
                    LoadChapterObjectAsyncCTS.Token.ThrowIfCancellationRequested();
                    using (ChapterObjectStream)
                    { ChapterObject = ChapterObjectStream.Deserialize<ChapterObject>(SaveType: App.UserConfig.SaveType); }
                }
                catch (OperationCanceledException) { }
                catch (Exception ex) { throw ex; }
                finally { }
            }
        }

        private CancellationTokenSource LoadBookmarkObjectAsyncCTS { get; set; }
        private async Task LoadBookmarkObjectAsync(Boolean OpeningPreviousChapter = false)
        {
            try { if (!Equals(LoadBookmarkObjectAsyncCTS, null)) { LoadBookmarkObjectAsyncCTS.Cancel(); } }
            catch { }
            using (LoadBookmarkObjectAsyncCTS = new CancellationTokenSource())
            {
                try
                {
                    Stream BookmarkObjectStream = await App.ZipManager.Retry(() =>
                    {
                        LoadBookmarkObjectAsyncCTS.Token.ThrowIfCancellationRequested();
                        return App.ZipManager.ReadAsync(ArchiveFilePath, typeof(BookmarkObject).Name);
                    }, TIMEOUT);
                    LoadBookmarkObjectAsyncCTS.Token.ThrowIfCancellationRequested();
                    using (BookmarkObjectStream)
                    { BookmarkObject = BookmarkObjectStream.Deserialize<BookmarkObject>(SaveType: App.UserConfig.SaveType); }

                    if (Equals(BookmarkObject, null)) BookmarkObject = new BookmarkObject();

                    if (!Equals(BookmarkObject.Volume, ChapterObject.Volume))
                    { BookmarkObject.Volume = ChapterObject.Volume; }
                    if (!Equals(BookmarkObject.Chapter, ChapterObject.Chapter))
                    { BookmarkObject.Chapter = ChapterObject.Chapter; }
                    if (!Equals(BookmarkObject.SubChapter, ChapterObject.SubChapter))
                    { BookmarkObject.SubChapter = ChapterObject.SubChapter; }

                    if (OpeningPreviousChapter)
                    {
                        if (BookmarkObject.Page <= 1)
                        {
                            BookmarkObject.Page = ChapterObject.Pages.Last().PageNumber;
                        }
                    }
                    else { BookmarkObject.Page = ChapterObject.Pages.First().PageNumber; }

                    PageObject = ChapterObject.PageObjectOfBookmarkObject(BookmarkObject);
                }
                catch (OperationCanceledException) { BookmarkObject = new BookmarkObject(); }
                catch (Exception ex) { throw ex; }
                finally { }
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
            if (!Equals(NextArchiveFilePath, null))
                if (File.Exists(NextArchiveFilePath))
                    return true;
            if (PageObject.PageNumber < ChapterObject.Pages.Last().PageNumber)
                return true;
            return false;
        }

        private void PageNext()
        {
            if (PageObject.PageNumber < ChapterObject.Pages.Last().PageNumber)
            { BookmarkObject.Page = (PageObject = ChapterObject.NextPageObject(PageObject)).PageNumber; }
            else
            { OpenForReading(MangaObject, MangaObject.NextChapterObject(ChapterObject), false); }
        }
        #endregion

        #region Prev Page Change Commands
        private DelegateCommand pagePrevCommand;
        public ICommand PagePrevCommand
        { get { return pagePrevCommand ?? (pagePrevCommand = new DelegateCommand(PagePrev, CanPagePrev)); } }

        private Boolean CanPagePrev()
        {
            if (!Equals(PrevArchiveFilePath, null))
                if (File.Exists(PrevArchiveFilePath))
                    return true;
            if (PageObject.PageNumber > ChapterObject.Pages.Last().PageNumber)
                return true;
            return false;
        }

        private void PagePrev()
        {
            if (PageObject.PageNumber > ChapterObject.Pages.Last().PageNumber)
            { BookmarkObject.Page = (PageObject = ChapterObject.PrevPageObject(PageObject)).PageNumber; }
            else
            { OpenForReading(MangaObject, MangaObject.PrevChapterObject(ChapterObject), true); }
        }
        #endregion

        #endregion
    }
}
