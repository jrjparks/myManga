using myMangaSiteExtension.Objects;
using myMangaSiteExtension.Utilities;
using myMangaSiteExtension.Collections;
using System;
using System.Windows;

namespace myManga_App.Objects.Cache
{
    public sealed class MangaArchiveCacheObject : DependencyObject
    {
        #region Progress
        public IProgress<Int32> ProgressReporter
        { get; private set; }

        private static readonly DependencyProperty ProgressProperty = DependencyProperty.RegisterAttached(
            "Progress",
            typeof(Int32),
            typeof(MangaArchiveCacheObject),
            new PropertyMetadata(0));

        public Int32 Progress
        {
            get { return (Int32)GetValue(ProgressProperty); }
            set { SetValue(ProgressProperty, value); }
        }

        private static readonly DependencyProperty ProgressActiveProperty = DependencyProperty.RegisterAttached(
            "ProgressActive",
            typeof(Boolean),
            typeof(MangaArchiveCacheObject),
            new PropertyMetadata(false));

        public Boolean ProgressActive
        {
            get { return (Boolean)GetValue(ProgressActiveProperty); }
            set { SetValue(ProgressActiveProperty, value); }
        }
        #endregion

        #region DataObjects
        #region MangaObjectProperty
        private static readonly DependencyProperty MangaObjectProperty = DependencyProperty.RegisterAttached(
            "MangaObject",
            typeof(MangaObject),
            typeof(MangaArchiveCacheObject),
            new PropertyMetadata(OnDataObjectChanged));

        public MangaObject MangaObject
        {
            get { return (MangaObject)GetValue(MangaObjectProperty); }
            set { SetValue(MangaObjectProperty, value); }
        }
        #endregion

        #region BookmarkObjectProperty
        private static readonly DependencyProperty BookmarkObjectProperty = DependencyProperty.RegisterAttached(
            "BookmarkObject",
            typeof(BookmarkObject),
            typeof(MangaArchiveCacheObject),
            new PropertyMetadata(OnDataObjectChanged));

        public BookmarkObject BookmarkObject
        {
            get { return (BookmarkObject)GetValue(BookmarkObjectProperty); }
            set { SetValue(BookmarkObjectProperty, value); }
        }
        #endregion

        private static void OnDataObjectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MangaArchiveCacheObject _this = (d as MangaArchiveCacheObject);
            _this.LastUpdate = DateTime.Now;
            if (!_this.Empty())
            {
                _this.SetValue(IsNewPropertyKey, false);
                _this.SetValue(ChapterProgressPropertyKey, 0);
                _this.SetValue(ChapterProgressMaximumPropertyKey, _this.MangaObject.Chapters.Count);

                if (BookmarkObject.Equals(_this.BookmarkObject, null))  // Does a bookmark exist yet? No? Then this is a newly added manga.
                {
                    _this.SetValue(ResumeChapterObjectPropertyKey, null);
                    _this.SetValue(IsNewPropertyKey, true);
                    _this.SetValue(HasMoreToReadPropertyKey, true);
                }
                else   // Yes? The set the resume data.
                { _this.SetValue(ResumeChapterObjectPropertyKey, _this.MangaObject.ChapterObjectOfBookmarkObject(_this.BookmarkObject)); }

                if (ChapterObject.Equals(_this.ResumeChapterObject, null))
                { _this.SetValue(HasMoreToReadPropertyKey, false); }
                else
                {
                    Int32 ResumeChapterObjectIndex = _this.MangaObject.IndexOfChapterObject(_this.ResumeChapterObject) + 1;
                    _this.SetValue(HasMoreToReadPropertyKey, ResumeChapterObjectIndex < _this.MangaObject.Chapters.Count);
                    _this.SetValue(ChapterProgressPropertyKey, ResumeChapterObjectIndex);
                }
            }
            else
            {
                _this.SetValue(IsNewPropertyKey, true);
                _this.SetValue(HasMoreToReadPropertyKey, true);
                _this.SetValue(ChapterProgressPropertyKey, 0);
                _this.SetValue(ChapterProgressMaximumPropertyKey, 1);
            }
        }
        #endregion

