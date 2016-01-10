using System;
using System.Windows;
using myMangaSiteExtension.Objects;
using myMangaSiteExtension.Utilities;
using System.Windows.Media.Imaging;

namespace myManga_App.Objects.Cache
{
    public sealed class PageCacheObject : DependencyObject
    {
        #region Constructors
        private readonly App App = App.Current as App;
        private String initialArchiveFileName;
        public String ArchiveFileName
        {
            get
            {
                if (!Equals(ChapterObject, null))
                    return ChapterObject.ChapterArchiveName(App.CHAPTER_ARCHIVE_EXTENSION);
                return initialArchiveFileName;
            }
            set { initialArchiveFileName = value; }
        }

        private String initialArchiveFilePath;
        public String ArchiveFilePath
        {
            get
            {
                if (!Equals(MangaObject, null))
                    if (!Equals(ChapterObject, null))
                        return System.IO.Path.Combine(
                            App.CHAPTER_ARCHIVE_DIRECTORY,
                            MangaObject.MangaFileName(),
                            ArchiveFileName);
                return initialArchiveFilePath;
            }
            set { initialArchiveFilePath = value; }
        }

        public PageCacheObject(MangaObject MangaObject, ChapterObject ChapterObject, PageObject PageObject, Boolean CreateProgressReporter = true)
            : base()
        {
            DownloadProgressReporter = new Progress<Int32>(ProgressValue =>
            {
                DownloadProgressActive = (0 < ProgressValue && ProgressValue < 100);
                DownloadProgress = ProgressValue;
            });

            this.MangaObject = MangaObject;
            this.ChapterObject = ChapterObject;
            this.PageObject = PageObject;
        }

        public override string ToString()
        {
            if (!Equals(MangaObject, null))
                if (!Equals(ChapterObject, null))
                    if (!Equals(PageObject, null))
                        return String.Format(
                            "[PageCacheObject][{0}]{1}/{2} - {3}.{4}.{5}/{6}",
                            MangaObject.Name,
                            ChapterObject.Name,
                            ChapterObject.Volume,
                            ChapterObject.Chapter,
                            ChapterObject.SubChapter,
                            PageObject.PageNumber);
            return String.Format("{0}", base.ToString());
        }
        #endregion

        #region Manga
        private static readonly DependencyPropertyKey MangaObjectPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            "MangaObject",
            typeof(MangaObject),
            typeof(ChapterCacheObject),
            null);
        private static readonly DependencyProperty MangaObjectProperty = MangaObjectPropertyKey.DependencyProperty;

        public MangaObject MangaObject
        {
            get { return (MangaObject)GetValue(MangaObjectProperty); }
            internal set { SetValue(MangaObjectPropertyKey, value); }
        }
        #endregion

        #region Chapter
        private static readonly DependencyPropertyKey ChapterObjectPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            "ChapterObject",
            typeof(ChapterObject),
            typeof(ChapterCacheObject),
            null);
        private static readonly DependencyProperty ChapterObjectProperty = ChapterObjectPropertyKey.DependencyProperty;

        public ChapterObject ChapterObject
        {
            get { return (ChapterObject)GetValue(ChapterObjectProperty); }
            internal set { SetValue(ChapterObjectPropertyKey, value); }
        }
        #endregion

        #region Page
        private static readonly DependencyPropertyKey PageObjectPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            "PageObject",
            typeof(PageObject),
            typeof(PageCacheObject),
            null);
        private static readonly DependencyProperty PageObjectProperty = PageObjectPropertyKey.DependencyProperty;

        public PageObject PageObject
        {
            get { return (PageObject)GetValue(PageObjectProperty); }
            internal set { SetValue(PageObjectPropertyKey, value); }
        }
        #endregion

        #region ThumbnailImageImage
        private static readonly DependencyPropertyKey ThumbnailImagePropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            "ThumbnailImage",
            typeof(BitmapImage),
            typeof(PageCacheObject),
            null);
        private static readonly DependencyProperty ThumbnailImageProperty = ThumbnailImagePropertyKey.DependencyProperty;

        public BitmapImage ThumbnailImage
        {
            get { return (BitmapImage)GetValue(ThumbnailImageProperty); }
            internal set { SetValue(ThumbnailImagePropertyKey, value); }
        }
        #endregion

        #region Status

        #region Progress
        public IProgress<Int32> DownloadProgressReporter
        { get; private set; }

        private static readonly DependencyPropertyKey DownloadProgressPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            "DownloadProgress",
            typeof(Int32),
            typeof(ChapterCacheObject),
            new PropertyMetadata(0));
        private static readonly DependencyProperty DownloadProgressProperty = DownloadProgressPropertyKey.DependencyProperty;

        public Int32 DownloadProgress
        {
            get { return (Int32)GetValue(DownloadProgressProperty); }
            private set { SetValue(DownloadProgressPropertyKey, value); }
        }

        private static readonly DependencyPropertyKey DownloadProgressActivePropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            "DownloadProgressActive",
            typeof(Boolean),
            typeof(ChapterCacheObject),
            new PropertyMetadata(false));
        private static readonly DependencyProperty DownloadProgressActiveProperty = DownloadProgressActivePropertyKey.DependencyProperty;

        public Boolean DownloadProgressActive
        {
            get { return (Boolean)GetValue(DownloadProgressActiveProperty); }
            private set { SetValue(DownloadProgressActivePropertyKey, value); }
        }
        #endregion

        #endregion
    }
}
