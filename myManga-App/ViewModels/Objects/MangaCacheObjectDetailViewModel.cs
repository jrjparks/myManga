using Core.MVVM;
using myManga_App.Objects;
using myManga_App.Objects.Cache;
using myMangaSiteExtension.Objects;
using myMangaSiteExtension.Utilities;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace myManga_App.ViewModels.Objects
{
    public sealed class MangaCacheObjectDetailViewModel : BaseViewModel
    {
        public MangaCacheObjectDetailViewModel()
            : this(null)
        { }
        public MangaCacheObjectDetailViewModel(MangaCacheObject MangaCacheObject)
            : base(false)
        {
            this.MangaCacheObject = MangaCacheObject;
        }

        #region MangaCacheObject
        private static readonly DependencyProperty MangaCacheObjectProperty = DependencyProperty.RegisterAttached(
            "MangaCacheObject",
            typeof(MangaCacheObject),
            typeof(MangaCacheObjectDetailViewModel),
            new PropertyMetadata(null));

        public MangaCacheObject MangaCacheObject
        {
            get { return (MangaCacheObject)GetValue(MangaCacheObjectProperty); }
            set { SetValue(MangaCacheObjectProperty, value); }
        }
        #endregion

        #region Downloads
        #region Chapter Download
        private DelegateCommand<ChapterObject> downloadChapterCommandAsync;
        public ICommand DownloadChapterCommandAsync
        { get { return downloadChapterCommandAsync ?? (downloadChapterCommandAsync = new DelegateCommand<ChapterObject>(DownloadChapterAsync, CanDownloadChapterAsync)); } }

        private Boolean CanDownloadChapterAsync(ChapterObject ChapterObject)
        {
            if (Equals(MangaCacheObject, null)) return false;
            if (Equals(MangaCacheObject.MangaObject, null)) return false;
            if (Equals(ChapterObject, null)) return false;
            return true;
        }

        private void DownloadChapterAsync(ChapterObject ChapterObject)
        { App.ContentDownloadManager.Download(MangaCacheObject.MangaObject, ChapterObject); }
        #endregion

        #region Selected Chapters Download
        private DelegateCommand<IList> downloadSelectedChaptersCommandAsync;
        public ICommand DownloadSelectedChaptersCommandAsync
        { get { return downloadSelectedChaptersCommandAsync ?? (downloadSelectedChaptersCommandAsync = new DelegateCommand<IList>(DownloadSelectedChaptersAsync, CanDownloadSelectedChaptersAsync)); } }

        private Boolean CanDownloadSelectedChaptersAsync(IList SelectedChapterObjects)
        {
            if (Equals(MangaCacheObject, null)) return false;
            if (Equals(MangaCacheObject.MangaObject, null)) return false;
            if (Equals(SelectedChapterObjects, null)) return false;
            if (Equals(SelectedChapterObjects.Count, 0)) return false;
            return true;
        }

        private void DownloadSelectedChaptersAsync(IList SelectedChapterObjects)
        {
            foreach (ChapterObject ChapterObject in SelectedChapterObjects)
            { App.ContentDownloadManager.Download(MangaCacheObject.MangaObject, ChapterObject); }
        }
        #endregion

        #region All Chapters Download
        private DelegateCommand downloadAllChaptersCommandAsync;
        public ICommand DownloadAllChaptersCommandAsync
        { get { return downloadAllChaptersCommandAsync ?? (downloadAllChaptersCommandAsync = new DelegateCommand(DownloadAllChaptersAsync, CanDownloadAllChaptersAsync)); } }

        private Boolean CanDownloadAllChaptersAsync()
        {
            if (Equals(MangaCacheObject, null)) return false;
            if (Equals(MangaCacheObject.MangaObject, null)) return false;
            return true;
        }

        private void DownloadAllChaptersAsync()
        {
            foreach (ChapterObject ChapterObject in MangaCacheObject.MangaObject.Chapters)
            { App.ContentDownloadManager.Download(MangaCacheObject.MangaObject, ChapterObject); }
        }
        #endregion

        #region To Latest Chapter Download
        private DelegateCommand downloadToLatestChapterCommandAsync;
        public ICommand DownloadToLatestChapterCommandAsync
        { get { return downloadToLatestChapterCommandAsync ?? (downloadToLatestChapterCommandAsync = new DelegateCommand(DownloadToLatestChapterAsync, CanDownloadToLatestChapterAsync)); } }

        private Boolean CanDownloadToLatestChapterAsync()
        {
            if (Equals(MangaCacheObject, null)) return false;
            if (Equals(MangaCacheObject.MangaObject, null)) return false;
            return true;
        }

        private void DownloadToLatestChapterAsync()
        {
            Int32 idx = MangaCacheObject.MangaObject.Chapters.IndexOf(MangaCacheObject.ResumeChapterObject);
            foreach (ChapterObject ChapterObject in MangaCacheObject.MangaObject.Chapters.Skip(idx))
            { App.ContentDownloadManager.Download(MangaCacheObject.MangaObject, ChapterObject); }
        }
        #endregion
        #endregion

        #region Open ChapterObjects

        #region Read ChapterObjects
        private DelegateCommand<IList> readSelectedChapterCommandAsync;
        public ICommand ReadSelectedChapterCommandAsync
        { get { return readSelectedChapterCommandAsync ?? (readSelectedChapterCommandAsync = new DelegateCommand<IList>(ReadSelectedChapterAsync, CanReadSelectedChapterAsync)); } }

        private Boolean CanReadSelectedChapterAsync(IList SelectedChapterObjects)
        {
            if (Equals(MangaCacheObject, null)) return false;
            if (Equals(MangaCacheObject.MangaObject, null)) return false;
            if (Equals(SelectedChapterObjects, null)) return false;
            if (!Equals(SelectedChapterObjects.Count, 1)) return false;
            return true;
        }

        private async void ReadSelectedChapterAsync(IList SelectedChapterObjects)
        {
            ChapterObject SelectedChapterObject = SelectedChapterObjects[0] as ChapterObject;
            String BookmarkChapterPath = Path.Combine(App.CHAPTER_ARCHIVE_DIRECTORY, MangaCacheObject.MangaObject.MangaFileName());
            MangaObject SelectedMangaObject = MangaCacheObject.MangaObject;
            if (!SelectedChapterObject.IsLocal(BookmarkChapterPath, App.CHAPTER_ARCHIVE_EXTENSION))
                await App.ContentDownloadManager.DownloadAsync(SelectedMangaObject, SelectedChapterObject);
            Messenger.Default.Send(new ReadChapterRequestObject(SelectedMangaObject, SelectedChapterObject), "ReadChapterRequest");
        }
        #endregion

        #endregion
    }
}
