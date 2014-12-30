using myMangaSiteExtension.Objects;
using myMangaSiteExtension.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace myManga_App.Objects.UserInterface
{
    public class MangaArchiveInformationObject : DependencyObject
    {
        #region MangaObjectProperty
        private static readonly DependencyProperty MangaObjectProperty = DependencyProperty.RegisterAttached(
            "MangaObject",
            typeof(MangaObject),
            typeof(MangaArchiveInformationObject));

        public MangaObject MangaObject
        {
            get { return (MangaObject)GetValue(MangaObjectProperty); }
            protected set { SetValue(MangaObjectProperty, value); }
        }
        #endregion

        #region BookmarkObjectProperty
        private static readonly DependencyProperty BookmarkObjectProperty = DependencyProperty.RegisterAttached(
            "BookmarkObject",
            typeof(BookmarkObject),
            typeof(MangaArchiveInformationObject),
            new PropertyMetadata(OnBookmarkObjectChanged));

        private static void OnBookmarkObjectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MangaArchiveInformationObject _this = (d as MangaArchiveInformationObject);
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
            typeof(MangaArchiveInformationObject),
            null);
        private static readonly DependencyProperty ResumeChapterObjectProperty = ResumeChapterObjectPropertyKey.DependencyProperty;

        public ChapterObject ResumeChapterObject
        { get { return (ChapterObject)GetValue(ResumeChapterObjectProperty); } }
        #endregion

        #region HasMoreToReadProperty
        private static readonly DependencyPropertyKey HasMoreToReadPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            "HasMoreToRead",
            typeof(Boolean),
            typeof(MangaArchiveInformationObject),
            null);
        private static readonly DependencyProperty HasMoreToReadProperty = HasMoreToReadPropertyKey.DependencyProperty;

        public Boolean HasMoreToRead
        { get { return (Boolean)GetValue(HasMoreToReadProperty); } }
        #endregion

        #region LastUpdateProperty
        private static readonly DependencyProperty LastUpdateProperty = DependencyProperty.RegisterAttached(
            "LastUpdate",
            typeof(DateTime),
            typeof(MangaArchiveInformationObject));

        public DateTime LastUpdate
        {
            get { return (DateTime)GetValue(LastUpdateProperty); }
            set { SetValue(LastUpdateProperty, value); }
        }

        public void UpdateLastUpdate()
        { this.LastUpdate = DateTime.Now; }
        #endregion

        #region Constructors
        public MangaArchiveInformationObject() : base() { }
        public MangaArchiveInformationObject(MangaObject MangaObject, BookmarkObject BookmarkObject)
            : base()
        {
            this.MangaObject = MangaObject;
            this.BookmarkObject = BookmarkObject;
            if(!this.Empty())
                this.SetValue(ResumeChapterObjectPropertyKey, BookmarkObject.Equals(this.BookmarkObject, null) ? null : this.MangaObject.ChapterObjectOfBookmarkObject(this.BookmarkObject));
        }
        #endregion
    }

    public static class MangaArchiveInformationObjectExtentions
    {
        #region Static Methods
        public static void Merge(this MangaArchiveInformationObject obj, MangaArchiveInformationObject value)
        {
            obj.MangaObject.Merge(value.MangaObject);
            obj.BookmarkObject = value.BookmarkObject;
        }

        public static Boolean Empty(this MangaArchiveInformationObject obj)
        {
            if (MangaArchiveInformationObject.Equals(obj, null)) return true;
            if (MangaObject.Equals(obj.MangaObject, null) || MangaObject.Equals(obj.MangaObject, default(MangaObject))) return true;
            return false;
        }
        #endregion
    }
}
