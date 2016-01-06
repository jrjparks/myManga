using myMangaSiteExtension.Objects;
using myMangaSiteExtension.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace myManga_App.Objects.Cache
{
    public sealed class MangaCacheObject : DependencyObject
    {
        #region Constructors
        private readonly App App = App.Current as App;
        private String initialArchiveFileName;
        public String ArchiveFileName
        {
            get
            {
                if (!Equals(MangaObject, null))
                    return MangaObject.MangaArchiveName(App.MANGA_ARCHIVE_EXTENSION);
                return initialArchiveFileName;
            }
            set { initialArchiveFileName = value; }
        }

        public MangaCacheObject() :
            this(MangaObject: null, BookmarkObject: null, CreateProgressReporter: false)
        { }
        public MangaCacheObject(BookmarkObject BookmarkObject) :
            this(MangaObject: null, BookmarkObject: BookmarkObject, CreateProgressReporter: false)
        { }
        public MangaCacheObject(MangaObject MangaObject) :
            this(MangaObject: MangaObject, BookmarkObject: null, CreateProgressReporter: false)
        { }
        public MangaCacheObject(MangaObject MangaObject, BookmarkObject BookmarkObject) :
            this(MangaObject: MangaObject, BookmarkObject: BookmarkObject, CreateProgressReporter: false)
        { }
        public MangaCacheObject(MangaObject MangaObject, BookmarkObject BookmarkObject, Boolean CreateProgressReporter = true) : base()
        {
            DownloadProgressReporter = new Progress<Int32>(ProgressValue =>
            {
                DownloadProgressActive = (0 < ProgressValue && ProgressValue < 100);
                DownloadProgress = ProgressValue;
            });

            this.BookmarkObject = BookmarkObject;
            this.MangaObject = MangaObject;
        }

        public override string ToString()
        {
            if (!Equals(MangaObject, null))
                return String.Format("[MangaCacheObject]{0}", MangaObject.Name);
            return String.Format("{0}", base.ToString());
        }
        #endregion

        #region Manga
        private static readonly DependencyPropertyKey MangaObjectPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            "MangaObject",
            typeof(MangaObject),
            typeof(MangaCacheObject),
            new PropertyMetadata(OnCacheObjectChanged));
        private static readonly DependencyProperty MangaObjectProperty = MangaObjectPropertyKey.DependencyProperty;

        public MangaObject MangaObject
        {
            get { return (MangaObject)GetValue(MangaObjectProperty); }
            internal set { SetValue(MangaObjectPropertyKey, value); }
        }
        #endregion

        #region Chapter Cache
        private static readonly DependencyPropertyKey ChapterCacheObjectsPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            "ChapterCacheObjects",
            typeof(List<ChapterCacheObject>),
            typeof(MangaCacheObject),
            null);
        private static readonly DependencyProperty ChapterCacheObjectsProperty = ChapterCacheObjectsPropertyKey.DependencyProperty;

        public List<ChapterCacheObject> ChapterCacheObjects
        {
            get { return (List<ChapterCacheObject>)GetValue(ChapterCacheObjectsProperty); }
            internal set { SetValue(ChapterCacheObjectsPropertyKey, value); }
        }
        #endregion

        #region Bookmark
        private static readonly DependencyPropertyKey BookmarkObjectPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            "BookmarkObject",
            typeof(BookmarkObject),
            typeof(MangaCacheObject),
            new PropertyMetadata(OnCacheObjectChanged));
        private static readonly DependencyProperty BookmarkObjectProperty = BookmarkObjectPropertyKey.DependencyProperty;

        public BookmarkObject BookmarkObject
        {
            get { return (BookmarkObject)GetValue(BookmarkObjectProperty); }
            internal set { SetValue(BookmarkObjectPropertyKey, value); }
        }
        #endregion

        #region CacheObject Changed
        private static void OnCacheObjectChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            MangaCacheObject control = source as MangaCacheObject;
            if (!Equals(control.MangaObject, null))
            {   // MangaObject exists
                // Use the first ChapterObject or null for the ResumeChapterObject
                ChapterObject ResumeChapterObject = control.MangaObject.Chapters.FirstOrDefault();

                if (!Equals(control.BookmarkObject, null))
                {   // BookmarkObject exists
                    control.IsNewManga = false;
                    // A BookmarkObject exists, lookup the ResumeChapterObject
                    ResumeChapterObject = control.MangaObject.ChapterObjectOfBookmarkObject(control.BookmarkObject);

                    Double ChapterProgressMaxValue = control.MangaObject.Chapters.Count,
                        ChapterProgressValue = control.MangaObject.IndexOfChapterObject(ResumeChapterObject) + 1;
                    // Calculate and round the readers progress for chapters
                    control.ChapterProgress = (Int32)Math.Round((ChapterProgressValue / ChapterProgressMaxValue) * 100);

                    ChapterObject LastChapterObject = control.MangaObject.Chapters.LastOrDefault();
                    if (!Equals(LastChapterObject, null))
                    {   // If there is a ChapterObject to resume, check to see if there are more chapters/pages to read.
                        control.HasMoreToRead = false;
                        if (!control.HasMoreToRead) control.HasMoreToRead = control.BookmarkObject.Volume < LastChapterObject.Volume;
                        if (!control.HasMoreToRead) control.HasMoreToRead = control.BookmarkObject.Chapter < LastChapterObject.Chapter;
                        if (!control.HasMoreToRead) control.HasMoreToRead = control.BookmarkObject.SubChapter < LastChapterObject.SubChapter;
                        if (!control.HasMoreToRead) control.HasMoreToRead = control.BookmarkObject.Page < control.BookmarkObject.LastPage;
                    }
                }
                else
                {
                    control.IsNewManga = true;
                    control.ChapterProgress = 0;
                    control.HasMoreToRead = !Equals(ResumeChapterObject, null);
                }

                // Store boolean for if the ResumeChapterObject is null
                Boolean ResumeChapterIsNull = Equals(ResumeChapterObject, null);
                String MangaObjectChaptersDirectory = System.IO.Path.Combine(control.App.CHAPTER_ARCHIVE_DIRECTORY, control.MangaObject.MangaFileName());
                control.ChapterCacheObjects = new List<ChapterCacheObject>(
                    from ChapterObject in control.MangaObject.Chapters
                    select new ChapterCacheObject(control.MangaObject, ChapterObject)
                    {
                        // If ResumeChapterIsNull is true, there is no ResumeChapter
                        IsResumeChapter = ResumeChapterIsNull ? false : Equals(ChapterObject, ResumeChapterObject),

                        // Store if the ChapterObject archive exists locally
                        IsLocal = ChapterObject.IsLocal(
                            MangaObjectChaptersDirectory,
                            control.App.CHAPTER_ARCHIVE_EXTENSION)
                    });

            }
            else
            {
                control.IsNewManga = false;
                control.HasMoreToRead = false;
                control.ChapterProgress = 0;
            }
        }
        #endregion

        #region Cover
        private static readonly DependencyProperty CoverImageProperty = DependencyProperty.RegisterAttached(
            "CoverImage",
            typeof(BitmapImage),
            typeof(MangaCacheObject),
            new PropertyMetadata(null));

        public BitmapImage CoverImage
        {
            get { return (BitmapImage)GetValue(CoverImageProperty); }
            set { SetValue(CoverImageProperty, value); }
        }
        #endregion

        #region Resume Chapter
        public ChapterCacheObject ResumeChapterCacheObject
        {
            get { return ChapterCacheObjects.FirstOrDefault(_ => _.IsResumeChapter); }
        }
        #endregion

        #region Status

        #region IsNewManga
        private static readonly DependencyPropertyKey IsNewMangaPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            "IsNewManga",
            typeof(Boolean),
            typeof(MangaCacheObject),
            new PropertyMetadata(null));
        private static readonly DependencyProperty IsNewMangaProperty = IsNewMangaPropertyKey.DependencyProperty;

        public Boolean IsNewManga
        {
            get { return (Boolean)GetValue(IsNewMangaProperty); }
            internal set { SetValue(IsNewMangaPropertyKey, value); }
        }
        #endregion

        #region HasMoreToRead
        private static readonly DependencyPropertyKey HasMoreToReadPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            "HasMoreToRead",
            typeof(Boolean),
            typeof(MangaCacheObject),
            new PropertyMetadata(null));
        private static readonly DependencyProperty HasMoreToReadProperty = HasMoreToReadPropertyKey.DependencyProperty;

        public Boolean HasMoreToRead
        {
            get { return (Boolean)GetValue(HasMoreToReadProperty); }
            internal set { SetValue(HasMoreToReadPropertyKey, value); }
        }
        #endregion

        #region ChapterProgress
        private static readonly DependencyPropertyKey ChapterProgressPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            "ChapterProgress",
            typeof(Int32),
            typeof(MangaCacheObject),
            new PropertyMetadata(0));
        private static readonly DependencyProperty ChapterProgressProperty = ChapterProgressPropertyKey.DependencyProperty;

        public Int32 ChapterProgress
        {
            get { return (Int32)GetValue(ChapterProgressProperty); }
            internal set { SetValue(ChapterProgressPropertyKey, value); }
        }
        #endregion

        #region Progress
        public IProgress<Int32> DownloadProgressReporter
        { get; private set; }

        private static readonly DependencyPropertyKey DownloadProgressPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            "DownloadProgress",
            typeof(Int32),
            typeof(MangaCacheObject),
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
            typeof(MangaCacheObject),
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

    #region Extensions
    public static class MangaCacheObjectExtensions
    {
        public static void Update(this MangaCacheObject current, MangaCacheObject update)
        {
            if (!Equals(update, null))
            {
                // Update BookmarkObject
                if (!Equals(update.BookmarkObject, null))
                { current.BookmarkObject = update.BookmarkObject; }

                // Update MangaObject
                if (!Equals(update.MangaObject, null))
                { current.MangaObject = update.MangaObject; }

                // Update CoverImage
                if (!Equals(update.CoverImage, null))
                { current.CoverImage = update.CoverImage.Clone(); }
            }
        }
    }
    #endregion
}