        #region ResumeChapterObjectProperty
        private static readonly DependencyPropertyKey ResumeChapterObjectPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            "ResumeChapterObject",
            typeof(ChapterObject),
            typeof(MangaArchiveCacheObject),
            new PropertyMetadata(Property_Updated));
        private static readonly DependencyProperty ResumeChapterObjectProperty = ResumeChapterObjectPropertyKey.DependencyProperty;

        public ChapterObject ResumeChapterObject
        { get { return (ChapterObject)GetValue(ResumeChapterObjectProperty); } }
        #endregion

        #region HasMoreToReadProperty
        private static readonly DependencyPropertyKey HasMoreToReadPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            "HasMoreToRead",
            typeof(Boolean),
            typeof(MangaArchiveCacheObject),
            new PropertyMetadata(Property_Updated));
        private static readonly DependencyProperty HasMoreToReadProperty = HasMoreToReadPropertyKey.DependencyProperty;

        public Boolean HasMoreToRead
        { get { return (Boolean)GetValue(HasMoreToReadProperty); } }
        #endregion

        #region IsNewProperty
        private static readonly DependencyPropertyKey IsNewPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            "IsNew",
            typeof(Boolean),
            typeof(MangaArchiveCacheObject),
            new PropertyMetadata(Property_Updated));
        private static readonly DependencyProperty IsNewProperty = IsNewPropertyKey.DependencyProperty;

        public Boolean IsNew
        { get { return (Boolean)GetValue(IsNewProperty); } }
        #endregion

        #region ChapterProgressProperty
        private static readonly DependencyPropertyKey ChapterProgressPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            "ChapterProgress",
            typeof(Int32),
            typeof(MangaArchiveCacheObject),
            new PropertyMetadata(Property_Updated));
        private static readonly DependencyProperty ChapterProgressProperty = ChapterProgressPropertyKey.DependencyProperty;

        public Int32 ChapterProgress
        { get { return (Int32)GetValue(ChapterProgressProperty); } }

        private static readonly DependencyPropertyKey ChapterProgressMaximumPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            "ChapterProgressMaximum",
            typeof(Int32),
            typeof(MangaArchiveCacheObject),
            new PropertyMetadata(Property_Updated));
        private static readonly DependencyProperty ChapterProgressMaximumProperty = ChapterProgressMaximumPropertyKey.DependencyProperty;

        public Int32 ChapterProgressMaximum
        { get { return (Int32)GetValue(ChapterProgressMaximumProperty); } }
        #endregion

        #region LastUpdateProperty
        private static readonly DependencyProperty LastUpdateProperty = DependencyProperty.RegisterAttached(
            "LastUpdate",
            typeof(DateTime),
            typeof(MangaArchiveCacheObject));

        public DateTime LastUpdate
        {
            get { return (DateTime)GetValue(LastUpdateProperty); }
            set { SetValue(LastUpdateProperty, value); }
        }

        private static void Property_Updated(DependencyObject d, DependencyPropertyChangedEventArgs e)
        { (d as MangaArchiveCacheObject).LastUpdate = DateTime.Now; }
        #endregion

        #region Constructors
        public MangaArchiveCacheObject() : this(null, null) { }
        public MangaArchiveCacheObject(MangaObject MangaObject, BookmarkObject BookmarkObject)
            : base()
        {
            ProgressReporter = new Progress<Int32>(ProgressValue =>
            {
                ProgressActive = (0 < ProgressValue && ProgressValue < 100);
                Progress = ProgressValue;
            });

            this.MangaObject = MangaObject;
            this.BookmarkObject = BookmarkObject;
            if (!this.Empty() && !BookmarkObject.Equals(this.BookmarkObject, null))
            {
                this.SetValue(IsNewPropertyKey, false);
                this.SetValue(ResumeChapterObjectPropertyKey, this.MangaObject.ChapterObjectOfBookmarkObject(this.BookmarkObject));

                Int32 ResumeChapterObjectIndex = this.MangaObject.IndexOfChapterObject(this.ResumeChapterObject) + 1;
                this.SetValue(HasMoreToReadPropertyKey, ResumeChapterObjectIndex < this.MangaObject.Chapters.Count);

                this.SetValue(ChapterProgressMaximumPropertyKey, this.MangaObject.Chapters.Count);
                this.SetValue(ChapterProgressPropertyKey, ResumeChapterObjectIndex);
            }
            else if (!this.Empty())
            {
                this.SetValue(IsNewPropertyKey, true);
                this.SetValue(HasMoreToReadPropertyKey, true);
                this.SetValue(ChapterProgressPropertyKey, 0);
                this.SetValue(ChapterProgressMaximumPropertyKey, 1);
            }
            this.LastUpdate = DateTime.Now;
        }
        #endregion
    }

    public static class MangaArchiveCacheObjectExtensions
    {
        #region Static Methods
        public static void Merge(this MangaArchiveCacheObject obj, MangaArchiveCacheObject value)
        {
            obj.MangaObject.Merge(value.MangaObject);
            obj.BookmarkObject = value.BookmarkObject;
        }

        public static Boolean Empty(this MangaArchiveCacheObject obj)
        {
            if (MangaArchiveCacheObject.Equals(obj, null)) return true;
            if (MangaObject.Equals(obj.MangaObject, null)) return true;
            if (MangaObject.Equals(obj.MangaObject, default(MangaObject))) return true;
            return false;
        }
        #endregion
    }
}
