using myMangaSiteExtension.Objects;
using myMangaSiteExtension.Utilities;
using System;
using System.IO;
using System.Windows;

namespace myManga_App.Objects.Cache
{
    public sealed class ChapterCacheObject : DependencyObject
    {
        #region ArchiveFileNames
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
                        return Path.Combine(
                            App.CHAPTER_ARCHIVE_DIRECTORY,
                            MangaObject.MangaFileName(),
                            ArchiveFileName);
                return initialArchiveFilePath;
            }
            set { initialArchiveFilePath = value; }
        }

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
        #endregion

        #region Constructors
        private readonly App App = App.Current as App;

        public ChapterCacheObject(MangaObject MangaObject, ChapterObject ChapterObject, Boolean CreateProgressReporter = true)
            : base()
        {
            DownloadProgressReporter = new Progress<Int32>(ProgressValue =>
            {
                DownloadProgressActive = (0 < ProgressValue && ProgressValue < 100);
                DownloadProgress = ProgressValue;
            });

            this.MangaObject = MangaObject;
            this.ChapterObject = ChapterObject;
        }

        public override string ToString()
        {
            if (!Equals(MangaObject, null))
                if (!Equals(ChapterObject, null))
                    return String.Format(
                        "[ChapterCacheObject][{0}]{1}/{2} - {3}.{4}.{5}",
                        IsLocal ? "LOCAL" : "CLOUD",
                        MangaObject.Name,
                        ChapterObject.Name,
                        ChapterObject.Volume,
                        ChapterObject.Chapter,
                        ChapterObject.SubChapter);
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

        #region Status

        #region IsLocal
        private static readonly DependencyPropertyKey IsLocalPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            "IsLocal",
            typeof(Boolean),
            typeof(ChapterCacheObject),
            null);
        private static readonly DependencyProperty IsLocalProperty = IsLocalPropertyKey.DependencyProperty;

        public Boolean IsLocal
        {
            get { return (Boolean)GetValue(IsLocalProperty); }
            internal set { SetValue(IsLocalPropertyKey, value); }
        }
        #endregion

        #region IsResumeChapter
        private static readonly DependencyPropertyKey IsResumeChapterPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            "IsResumeChapter",
            typeof(Boolean),
            typeof(ChapterCacheObject),
            null);
        private static readonly DependencyProperty IsResumeChapterProperty = IsResumeChapterPropertyKey.DependencyProperty;

        public Boolean IsResumeChapter
        {
            get { return (Boolean)GetValue(IsResumeChapterProperty); }
            internal set { SetValue(IsResumeChapterPropertyKey, value); }
        }
        #endregion

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
