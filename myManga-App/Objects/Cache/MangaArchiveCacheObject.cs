using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using myMangaSiteExtension.Objects;
using myMangaSiteExtension.Utilities;
using System.Windows;

namespace myManga_App.Objects.Cache
{
    public sealed class MangaArchiveCacheObject : DependencyObject
    {
        #region MangaObjectProperty
        private static readonly DependencyProperty MangaObjectProperty = DependencyProperty.RegisterAttached(
            "MangaObject",
            typeof(MangaObject),
            typeof(MangaArchiveCacheObject),
            new PropertyMetadata(Property_Updated));
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
            new PropertyMetadata(OnBookmarkObjectChanged));
        private static void OnBookmarkObjectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MangaArchiveCacheObject _this = (d as MangaArchiveCacheObject);
            _this.LastUpdate = DateTime.Now;
            if (!_this.Empty())
            {
                if (BookmarkObject.Equals(_this.BookmarkObject, null))
                { d.SetValue(ResumeChapterObjectPropertyKey, null); }
                else
                { d.SetValue(ResumeChapterObjectPropertyKey, _this.MangaObject.ChapterObjectOfBookmarkObject(_this.BookmarkObject)); }

                if (ChapterObject.Equals(_this.ResumeChapterObject, null))
                { d.SetValue(HasMoreToReadPropertyKey, false); }
                else
                {
                    Int32 ResumeChapterObjectIndex = _this.MangaObject.IndexOfChapterObject(_this.ResumeChapterObject) + 1;
                    d.SetValue(HasMoreToReadPropertyKey, ResumeChapterObjectIndex < _this.MangaObject.Chapters.Count);
                }
            }
        }

        public BookmarkObject BookmarkObject
        {
            get { return (BookmarkObject)GetValue(BookmarkObjectProperty); }
            set { SetValue(BookmarkObjectProperty, value); }
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

        #region Guid
        private static readonly DependencyPropertyKey GuidPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            "Id",
            typeof(Guid),
            typeof(MangaArchiveCacheObject),
            null);
        private static readonly DependencyProperty GuidProperty = GuidPropertyKey.DependencyProperty;
        public Guid Id { get { return (Guid)GetValue(GuidProperty); } }
        #endregion

        #region Constructors
        public MangaArchiveCacheObject() : this(null, null) { }
        public MangaArchiveCacheObject(MangaObject MangaObject, BookmarkObject BookmarkObject)
            : base()
        {
            SetValue(GuidPropertyKey, Guid.NewGuid());
            this.MangaObject = MangaObject;
            this.BookmarkObject = BookmarkObject;
            if (!this.Empty() && !BookmarkObject.Equals(this.BookmarkObject, null))
                this.SetValue(ResumeChapterObjectPropertyKey, this.MangaObject.ChapterObjectOfBookmarkObject(this.BookmarkObject));
            this.LastUpdate = DateTime.Now;
        }
        #endregion
    }

    public static class MangaArchiveCacheObjectExtentions
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
            if (MangaObject.Equals(obj.MangaObject, null) || MangaObject.Equals(obj.MangaObject, default(MangaObject))) return true;
            return false;
        }
        #endregion
    }
}
