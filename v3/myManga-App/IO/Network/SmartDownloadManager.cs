using Amib.Threading;
using Core.IO;
using Core.IO.Storage.Manager.BaseInterfaceClasses;
using Core.Other.Singleton;
using myManga_App.Properties;
using myMangaSiteExtension.Objects;
using myMangaSiteExtension.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace myManga_App.IO.Network
{
    public sealed class SmartDownloadManager
    {
        protected readonly SynchronizationContext synchronizationContext;

        private readonly SmartMangaDownloader smd;
        private readonly SmartChapterDownloader scd;
        private readonly SmartPageDownloader spd;
        private readonly SmartImageDownloader sid;
        private readonly App App = App.Current as App;

        public event EventHandler ActivityUpdated;
        private void OnActivityUpdated(EventArgs e)
        {
            if (ActivityUpdated != null)
            {
                if (synchronizationContext == null)
                    ActivityUpdated(this, e);
                else
                    foreach (EventHandler del in ActivityUpdated.GetInvocationList())
                        synchronizationContext.Post(_e => del(this, _e as EventArgs), e);
            }
        }

        public Int32 Concurrency
        { get { return smd.Concurrency + scd.Concurrency + spd.Concurrency + sid.Concurrency; } }
        public Boolean IsIdle
        { get { return smd.IsIdle && scd.IsIdle && spd.IsIdle && sid.IsIdle; } }
        public Boolean SmartMangaDownloaderIsIdle
        { get { return smd.IsIdle; } }
        public Boolean SmartChapterDownloaderIsIdle
        { get { return scd.IsIdle; } }
        public Boolean SmartPageDownloaderIsIdle
        { get { return spd.IsIdle; } }
        public Boolean SmartImageDownloaderIsIdle
        { get { return sid.IsIdle; } }

        public SmartDownloadManager() : this(null) { }
        public SmartDownloadManager(STPStartInfo stpThredPool)
        {
            synchronizationContext = SynchronizationContext.Current;

            smd = new SmartMangaDownloader(stpThredPool);
            smd.MangaObjectComplete += smd_MangaObjectComplete;

            scd = new SmartChapterDownloader(stpThredPool);
            scd.ChapterObjectComplete += scd_ChapterObjectComplete;
            
            spd = new SmartPageDownloader(stpThredPool);
            spd.PageObjectComplete += spd_PageObjectComplete;

            sid = new SmartImageDownloader(stpThredPool);
            sid.SmartImageDownloadObjectComplete += sid_SmartImageDownloadObjectComplete;
        }

        #region SmartMangaDownloader
        public IWorkItemResult Download(MangaObject mangaObject)
        { return smd.DownloadMangaObject(mangaObject); }

        public ICollection<IWorkItemResult> Download(ICollection<MangaObject> mangaObjects)
        { return smd.DownloadMangaObject(mangaObjects); }

        private delegate void smd_MangaObjectCompleteInvoke(object sender, MangaObject e);
        private void smd_MangaObjectComplete(object sender, MangaObject e)
        {
            if (App.Dispatcher.Thread == Thread.CurrentThread)
            {
                OnActivityUpdated(null);
                String save_path = Path.Combine(App.MANGA_ARCHIVE_DIRECTORY, e.MangaArchiveName(App.MANGA_ARCHIVE_EXTENSION));
                Singleton<ZipStorage>.Instance.Write(save_path, e.GetType().Name, e.Serialize(SaveType: Settings.Default.SaveType));
                Singleton<ZipStorage>.Instance.Write(save_path, typeof(BookmarkObject).Name, new BookmarkObject().Serialize(SaveType: Settings.Default.SaveType));
                foreach (String cover_url in e.Covers)
                { sid.Download(cover_url, save_path); }
            }
            else
                App.Dispatcher.BeginInvoke(new smd_MangaObjectCompleteInvoke(smd_MangaObjectComplete), new Object[] { sender, e });
        }
        #endregion

        #region SmartChapterDownloader
        public IWorkItemResult Download(ChapterObject chapterObject)
        { return scd.DownloadChapterObject(chapterObject); }

        public ICollection<IWorkItemResult> Download(ICollection<ChapterObject> chapterObjects)
        { return scd.DownloadChapterObject(chapterObjects); }

        private delegate void scd_ChapterObjectCompleteInvoke(object sender, ChapterObject e);
        private void scd_ChapterObjectComplete(object sender, ChapterObject e)
        {
            if (App.Dispatcher.Thread == Thread.CurrentThread)
            {
                OnActivityUpdated(null);
                String save_path = Path.Combine(App.CHAPTER_ARCHIVE_DIRECTORY, new String(e.MangaName.Where(Char.IsLetterOrDigit).ToArray()), e.ChapterArchiveName(App.CHAPTER_ARCHIVE_EXTENSION));
                Singleton<ZipStorage>.Instance.Write(save_path, e.GetType().Name, e.Serialize(SaveType: Settings.Default.SaveType));
                spd.DownloadPageObjectPages(e);
            }
            else
                App.Dispatcher.BeginInvoke(new smd_MangaObjectCompleteInvoke(smd_MangaObjectComplete), new Object[] { sender, e });
        }
        #endregion

        #region SmartPageDownloader
        private void spd_PageObjectComplete(object sender, SmartPageDownloader.PageObjectCompleted e)
        {
            OnActivityUpdated(null);
            String save_path = Path.Combine(App.CHAPTER_ARCHIVE_DIRECTORY, new String(e.ChapterObject.MangaName.Where(Char.IsLetterOrDigit).ToArray()), e.ChapterObject.ChapterArchiveName(App.CHAPTER_ARCHIVE_EXTENSION));
            Singleton<ZipStorage>.Instance.Write(save_path, e.ChapterObject.GetType().Name, e.ChapterObject.Serialize(SaveType: Settings.Default.SaveType));
            sid.Download(e.PageObject.ImgUrl, save_path);
        }
        #endregion

        #region SmartImageDownloader
        private void sid_SmartImageDownloadObjectComplete(object sender, SmartImageDownloader.SmartImageDownloadObject e)
        {
            OnActivityUpdated(null);
            Singleton<ZipStorage>.Instance.Write(e.LocalPath, e.Filename, e.Stream);
            e.Stream.Close();
        }
        #endregion
    }
}
