using myManga_App.Objects;
using myManga_App.Objects.Cache;
using System;
using System.Collections;
using System.Communication;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace myManga_App.ViewModels.Objects.Cache.MangaCacheObjectViewModels
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
            null);

        public MangaCacheObject MangaCacheObject
        {
            get { return (MangaCacheObject)GetValue(MangaCacheObjectProperty); }
            set { SetValue(MangaCacheObjectProperty, value); }
        }
        #endregion

        #region Downloads
        #region Chapter Download
        private DelegateCommand<ChapterCacheObject> downloadChapterAsyncCommand;
        public ICommand DownloadChapterAsyncCommand
        { get { return downloadChapterAsyncCommand ?? (downloadChapterAsyncCommand = new DelegateCommand<ChapterCacheObject>(DownloadChapterAsync, CanDownloadChapterAsync)); } }

        private Boolean CanDownloadChapterAsync(ChapterCacheObject ChapterCacheObject)
        {
            if (Equals(MangaCacheObject, null)) return false;
            if (Equals(MangaCacheObject.MangaObject, null)) return false;
            if (Equals(ChapterCacheObject, null)) return false;
            if (Equals(ChapterCacheObject.ChapterObject, null)) return false;
            return true;
        }

        private void DownloadChapterAsync(ChapterCacheObject ChapterCacheObject)
        { App.ContentDownloadManager.Download(ChapterCacheObject.MangaObject, ChapterCacheObject.ChapterObject, ChapterCacheObject.DownloadProgressReporter); }
        #endregion

        #region Selected Chapters Download
        private DelegateCommand<IList> downloadSelectedChaptersAsyncCommand;
        public ICommand DownloadSelectedChaptersAsyncCommand
        { get { return downloadSelectedChaptersAsyncCommand ?? (downloadSelectedChaptersAsyncCommand = new DelegateCommand<IList>(DownloadSelectedChaptersAsync, CanDownloadSelectedChaptersAsync)); } }

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
            foreach (ChapterCacheObject ChapterCacheObject in SelectedChapterObjects)
            { App.ContentDownloadManager.Download(ChapterCacheObject.MangaObject, ChapterCacheObject.ChapterObject, ChapterCacheObject.DownloadProgressReporter); }
        }
        #endregion

        #region All Chapters Download
        private DelegateCommand downloadAllChaptersAsyncCommand;
        public ICommand DownloadAllChaptersAsyncCommand
        { get { return downloadAllChaptersAsyncCommand ?? (downloadAllChaptersAsyncCommand = new DelegateCommand(DownloadAllChaptersAsync, CanDownloadAllChaptersAsync)); } }

        private Boolean CanDownloadAllChaptersAsync()
        {
            if (Equals(MangaCacheObject, null)) return false;
            if (Equals(MangaCacheObject.MangaObject, null)) return false;
            return true;
        }

        private void DownloadAllChaptersAsync()
        {
            foreach (ChapterCacheObject ChapterCacheObject in MangaCacheObject.ChapterCacheObjects)
            { if (!ChapterCacheObject.IsLocal) { App.ContentDownloadManager.Download(ChapterCacheObject.MangaObject, ChapterCacheObject.ChapterObject, ChapterCacheObject.DownloadProgressReporter); } }
        }
        #endregion

        #region To Latest Chapter Download
        private DelegateCommand downloadToLatestChapterAsyncCommand;
        public ICommand DownloadToLatestChapterAsyncCommand
        { get { return downloadToLatestChapterAsyncCommand ?? (downloadToLatestChapterAsyncCommand = new DelegateCommand(DownloadToLatestChapterAsync, CanDownloadToLatestChapterAsync)); } }

        private Boolean CanDownloadToLatestChapterAsync()
        {
            if (Equals(MangaCacheObject, null)) return false;
            if (Equals(MangaCacheObject.MangaObject, null)) return false;
            return true;
        }

        private void DownloadToLatestChapterAsync()
        {
            Int32 idx = MangaCacheObject.ChapterCacheObjects.FindIndex(_ => _.IsResumeChapter);
            foreach (ChapterCacheObject ChapterCacheObject in MangaCacheObject.ChapterCacheObjects.Skip(idx))
            { if (!ChapterCacheObject.IsLocal) { App.ContentDownloadManager.Download(ChapterCacheObject.MangaObject, ChapterCacheObject.ChapterObject, ChapterCacheObject.DownloadProgressReporter); } }
        }
        #endregion
        #endregion

        #region Open ChapterObjects

        #region Read ChapterObject
        private DelegateCommand<IList> readSelectedChapterAsyncCommand;
        public ICommand ReadSelectedChapterAsyncCommand
        { get { return readSelectedChapterAsyncCommand ?? (readSelectedChapterAsyncCommand = new DelegateCommand<IList>(ReadSelectedChapterAsync, CanReadSelectedChapterAsync)); } }

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
            ChapterCacheObject ChapterCacheObject = SelectedChapterObjects[0] as ChapterCacheObject;
            if (!ChapterCacheObject.IsLocal)
                await App.ContentDownloadManager.DownloadAsync(ChapterCacheObject.MangaObject, ChapterCacheObject.ChapterObject, ChapterCacheObject.DownloadProgressReporter);
            Messenger.Instance.Send(new ReadChapterRequestObject(ChapterCacheObject.MangaObject, ChapterCacheObject.ChapterObject), "ReadChapterRequest");
            Messenger.Instance.Send(ChapterCacheObject, "ReadChapterCacheObject");
        }
        #endregion

        #region Resume ChapterObject
        private DelegateCommand<ChapterCacheObject> resumeReadingAsyncCommand;
        public ICommand ResumeReadingAsyncCommand
        { get { return resumeReadingAsyncCommand ?? (resumeReadingAsyncCommand = new DelegateCommand<ChapterCacheObject>(ResumeReadingAsync, CanResumeReadingAsync)); } }

        private Boolean CanResumeReadingAsync(ChapterCacheObject ChapterCacheObject)
        {
            if (Equals(MangaCacheObject, null)) return false;
            if (Equals(MangaCacheObject.MangaObject, null)) return false;
            if (Equals(ChapterCacheObject, null)) return false;
            if (Equals(ChapterCacheObject.MangaObject, null)) return false;
            if (Equals(ChapterCacheObject.ChapterObject, null)) return false;
            if (Equals(ChapterCacheObject.IsResumeChapter, null)) return false;
            return true;
        }

        private async void ResumeReadingAsync(ChapterCacheObject ChapterCacheObject)
        {
            if (!ChapterCacheObject.IsLocal)
                await App.ContentDownloadManager.DownloadAsync(ChapterCacheObject.MangaObject, ChapterCacheObject.ChapterObject, ChapterCacheObject.DownloadProgressReporter);
            Messenger.Instance.Send(new ReadChapterRequestObject(ChapterCacheObject.MangaObject, ChapterCacheObject.ChapterObject), "ReadChapterRequest");
            Messenger.Instance.Send(ChapterCacheObject, "ResumeChapterCacheObject");
        }
        #endregion

        #endregion
    }
}
