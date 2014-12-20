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
    public class MangaArchiveInformationObject : DependencyObject, INotifyPropertyChanging, INotifyPropertyChanged
    {
        #region NotifyPropertyChange
        public event PropertyChangingEventHandler PropertyChanging;
        protected void OnPropertyChanging([CallerMemberName] String caller = "")
        {
            if (PropertyChanging != null)
                PropertyChanging(this, new PropertyChangingEventArgs(caller));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] String caller = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(caller));
        }
        #endregion

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
                d.SetValue(
                    ResumeChapterObjectPropertyKey, 
                    BookmarkObject.Equals(_this.BookmarkObject, null) ? null : _this.MangaObject.ChapterObjectOfBookmarkObject(_this.BookmarkObject));
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

        #region Constructors
        public MangaArchiveInformationObject() : base() { }
        public MangaArchiveInformationObject(MangaObject MangaObject, BookmarkObject BookmarkObject)
            : base()
        {
            this.MangaObject = MangaObject;
            this.BookmarkObject = BookmarkObject;
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
